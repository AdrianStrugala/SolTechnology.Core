# Logging

`SolTechnology.Core.Logging` adds request logging, correlation, scope enrichment, Chronicle step
reports, PII masking and the operation-lifecycle log events on top of
`Microsoft.Extensions.Logging`. It targets the ASP.NET Core shared framework. Consumer-facing usage
is in [`docs/Log.md`](../Log.md).

## One registration path

Both `AddSolLogging(Action<LoggingOptions>?)` and `AddSolLogging(IConfiguration)` call the same
internal `LoggingServiceRegistrations.RegisterCore`, which registers `ICorrelationIdService`,
`TimeProvider.System`, Chronicle (`AddSolChronicle`), the built-in `RequestHeadersEnricher` and the
validated `LoggingOptions` (`ValidateOnStart`). The overloads differ only in how options are filled:
`Configure` or `Bind` to `Logging:Core`. A single path keeps the overloads from drifting apart: an
overload that misses a service `LoggingMiddleware` resolves per request fails every request. The shared
registrations use `TryAdd*` or an explicit "already registered" check, so repeated calls and
consumer overrides are safe. `ModuleResolutionTests` resolves every service under `ValidateOnBuild` and
`ValidateScopes` and sends a request through `UseSolLogging()` for both overloads.

## Request pipeline

`LoggingMiddleware` runs per request in this order:

1. Resolve the correlation id, store it in `ICorrelationIdService`, and echo it when the response
   starts.
1. Skip everything else for `SkipPaths`; the correlation id is still echoed.
1. Build the log scope: the correlation id plus every `ILogScopeEnricher`. A failing enricher is
   logged and skipped. The JSON body is parsed once, only when an `IRequiresRequestBody` enricher
   asks for it, only for `application/json` within `MaxLoggedJsonBodyBytes`, and the stream is
   rewound for model binding (`RequestBodyBuffer`). Malformed JSON is logged at Debug and skipped; a
   client that disconnects mid-upload (`OperationCanceledException`, or an `IOException` such as
   `ConnectionResetException` or `BadHttpRequestException`) only skips body enrichment, because the
   pipeline's own read of the body surfaces the abort as the single Warning below.
1. Open the request's Chronicle root (see Chronicle below), write `Started`, call the rest of the
   pipeline, then write exactly one outcome entry.

When the request root records, the outcome entry is its report, EventId 2155
`Finished request [..] [..] -> [..] in [..]. Chronicle: [{ChronicleStatus}] [{@Chronicle}]`, at
Error for a failure or a 5xx, Warning for a client abort, a 4xx, a failed step or a slow request,
and Information otherwise. When Chronicle is disabled, replaced by another `IChronicle`, or the
request is long-lived, the middleware writes the entry it writes without Chronicle:

| Outcome | Entry | Level |
|---|---|---|
| Response completed | 2146 `Finished … -> [{StatusCode}]` | Information below 400, Warning for 4xx, Error for 5xx |
| Unhandled exception | 2145 `failed after`, with the exception; status set to 500 if the response has not started | Error |
| Client abort (`ConnectionResetException`, or cancellation of `RequestAborted`) | 2144 `aborted by client after`, with the exception | Warning |

A logged exception is never followed by a second entry: two Error entries for the same failure
would double error-rate alerts. Every middleware entry is a `[LoggerMessage]` method on the
internal `RequestEnvelopeLogger` (2141–2146, 2148) or `ChronicleLogMessages` (2155); 2147, the 1.x
finish entry with timings, is retired. Templates bracket every placeholder (Coding Guide §11).

`SkipPaths` and `LogDetail` endpoint lists match per path segment and case-insensitively
(`LoggingPathMatcher`, `PathString.StartsWithSegments`): `/health` covers `/health` and
`/health/ready` but not `/healthcare`. A trailing `/` in a prefix is ignored, and a prefix without a
leading `/` never matches.

## Correlation ids

`CorrelationId.FromRequest` resolves the id in this order:

1. The current `Activity` trace id when the request carried trace context: a `traceparent` header,
   or an `Activity` with a remote parent (another propagator).
1. A valid inbound `X-Correlation-Id`.
1. The current `Activity` trace id: the new trace the host started for this request.
1. A generated 32-character hex value.

ASP.NET Core starts a request `Activity` whenever logging, a `DiagnosticListener`, or an
`ActivitySource` listener is active, which is true in every real host. Letting that `Activity` win
unconditionally would replace every caller's `X-Correlation-Id` with a fresh trace id in production;
inbound trace context still wins because it already identifies the caller's trace.
The inbound header is accepted only when it is
at most 128 characters from `[A-Za-z0-9._:-]` (`CorrelationIdValidator`). The value is echoed on the
response and written into every log scope, so anything else (CR/LF, markup, separators) is replaced
by a generated id, and the rejection reason logged as a Warning never contains the rejected value.

`traceparent` is echoed only when the correlation id is the current trace id; an id taken from
`X-Correlation-Id` has no trace context, so no span id is invented for it. The same
`TryBuildTraceParentFromCurrentActivity` formats the header for the outbound handler in
`SolTechnology.Core.HTTP`.

## Masking

`PiiMask.Partial` keeps `keepChars` characters at each end only when the value is longer than
`4 * keepChars`, so at most half of a value is revealed; shorter values are fully masked, and a
negative `keepChars` throws `ArgumentOutOfRangeException`.

## Chronicle

Chronicle (`SolTechnology.Core.Logging.Chronicles`) records a bounded tree of named steps per root
operation and writes one entry per root. Consumer usage, options and markers are in
[`docs/Log.md`](../Log.md#chronicle).

### Placement and API

Chronicle lives in Logging, which already owns the operation log shapes, `OperationsName`,
`PiiMask` and the request middleware, and which CQRS, HTTP, MessageBus and Hangfire already
reference. Tale references Logging for it; SQL does not (see seams). Logging references no other
`SolTechnology.*` project, so no cycle can form.

`IChronicle` is a DI singleton (`ChronicleRecorder`) rather than a static facade: options and
`TimeProvider` are injected, every integration is DI-resolved, and Guide §9.6 rules out public
static state. The ambient node lives in a private `AsyncLocal` inside the internal
`AmbientChronicle`, like `CorrelationIdService`, so parallel branches (`Task.WhenAll`) build
isolated subtrees and every `await` restores the caller's node. Handles are a `readonly struct`
`ChronicleStep`: a `using` does not box, a disabled or root-less step costs one `AsyncLocal` read
and one `HasListeners()` check and allocates nothing, and closing is idempotent through
`Interlocked`, so a copied handle cannot close a node twice. `Current` is a non-owning handle that
cannot close an ancestor. Every write to a node is lock-guarded, and instrumentation never throws
into the caller: listener failures are caught, and a report that cannot be written is logged as a
warning.

`ChronicleRecorder` decides root, child or nothing; `ChronicleWriter` (one per recorder) applies
the limits, moves the ambient node and notifies listeners; each `ChronicleRoot` carries its writer,
which the SQL observer compares so a host records only its own roots.

### Roots

- `Begin` joins an open ambient root as a child and starts a root otherwise, so a CQRS command
  inside a request stays one report and a worker command gets its own.
- `BeginRoot` always starts a root and restores the previous node on dispose. Work that outlives
  its caller (fire-and-forget events, messages, jobs, `Task.Run`) would otherwise land in the
  caller's report as late or unfinished. It records `DetachedFrom` when another root is open and
  keeps the trace through `Activity.Current`. Suppressing `ExecutionContext` flow was rejected,
  because it also drops the correlation id and the activity.
- A step under an already closed parent while its root is open is recorded and counted as late;
  a step after its root closed is dropped without allocation.

### Report and emission

`ChronicleReportBuilder` snapshots a closed root into the public, read-only `ChronicleReport`:
siblings ordered by start, same-name siblings (SQL: same statement) aggregated from
`AggregateThreshold`, and `N+1 suspected` from `NPlusOneThreshold`. Bounds are per dimension
(`MaxSteps`, `MaxDepth`, `MaxAttributesPerStep`, `MaxAttributeValueLength` for values, keys, names,
error types and messages) plus a 32,768-character cap on the rendered tree; dropped work stays
visible as markers, and beyond `MaxSteps` steps only feed their parent's aggregate (overflow groups
capped at 64 keys). The tree is ASCII so every sink renders it, and it is rendered once, only when
a provider formats the entry.

`ChronicleReporter` writes 2154 under `SolTechnology.Core.Logging.Chronicle` for every root except
HTTP request roots. The level comes from the root before any report is built, so a disabled level
builds nothing: `Failed` is Error, `Cancelled`, a slow root, or an `Ok` root with a failed step is
Warning, otherwise Information. Sampling is tail-based: failures, cancellations, Warning and
above, failed steps and N+1 are always written, and only fast successes are sampled by
`SampleRate`. Head-based sampling was rejected because it cannot keep failures.

The `[LoggerMessage]` templates use `{@Chronicle}`, but the .NET 10 generator passes it under the
parameter name `Chronicle`, so providers receive the `ChronicleReport` object without an `@` key:
Serilog, OpenTelemetry and Application Insights store the tree text, and queries filter on the
scalar placeholders. A JSON-shaped state was rejected for 2.0 because `[LoggerMessage]` cannot
produce it.

### Request roots

`LoggingMiddleware` opens the request root through `ChronicleRequestRoot` right after the skip-path
check, without an activity (ASP.NET Core owns the request activity) and marked as reported by the
caller, so the reporter skips it and the middleware writes it as the finish entry 2155. One entry
per request was chosen over a separate report next to "Finished request"; the method, path, status
and elapsed-time placeholders stay so existing queries keep working. A 5xx without an exception
(an MVC filter mapped it) fails the root. WebSocket, upgraded and server-sent-event requests get no
root, because one report per connection would grow until disconnect; they, and hosts with
Chronicle disabled or replaced, keep 2144–2146.

### Activities

Each recorded step starts an `Activity` on `OperationsName` when the source has listeners and
`EmitActivities` is on, with typed tags from `Set`, the masked value from `SetMasked`, and
`error.type` and an error status from `Fail`. Sharing the existing source keeps consumers'
`AddSource` working without a duplicate CQRS span. HTTP client steps are logical spans around
`HttpClient`'s physical ones; SQL steps and the HTTP request root start none. Sampling never
affects activities.

### Flight recorder

An opt-in `ILoggerProvider` (alias `ChronicleFlightRecorder`) buffers Debug entries written inside
an open root and attaches them only to a failed root's report. A provider composes with Console,
OpenTelemetry and `builder.Logging.AddSerilog()`; a factory decorator would depend on
registration order and a Serilog sink would add a dependency. A post-configure step adds default
filter rules (`None` when disabled, so MEL never consults the provider; Warning for `Microsoft` and
`System`) unless a rule already targets the alias. It reads invalid options as disabled, because
the logger factory is built before `ValidateOnStart` reports them. Chronicle's own category is
never buffered.

### Integration seams

Modules that reference Logging call `IChronicle` directly; the installers call the idempotent
`AddSolChronicle()`, whose options builder still takes a later `Bind`, so configuration wins over
installer defaults. SqlClient commands are observed through `SqlClientDiagnosticListener`
(`WriteCommandBefore`/`After`/`Error`, read as `IReadOnlyList` of pairs with the command as
`IDbCommand`), from a hosted service, so Dapper, EF Core and raw `SqlConnection` are covered without
a wrapper or an SQL → Logging edge. The observer gates `Before` on an open root it owns and
`After`/`Error` on a pending command before reading a payload, and statement text is off by
default because it can carry literals. Hangfire's filter is process-wide, so with several hosts in
one process the last host's recorder reports every job (accepted, as for `CorrelationIdJobFilter`).

The per-module mapping of outcomes to step status lives with each module: [CQRS](cqrs.md),
[Tale](tale-framework.md), [HTTP clients](http-client.md),
[background processing](background-processing.md), and
[infrastructure modules](infrastructure-modules.md).

### SQL step names

`SQLStepNamer` names a command `SQL {OPERATION} {table}`, after the OpenTelemetry database span
name `{db.operation.name} {db.collection.name}`, because a bare keyword (`SQL SELECT`, or
`SQL DECLARE` for a Hangfire batch) did not say which table a slow or repeated command touched.
`SQLTokenizer` splits the text into names, string literals and symbols and skips comments, so a
keyword inside a literal, a comment or a bracketed name never matches; it is not a parser.

- The operation is the first keyword after leading `SET …;`, `DECLARE …;` and `BEGIN TRAN`.
- The table is the first `FROM` source followed by a name (a `FROM (` subquery is looked into), the
  target of `INSERT`, `UPDATE`, `DELETE`, `MERGE` and `TRUNCATE TABLE`, or the procedure of `EXEC`
  (`@ret =` skipped); a stored-procedure command is `SQL EXEC {procedure}`.
- The table keeps at most its last two name parts without quotes. A table variable, a name over
  128 characters, or no name gives `SQL {OPERATION}`, and no operation gives `SQL`.
- Only identifiers reach the name; values and literals stay behind `CaptureSQLCommandText`.
- The scan reads at most 4096 characters, uses spans and allocates only the name, and runs only
  after the observer's open-root gate; the name then goes through `MaxAttributeValueLength`.

Aggregation is unchanged: the group key is the name plus the hash of the full statement, and the
name depends only on the statement, so the same statement still forms one line, now with its table
(`SQL SELECT dbo.Streets x42 … N+1 suspected`), and different statements on one table stay apart.

## Operation events and tracing

`LoggerExtensions` adds `OperationStarted`, `OperationSucceeded` and `OperationFailed` to every
`ILogger` through an `extension(ILogger)` block. They emit event ids 2137–2140 through
`[LoggerMessage]` methods; a caller's message is always a `[{Message}]` argument, never a template.

`CoreLoggingActivitySources.OperationsName` is the public contract for tracing: hosts subscribe with
`AddSource(OperationsName)`. The `ActivitySource` field itself is internal, and only Chronicle starts
activities on it: every integration, CQRS included, gets its span from its Chronicle step, so no
package needs its own source with the same name.

CQRS references Logging, so it also depends on the ASP.NET Core shared framework. Packages version
in lockstep and warn with `SOLCORE001` on a mixed major-version graph; see
[Package release](package-release.md) and [CQRS](cqrs.md).

## Cross-module log contract

Every `SolTechnology.Core.*` module follows the same template rules (Coding Guide §11), so one
query style works across them:

- Every placeholder is bracketed, `[{Name}]`, so an empty value shows as `[]`; the brackets are
  text, and the property name stays the queryable key.
- A caught exception is the log entry's exception argument, never a `{Exception}` placeholder or
  text in the message.
- A failed `Result` is logged as a failure with its `Error` type and message, not as a success.
- Modules log only through `ILogger`, never `Console`, and a log scope carries the real identifier
  of the work it covers (for example the tale id).

Templates and property names are a contract: dashboards and alerts query them. Changing one is
reported as a log contract change in the release notes, and a renamed property is called out
(for example `HealthPath` → `UpstreamURL`, `Key` → `IdempotencyKey`). Event ids are listed in
[`docs/Log.md`](../Log.md) and, for the Hangfire start-up entries, [`docs/Hangfire.md`](../Hangfire.md).
