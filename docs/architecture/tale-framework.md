# Tale Framework

`SolTechnology.Core.Tale` provides readable, resumable orchestration through
`TaleHandler<TInput, TContext, TOutput>`, `Tale`, `Chapter<TContext>`,
`InteractiveChapter<TContext, TInput>`, and `Context<TInput, TOutput>`. Consumer usage lives in
[`docs/Tale.md`](../Tale.md).

`Tell()` is the table of contents for a workflow. Chapters contain individual operations, while
the typed context carries state. This keeps orchestration visible and avoids competing chain or
flow abstractions.

## Registration

`AddSolTale()` scans the supplied assemblies, or the calling and entry assemblies when none are
given. It is `NoInlining` and captures `Assembly.GetCallingAssembly()` itself, so the scan sees the
consumer's assembly, not the Tale assembly, even under `WebApplicationFactory`. It registers
chapters and handlers as transient, `TaleManager` as scoped, `TaleOptions` and
`TaleHandlerRegistry` as singletons, and the in-memory repository as the default
`ITaleRepository`. A self-referencing chapter input type is safe: the input-schema walk skips a
type already on its path and stops at depth 8.

## Direct and managed runs

Every `Handle` call starts a fresh context and run state, so a handler instance can be reused and
always returns the result of its own input. Only runs started by `TaleManager` are persisted
(`StartStory`, `ResumeStory`); a direct `Handle` call, including every CQRS query or command tale
sent through `IMediator`, never writes to the repository. Persisting every run made the singleton
in-memory store grow without bound and retain personal data, and no consumer reads direct runs
back. A direct run that pauses on an interactive chapter logs a warning: it was not stored and
cannot be resumed, so interactive tales must start through `TaleManager`.

`TaleManager` creates a fresh dependency-injection scope for every start and resume.

## Execution

Each chapter receives the run's `CancellationToken`. Only that token cancels a tale: an
`OperationCanceledException` raised while the caller's token is not cancelled (for example an
`HttpClient` timeout) is a chapter error and fails the tale. Exceptions from chapters become
chapter errors; the first error is the tale's result. The engine logs the exception once and
stores a generic error ("Chapter [X] failed unexpectedly.", or "Tale [X] failed unexpectedly." for
an inline step or the plan itself) with the `Activity.Current` trace ID as `CorrelationId`.
Exception text can carry connection strings or personal data, and the error reaches HTTP clients
and the persisted history. A cancelled run stores `Cancelled` and an exception outside a chapter
stores `Failed`, even when it escapes between two saves.

`InteractiveChapterInvoker` calls `ReadWithInput` by reflection with
`BindingFlags.DoNotWrapExceptions`, so a non-async chapter that throws synchronously is
classified like an async one (cancellation versus chapter error) instead of as a
`TargetInvocationException`.

The engine (`TaleEngine`) runs the plan through `TaleStepInterpreter`, keeps flags, history and
errors in `TaleRunState`, runs interactive chapters through `InteractiveChapterInvoker`, and saves
through `TaleStatePersistence`; all four are internal.

## Chapter history

The engine matches a chapter's history entry by its step index in `Tell()` and its full type name
without assembly information (namespace, name, generic arguments resolved recursively), stored in
`ChapterInfo.StepIndex` and `ChapterInfo.ChapterTypeName`. `ChapterId` is display only. The same
chapter read twice, or two chapters with the same name in different namespaces, therefore both run.
Leaving the assembly out keeps the key stable when the consumer redeploys a new assembly version;
`Type.FullName` of a closed generic would embed `Version=` and re-run a completed chapter on resume.

`StepIndex` is nullable, so history written by 1.x reads as `null` instead of `0`. `ResumeStory`
rejects such a tale with an error asking to restart it and leaves the stored record untouched;
it never resumes against the wrong keys. `Tell()` must be deterministic because it is rebuilt on
every resume.

## Persistence and concurrency

`ITaleRepository` is the application-owned persistence boundary; `UseTaleRepository<T>()` replaces
the in-memory default with the application's lifetime and dependencies. All methods take a
`CancellationToken`; `List` is optional and throws `NotSupportedException` unless implemented.

`Save` is a conditional write on `TaleInstance.Version`: it writes only when the stored version (a
missing tale counts as `0`) equals the instance's version, then stores and returns `Version + 1`;
on a mismatch, or when a new tale reuses another tale's idempotency key, it returns
`ConflictError` without writing and never throws. Returning a `Result` keeps conflicts in the
Result-everywhere model and reuses the existing `ConflictError`; the cost is that every custom
repository implements a conditional write.

Every persisted run first saves the tale as `Running` with the version it read, before any chapter
runs. That claim stores a new tale's idempotency key, so a concurrent `StartStory` with the same
key gets a conflict and `TaleManager` returns the first tale. A concurrent resume of the same
paused tale gets `ConflictError`, and a `CancelStory` during a run wins because the run stops at
its next save. A failed tale is never resumed. The terminal save uses no cancellation token, so a
cancelled run still stores `Cancelled`.

`Running` is a lease, not a lock. Every save of a run, starting with the claim, stamps
`TaleInstance.LastUpdatedAt` from the injected `TimeProvider`. `ResumeStory` refuses a `Running`
tale whose last save is younger than `TaleOptions.RunningLeaseTimeout` (default 5 minutes) and
takes over an older one, logging a warning: a host that crashed mid-run would otherwise leave the tale stuck, since
cancelling and restarting loses its progress. The takeover is an ordinary claim at the version it
read, so of two concurrent takeovers exactly one wins, and a still-alive original run stops at its
next save with `ConflictError`. Completed chapters are skipped; the interrupted chapter runs again,
so chapters with side effects must be idempotent. The lease reuses `LastUpdatedAt`, which every
repository already stores, instead of adding a column; a value written by another host's clock is
compared as is, so clock skew between hosts shortens or lengthens the lease. There is no heartbeat
inside a chapter, so the timeout must exceed the longest chapter.

Stored chapter errors keep their type only through `TaleJsonOptions.Default`, which registers the
`talePaused` and `taleCancelled` discriminators on `Error` (see
[Result and errors](core-result-and-errors.md)). `TaleInstance.Clone()` and DreamTravel's
`SQLiteTaleRepository` use it, and custom repositories must too.

The core package does not ship a database provider. DreamTravel contains `SQLiteTaleRepository`
as a sample-side implementation with a `Version` column, added on start to existing files, and an
`UPDATE … WHERE Version = @Version`. Process-restart durability exists only when the application
registers a durable repository.

### Upgrading from 1.x

Tale hosts are cut over in one step: every 1.x host stops before a 2.x host serves tale traffic,
and there is no rollback to 1.x once a 2.x host has written tale state. 1.x cannot read the Tale
error discriminators and drops `StepIndex` and `Version` on save, after which 2.x would reject the
tale as created by 1.x.

## HTTP surface

Applications expose Tale endpoints by inheriting `TaleController`, choosing the route and adding
their authorization policy. The base route is `/api/tale`:

| Operation | Endpoint |
|---|---|
| Start | `POST /{handler}/start` |
| Resume | `POST /{id}` |
| State | `GET /{id}` |
| Result | `GET /{id}/result` |
| Cancel | `DELETE /{id}` |

Only handlers opted in with `ITaleBuilder.ExposeOverHttp<THandler>()` are served. Exposing every
scanned handler let clients start query tales and domain services with arbitrary input, bypassing
validation and the CQRS behaviours. Any other name returns the same plain 404; when the name
belongs to a scanned handler the server logs a warning naming `ExposeOverHttp`, so operators of
upgraded apps can see why an endpoint disappeared. `GET /{id}` and `DELETE /{id}` load the tale
first and answer a tale of a handler that is not exposed with the same 404 as a missing tale, so
the state of internal tales can be neither read nor cancelled over HTTP.

The controller binds input with `TaleJsonOptions.Default`. Waiting for input returns `202`,
success `200`, a business failure `400`. Resume input that does not deserialize keeps the tale
paused with a `ValidationError` naming the JSON path. Unexpected errors return `500` with a fixed
message and the correlation ID (`Activity.Current` trace ID, else `HttpContext.TraceIdentifier`),
never the exception text.

## Logging

Engine and handler entries use the consumer handler's logger category inside a scope with
`TaleHandler` and the real `TaleId` (opened after the ID is assigned). `TaleManager` and
`TaleController` log under their own type names. Every placeholder is bracketed
(`[{ChapterId}]`), per the Coding Guide.
