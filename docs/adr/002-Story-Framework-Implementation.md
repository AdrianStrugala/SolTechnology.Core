# ADR-002: Story Framework

- **Status:** Accepted — implementation in progress. Earlier versions of this ADR over-stated
  readiness ("Production Ready ⭐⭐⭐⭐⭐"); this revision reflects the reality of the codebase
  after the 2026 review.
- **Date:** 2026-01 (initial); revised 2026-04 following framework review
  (`docs/reviews/Story-Framework-Review.md`).
- **Supersedes:** Chain and Flow patterns.
- **Related:** ADR-001 (acronym capitalization).

---

## Context

Workflows in SolTechnology.Core were previously expressed with two distinct abstractions —
`ChainHandler` (sequential, in-memory) and `FlowHandler` (persisted steps, own DSL). Both
suffered from:

- Two mental models for essentially the same problem (sequence + state + pause).
- Flow persistence was bespoke, not reusable between projects.
- Neither supported interactive pause/resume cleanly; flows served as a poor approximation.
- Business code readability was low — pipelines drowned in framework ceremony.

## Decision

Replace both with a single abstraction: **Story Framework**.

- **`StoryHandler<TInput, TContext, TOutput>`** orchestrates an explicit `TellStory()`
  pipeline.
- **`Chapter<TContext>`** / **`InteractiveChapter<TContext, TInput>`** implement individual
  steps. Interactive chapters cause the workflow to pause until the caller supplies typed
  JSON input.
- **`StoryManager`** owns start/resume/cancel for persisted stories. Creates a fresh DI
  scope per invocation to avoid captive dependencies.
- **`IStoryRepository`** is the persistence seam. In-memory (dev/test) and SQLite (with WAL
  journaling and retry-on-busy) implementations ship in-box.
- **`StoryController`** exposes a REST surface, restricted to handlers explicitly
  registered via `StoryHandlerRegistry` to prevent lateral-movement through short-name
  lookups.
- The framework embraces the **Tale Code** philosophy — code reads like prose, chapters are
  named as actions, the story is obvious at a glance.

## Key design guarantees

### Typed lifecycle signalling

Pause and cancellation are expressed through dedicated `Error` subtypes:

- `StoryPausedError` — story has reached an `InteractiveChapter`.
- `StoryCancelledError` — execution was cancelled via token or `CancelStory`.

Callers detect these by **type**, never by string-matching the message. The string-matched
pause detection from the previous revision has been removed from the engine, the manager
and the controller.


### Strong typing in the engine

`StoryEngine<TInput, TContext, TOutput>` is fully generic. The previous `dynamic`-based
property access and `RuntimeBinderException` catch blocks are gone; `TContext` must
inherit from `Context<TInput, TOutput>`, which gives the engine direct typed access to
`Input`, `Output`, `StoryInstanceId` and `CurrentChapterId`.

### Interactive chapter detection

The engine walks the inheritance chain to recognize `InteractiveChapter<,>`, rather than
inspecting only `BaseType`. Multi-level subclassing works.

### Idempotency, listing, cancellation

- `StartStory` accepts an `idempotencyKey` (also honored as `Idempotency-Key` HTTP header);
  retries return the existing instance instead of creating a new story.
- `IStoryRepository.ListAsync(status?, handlerTypeName?, skip, take)` enables operator
  tooling and dashboards.
- `StoryManager.CancelStory(storyId)` flips status to `Cancelled`; the controller exposes
  it as `DELETE /api/story/{id}`.

### HTTP semantics

- Successful completion → `200 OK`.
- Paused (waiting for input) → `202 Accepted`.
- Not found / invalid id → `404` / `400`.
- Failure → `400` with error envelope.

### Security posture

- The controller resolves handlers exclusively through `StoryHandlerRegistry` — only
  handlers registered by `RegisterStories()` (i.e. in the scanned assemblies) are exposed.
  Inherit the controller and annotate with `[Authorize]` to require authentication.
- `SqliteStoryRepository` validates the supplied path and sandboxes it via
  `Path.GetFullPath`.

## Alternatives considered

- **Keep Chain and Flow side-by-side.** Rejected: duplicate mental model, migration burden
  stays.
- **Adopt an external workflow engine (Temporal, Durable Task, Elsa).** Rejected for v1 —
  would introduce out-of-process dependencies, sacrifice the lightweight in-process
  developer experience and require extensive integration work before any payoff.
- **Event-sourced saga.** Rejected for v1 — valuable but orthogonal; a future addition.

## Consequences

### Positive

- Single narrative abstraction for both sync pipelines and human-in-the-loop workflows.
- Typed, testable, DI-native.
- Persistence is selected through the builder returned by `RegisterStories()`:
  `UseInMemoryStoryRepository` (default), `UseSqliteStoryRepository(...)`, or
  `UseStoryRepository<T>()` for custom backends. A repository is always registered — the
  minimum is in-memory.

### Negative / limitations (as of this ADR)

- Execution is sequential. No parallel `ReadChapter` primitive.
- No durable retry/backoff policy — chapter failures propagate immediately; for retries
  you must compose with Polly inside your chapter.
- No saga / cross-process compensation.
- No distributed tracing via `ActivitySource` yet.
- Batch-style workflows (thousands of items per run) are out of scope — context is fully
  serialized to JSON on every persist.
- Operator tooling ships as `ListAsync` only; no dashboard / CLI.

All of these are tracked in `docs/reviews/Story-Framework-Review.md` (Sections §10, §12).

## Migration

- `ChainHandler<TInput, TState, TOutput>` → `StoryHandler<TInput, TContext, TOutput>`.
- `ChainStep` → `Chapter<TContext>`.
- `Invoke<T>()` → `ReadChapter<T>()`.
- `FlowHandler` → `StoryHandler` registered via
  `services.RegisterStories().UseSqliteStoryRepository("Data Source=stories.db")`.
- Registration helper: `AddFlows` → `RegisterStories`.

Chain and Flow are not removed yet; consumers migrate on their own schedule. A subsequent
release will mark them `[Obsolete]`.

## Testing

- 100+ unit/integration tests across nine fixtures cover chapter execution, error
  aggregation, interactive pause/resume, persistence round-trips (InMemory + SQLite),
  controller endpoints, cancellation, idempotency and listing. Tests run on CI for every
  pull request.
- Claims from the previous revision of this ADR about specific coverage percentages,
  "2% overhead", "100 concurrent stories benchmark", and named DreamTravel migration
  numbers were not reproducible and have been removed.

## Open work

- OpenTelemetry `ActivitySource` + per-chapter metrics.
- SQLite health check integration.
- Encryption-at-rest hook.
- Pluggable authorization in `StoryController` (attribute-driven, per-handler).
- `[Obsolete]` markers on `ChainHandler` / `FlowHandler`.
- Benchmark suite (BenchmarkDotNet).

## Future extensions

### Handler versioning (deferred)

The framework intentionally **does not** version handlers today. A previous revision
shipped `[StoryVersion("X")]` + `StoryVersionMismatchError` with strict string equality on
load. That implementation was removed because it forced an "all in-flight stories die on
every deploy" failure mode — equivalent to bumping a REST API's major version on every
PATCH change. It contradicts how every healthy public contract is evolved.

The intended replacement, when needed, is a **SemVer-based compatibility check** modelled
on how API versioning is treated in the wider ecosystem:

- A handler may declare `[StoryVersion("MAJOR.MINOR.PATCH")]`.
- On resume, persisted `MAJOR` must match current `MAJOR`. `MINOR` and `PATCH` differences
  are treated as backward-compatible and accepted silently.
- `MAJOR` mismatch produces a typed `StoryVersionMismatchError`; the operator decides
  whether to migrate, cancel, or force-complete.
- A `StoryOptions.VersionCompatibility` enum exposes the policy:
  - `None` (default until this is implemented) — no comparison at all,
  - `SemverMajor` — the rule above,
  - `StrictExact` — full string equality (legacy paranoid mode).
- Handlers without `[StoryVersion]` are treated as version `null`. `null ↔ null` matches;
  `null ↔ X` fails to surface "you started versioning halfway through, decide what to do
  with the legacy rows".

Compatibility table for `SemverMajor` mode:

| persisted | current | result |
|---|---|---|
| `1.0.0` | `1.0.1` | ✅ compatible (patch) |
| `1.0.0` | `1.4.2` | ✅ compatible (minor) |
| `1.0.0` | `2.0.0` | ❌ `StoryVersionMismatchError` |
| `1.0.0` | `1.0.0` | ✅ identical |
| `null`  | `null`  | ✅ both unversioned |
| `null`  | `1.0.0` | ❌ asymmetric (decision needed) |
| `1.0.0` | `null`  | ❌ asymmetric (decision needed) |

Bump guide for handler authors:

| change | bump |
|---|---|
| Append a chapter at the end of `TellStory()` | `MINOR` |
| Add a nullable property to `Context` | `MINOR` |
| Pure refactor / bugfix without shape change | `PATCH` |
| Insert / reorder / remove a chapter | `MAJOR` |
| Remove or rename a `Context` field | `MAJOR` |
| Change shape of an `InteractiveChapter`'s `TChapterInput` | `MAJOR` |

Rule of thumb: **append-only and nullable** = minor; **modifying anything that persisted
state already depends on** = major. This puts the "is this breaking?" decision in the same
hands and the same mental model as REST API evolution.

Acceptance criteria for re-introducing versioning (in priority order):

1. Persisted `HandlerVersion` column round-trips through `IStoryRepository` (currently
   absent — would re-add `HandlerVersion` to `StoryInstance`, the SQLite schema, and the
   migration code path).
2. Small `SemanticVersion` type with parse + `IsBackwardCompatibleWith(other, mode)`.
3. Engine consults `_options.VersionCompatibility` before deciding to fail.
4. Single dense parameterized test driving the table above + one round-trip showing
   minor-bump-mid-flight resumes successfully.
5. `docs/Story.md` and the package README regain a "Versioning" section that points here.

Until the above is in, deploy discipline is the developer's responsibility: keep changes
to active handlers backward-compatible (additive, append-only, nullable). For
deploy-then-drain workflows where in-flight stories don't outlive a deploy this is a
non-issue; the feature exists strictly for long-lived workflows.

## References

- Framework review: `docs/reviews/Story-Framework-Review.md`
- User guide: `docs/Story.md`
- Package README: `src/SolTechnology.Core.Story/README.md`
- Tale Code philosophy: `docs/Tale.md`

