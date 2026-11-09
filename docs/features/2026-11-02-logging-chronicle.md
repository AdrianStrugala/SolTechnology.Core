---
status: planned
created: 2026-11-02
completed:
---

# Chronicle — per-operation step reports in SolTechnology.Core.Logging

> Historical delivery record. It may not describe the current system.

## Goal

Ship **Chronicle** in `SolTechnology.Core` 2.0. Chronicle records a bounded, hierarchical tree of
named steps for every root operation and emits **one** log entry per root when it completes. The
root can be an HTTP request, a CQRS command outside a request, a Hangfire job, a MessageBus
message, or a manual `Begin("ImportCities")`. Success means:

- `IChronicle` replaces `ITimingService` (removed in 2.0, user decision 2026-11-02).
- The tree is correct under parallel async branches (`Task.WhenAll`), inside and outside HTTP
  requests.
- CQRS handlers, Tale chapters, `SolTechnology.Core.HTTP` calls, and SqlClient commands appear as
  steps without user code. Hangfire jobs, MessageBus messages, and fire-and-forget CQRS events
  each produce their own root.
- The report detects N+1 patterns, escalates its level by outcome and duration, attaches buffered
  Debug/Trace entries only on failure, samples fast successes, and surfaces steps as `Activity`
  spans. No new NuGet dependency is added to any `src/` package.
- When Chronicle is disabled, a step costs one `AsyncLocal` read and one
  `ActivitySource.HasListeners()` check, with zero allocations.

## Context

### Clean-room statement

The idea comes from an internal company library: named scopes accumulated in the async call
context and emitted once as a "timing report" at request end. Chronicle is designed and
implemented **from scratch**. No proprietary source, binary, decompiled code, package, or
repository of that library is opened, copied, decompiled, or referenced. Only the concept
(ambient scopes plus one report per operation) is reused. This repository names none of that
library's identifiers, packages, or repositories. Reviewers reject any PR that cites, pastes, or
paraphrases that code. Every Chronicle PR description attests that no such material was opened,
and feature acceptance includes a grep for decompiler traces.

### Decisions made by the user (2026-11-02, do not re-ask)

| ID | Decision |
|---|---|
| U1 | Name: **Chronicle** (tale → chapters → chronicle). |
| U2 | Ships in 2.0, before [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md). |
| U3 | Replace `ITimingService` with Chronicle (breaking, allowed in 2.0). Every public symbol change goes through one `CLAUDE.md §2` confirmation round (table `CH-nn` below). |
| U4 | Core: a hierarchical tree of steps (not a flat list). Each step has a name, start offset, duration, status (`Ok`/`Failed`/`Cancelled`), and attributes. The tree is correct under parallel async forks (`AsyncLocal` semantics, thread-safe appends) and works with and without HTTP (manual `Begin`, background jobs, message handlers, tests). |
| U5 | Emission: one entry per root. The message holds a human-readable tree; a structured payload serves KQL, Serilog, and OTel. Size is bounded (steps, depth, attribute length) with truncation markers. PII is masked through `[Masked]`/`PiiMask`. `[LoggerMessage]` source generation with stable EventIds; Guide §11 `[{Placeholder}]` brackets. |
| U6 | Auto-steps: CQRS handlers, Tale chapters, HTTP client calls, SQL queries. Hangfire jobs and MessageBus handlers each produce their own root. |
| U7 | Extras, all selected: (1) N+1 detection, (2) level escalation, (3) flight recorder, (4) sampling, (5) OpenTelemetry Activities (BCL `System.Diagnostics` only). |
| U8 | Performance: near-zero cost when disabled; allocation-conscious hot path; `TimeProvider`; options validated on start. |

### Current state (verified 2026-11-02 against `master@a0a9bbc6`; re-verified by the plan review at `497b21f3`)

- `src/SolTechnology.Core.Logging/ITimingService.cs` is public: `StartContext(string)`,
  `GetTimings()`, `Reset()`.
- `TimingService.cs` is internal, scoped (`ModuleInstaller.cs:29`, registered only by the
  `Action` overload), and stores a flat `ConcurrentDictionary<string,long>` of summed
  milliseconds through `TimeProvider`. It is not `AsyncLocal`, although the XML doc says so
  (fixed by `2026-11-02-core-2-0-logging` step "Logging — sanitize inbound correlation IDs; safe timings and
  masking").
- `Middleware/LoggingMiddleware.cs:33-38` injects `ITimingService`, calls `Reset()` (`:58`), and
  emits `{@Timings}` in the "Finished request" entry (`:128-149`). The public `InvokeAsync`
  signature contains `ITimingService`.
- `OperationLogMessages.cs` has `[LoggerMessage]` EventIds 2137–2140.
  `Operations/CoreLoggingActivitySources.cs:29,32` holds `OperationsName` (const) and
  `Operations`, which `2026-11-02-core-2-0-logging` step "Logging — public surface cleanup (`LoggerExtensions`,
  internal `Operations`)" makes internal.
- `CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:28` starts an Activity per request. The same
  logging step moves it to a CQRS-owned `ActivitySource` named `OperationsName` (RQ9). `[LogScope]`
  and `[Masked]` project request properties into the scope and Activity tags (`:36-44`).
- `Correlations/CorrelationIdService.cs:5` is the precedent for ambient state:
  `private static readonly AsyncLocal<CorrelationId?>` inside an internal type behind a DI
  interface.
- Package edges (`src/*/*.csproj`): CQRS, HTTP, Api, MessageBus, and Hangfire reference Logging.
  Tale references only Core and AUID (it already has `FrameworkReference Microsoft.AspNetCore.App`).
  SQL references only Core. Logging references no `SolTechnology.*` project and has
  `FrameworkReference Microsoft.AspNetCore.App`.
- Tale emits no Activity. Chapters execute in `Orchestration/TaleEngine.cs:199-260`
  (`ExecuteChapter`); `2026-11-02-core-2-0-tale` step "Guide conformance — Tale engine split"
  moves this into `TaleStepInterpreter.cs`.
- HTTP: `ModuleInstaller.cs:160-183` adds `CorrelationPropagatingHandler` and then
  `AddResilienceHandler`. `2026-11-02-core-2-0-http` step "Guide conformance — HTTP" moves the retry options
  into `RetryStrategyOptionsBuilder.cs`.
- SQL: `ISQLConnectionFactory.CreateConnection()` returns `IDbConnection` backed by
  `Microsoft.Data.SqlClient` 7.0.1. SqlClient raises `DiagnosticListener`
  `"SqlClientDiagnosticListener"` events `Microsoft.Data.SqlClient.WriteCommandBefore`,
  `WriteCommandAfter`, and `WriteCommandError`. The plan review verified this on 2026-11-02 in
  the SqlClient `v7.0.1` tag source (`Diagnostics/SqlDiagnosticListener.cs`,
  `Diagnostics/SqlClientCommandBefore.cs`): the listener is named `SqlClientDiagnosticListener`;
  each write is guarded by the single-argument `IsEnabled(eventName)`; and the payloads are the
  public sealed types `SqlClientCommandBefore`/`After`/`Error`. These types implement
  `IReadOnlyList<KeyValuePair<string, object>>` and carry `OperationId`, `Command`, and
  `Exception` for `Error`. Step 07's integration test remains the binding evidence.
- MessageBus: `Receive/MessageBusReceiver.cs:67` `HandleMessageAsync` per message (internal after
  `2026-11-02-core-2-0-messagebus` step "MessageBus — internal implementation types"). Hangfire: `Filters/CorrelationIdJobFilter.cs` (AsyncLocal capture and restore
  per `2026-11-02-core-2-0-hangfire` step "Hangfire — restore ambient correlation state; async,
  cancellable event dispatch"), installed by `UseSolFilters` (`Hangfire/ModuleInstaller.cs:82`).
- `ITimingService` call sites outside Logging: only
  `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/FindCityByName/FindCityByNameHandler.cs:14,22,27`.
- DreamTravel uses MEL plus the OpenTelemetry logging provider (`DreamTravel.ServiceDefaults/Extensions.cs:46`)
  and subscribes to `OperationsName` (`DreamTravel.Api/Program.cs:45-46`). It does not use Serilog.
- No `docs/architecture/logging.md` exists yet. The `2026-11-02-core-2-0-logging` step "Retrospective" creates it.
  `docs/architecture/infrastructure-modules.md` does not exist either; the first of the
  `2026-11-02-core-2-0-sql`, `-blob`, or `-messagebus` retrospectives creates it.
- CQRS fire-and-forget events: `Internal/InMemoryEventPublisher.cs` dispatches through `Task.Run`,
  which flows the caller's `ExecutionContext` (and therefore any ambient Chronicle node) into a
  dispatch that usually outlives the publishing request. `Internal/EventDispatcher.cs` invokes
  each `IEventHandler<>` sequentially with per-handler error isolation.

### Design alternatives (blue-red-team)

Choices B1–B3 change module boundaries and the public API shape, so they get full blue/red
analysis. B4–B11 are compact. Each recommendation is the planned default. The plan-reviewer and
the premortem may overturn it.

#### B1 — Where Chronicle lives

**Proposal.** Implement Chronicle inside `src/SolTechnology.Core.Logging/Chronicles/` (namespace
`SolTechnology.Core.Logging.Chronicles`). No new package and no new top-level folder.

**Blue team.**

1. Logging already owns the operation-lifecycle shapes: Guide §11 says reusable lifecycle
   emitters live in Logging. It also owns `OperationLogMessages.cs` (EventIds 2137–2140),
   `OperationsName`, `PiiMask`/`[Masked]`, the `TimeProvider` registration, and the middleware
   that writes the per-request finish entry. Chronicle replaces `ITimingService`, which also
   lives there.
2. Four of the six integrations (CQRS, HTTP, MessageBus, Hangfire) already reference Logging, so
   they need no new package edge.
3. Avoids a `CLAUDE.md §2` new-folder confirmation, and gives the release feature no new package
   ID to put under the nuget.org organization.

**Red team.**

1. Logging carries `FrameworkReference Microsoft.AspNetCore.App`, so non-web hosts (workers)
   inherit the shared framework. This already happens through CQRS → Logging; the split is out of
   scope (flagship "Out of scope", `cqrs-tale#18`).
2. SQL and Tale cannot call Chronicle without a new edge.
3. The base package `SolTechnology.Core` is zero-dependency after the flagship step "Packaging — a truly
   zero-dependency `SolTechnology.Core`". It could host
   a BCL-only recorder that every module already references.

**Alternatives.**

- (b) A new package `SolTechnology.Core.Chronicle`: needs `§2` confirmation for a new top-level
  folder, adds a package ID to the release, and creates a third package in every graph.
- (c) The recorder in `SolTechnology.Core` and the emission in Logging: one feature split across
  two packages, and the base package gains ambient state and options.

**Cruxes.**

1. Does SQL need an edge? No. SqlClient diagnostics can be observed from Logging (B3).
2. Does Tale → Logging cost anything? Tale already has the same `FrameworkReference` and Logging
   has no external `PackageReference`, so the only cost is the nuspec dependency edge. Lockstep is
   enforced by the flagship step "Bump every package to 2.0.0" (`SOLCORE001`).

**Recommendation.** Proceed with (a). Logging is the natural owner, and no dependency forces
another location.

#### B2 — Public API shape

**Proposal.**

- A DI-resolved singleton `IChronicle` with `Begin(name)`, `Step(name)`, `Current`, and
  `IsActive`.
- These return a `readonly struct ChronicleStep : IDisposable` with `Set`, `SetMasked`, `Fail`,
  and `Cancel`.
- The ambient tree lives in an internal `private static readonly AsyncLocal<ChronicleNode?>`,
  following the `CorrelationIdService.cs:5` precedent.

**Blue team.**

1. It matches the `ICorrelationIdService` DI pattern and the Guide §2 installer model. Options and
   `TimeProvider` are injected, so there is no static configuration.
2. Guide §9.6 forbids public static mutable state. The `AsyncLocal` stays private, inside an
   internal type, as in `CorrelationIdService`.
3. `using var step = chronicle.Step("ImportCities");` reads like prose (Tale Code). A struct
   handle in a `using` does not box, so the disabled path allocates nothing.
4. Tests use the real recorder with `FakeTimeProvider` (already referenced by Logging.Tests at
   `10.7.0`) and need no mocks.

**Red team.**

1. Domain code and static helpers must inject `IChronicle`, which counts toward the ≤5
   constructor-dependency budget (Guide §9.4). A static `Chronicle.Step()` facade
   is more ergonomic.
2. Struct handles can be copied and disposed twice. Node close must be idempotent.
3. `Current` lets deep code add attributes to an ancestor step (intended for HTTP retries, but
   open to misuse).

**Alternatives.**

- (b) A static ambient facade: hidden dependency, and global options needed before DI exists.
- (c) Both: two ways to do one thing (Guide §1.2 simplicity).

**Crux.** Does any target call site lack DI? Every integration is DI-resolved: the pipeline
behavior, the handler, the receiver, the Hangfire filter through `UseSolFilters(sp)`, and the Tale
engine through the run's `IServiceProvider`. So does the only external call site (DreamTravel
`FindCityByNameHandler`). There is no evidence for a static need.

**Recommendation.** Proceed with (a), DI-only. Node close is idempotent through `Interlocked`.

#### B3 — Integration seams without dependency cycles

**Proposal.**

- Modules that reference Logging (CQRS, HTTP, MessageBus, Hangfire) call `IChronicle` directly.
- Tale gains a `ProjectReference` to Logging (CP-01).
- SQL commands are captured by a Logging-owned observer of SqlClient's `DiagnosticListener`
  (`"SqlClientDiagnosticListener"`, `Microsoft.Data.SqlClient.WriteCommandBefore`/`After`/`Error`).
  `SolTechnology.Core.SQL` code does not change.
- Logging references no `SolTechnology.*` project, so no cycle can form; acceptance includes a
  grep.

**Blue team.**

1. One recording mechanism for code this repository owns.
2. The SQL observer captures every SqlClient command: Dapper over `ISQLConnectionFactory`, EF Core,
   and raw `SqlConnection`. It needs no `IDbConnection` wrapper, which would break `SqlBulkCopy`
   casts and provider-specific APIs.
3. SQL stays free of `Microsoft.AspNetCore.App`.
4. Tale already has the shared framework, so the new edge adds no external package.

**Red team.**

1. Tale → Logging is a new package edge (CP-01) and couples the orchestration package to logging.
2. The SQL observer depends on SqlClient's event names and payload keys (`OperationId`,
   `Command`, `Exception`), which are not checked at compile time. *Plan review:* the 7.x payloads
   implement `IReadOnlyList<KeyValuePair<string, object>>`, and `Command` (a `SqlCommand`) is read
   as the BCL `IDbCommand`, so step 07 needs no reflection and the observer stays trimming-safe.
3. Command text can carry PII literals, so capturing it is off by default.

**Alternatives.**

- (a) An Activity bridge: Tale emits Activities on `OperationsName` (RQ9-style name sharing), and
  a Chronicle `ActivityListener` converts foreign Activities into steps. This avoids the Tale edge.
  But Chronicle must sample those Activities whenever a root is active. That changes
  `Activity.Current`, and the parent span ID in outgoing `traceparent`, in hosts without
  OpenTelemetry, and it adds a second recording mechanism.
- (b) An `IDbConnection` wrapper in SQL plus a SQL → Logging edge: drags ASP.NET Core into SQL
  consumers and misses EF Core and raw `SqlConnection`.
- (c) The Chronicle API in `SolTechnology.Core`: see B1 (c).

**Cruxes.**

1. Does SqlClient 7.0.1 still raise these events? Step 07's `SQLFixture` MSSQL integration test
   settles it. If it fails, step 07 is set `blocked` and alternative (b) goes back to the user.
2. Does any Tale consumer object to the Logging dependency? No evidence: DreamTravel uses both.

**Recommendation.** Proceed with conditions: step 07's integration test must pass against
`Microsoft.Data.SqlClient` 7.0.1.

#### B4–B11 — Other hard-to-reverse choices

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| B4 | Flight-recorder capture | (a) an `ILoggerProvider` with `[ProviderAlias("ChronicleFlightRecorder")]`, plus default `LoggerFilterOptions` rules (Debug for all categories; Warning for `Microsoft` and `System`) added only when no rule already targets the alias; (b) an `ILoggerFactory` decorator in DI; (c) a Serilog sink | **(a).** It is the canonical MEL extension point and composes with Console, OTel, and `builder.Logging.AddSerilog()`. It records only while a root is active (`IsEnabled` checks the ambient root), so it costs nothing outside roots. Crux: `UseSerilog()` replaces `ILoggerFactory`, so providers receive nothing unless `writeToProviders: true` is set; this is documented in `Log.md`. (b) depends on registration order relative to `UseSerilog` and misses factories created outside DI. (c) adds an external dependency (`§2`, `nuget-stats.json`). |
| B5 | Middleware finish entry | (a) For HTTP roots the Chronicle report **is** the finish entry: EventId 2155, `Finished request [..] -> [..] in [..]` prefix and the `RequestMethod`/`RequestPath`/`StatusCode`/`ElapsedMs` properties kept, `{@Timings}` removed; (b) keep "Finished request" and add a separate report | **(a)**, per U5 ("one entry per root"). Existing KQL on the four properties keeps working. P-15 (one Error entry per failure, `2026-11-02-core-2-0-logging` step "Logging — segment-aware path matching; one Error entry per failure") holds: the report replaces the "failed after" entry. With `Enabled = false` the middleware keeps the entries defined by that logging step. Log contract changes LC-01/LC-02. |
| B6 | Structured payload | (a) `[LoggerMessage]` with scalar placeholders plus `[{@Chronicle}]`, where `ChronicleReport.ToString()` renders the ASCII tree and Serilog destructures the public read-only model; (b) a hand-written `IReadOnlyList<KeyValuePair<string, object?>>` state with a JSON `Chronicle` property that does not appear in the message | **(a)**, because U5 mandates source generation. Limitation: OTel and App Insights receive `Chronicle` as tree text, and KQL filters on the scalar placeholders. (b) becomes a follow-up if per-step KQL is needed. Crux settled by the plan review: the generator has accepted `{@Name}` templates since .NET 8 (dotnet/runtime#69490, closed in milestone 8.0.0), so `[{@Chronicle}]` is kept. If step 04 still observes `SYSLIB1014`, use `[{Chronicle}]`, record a deviation, and document that Serilog then stores the tree as a string. |
| B7 | OpenTelemetry mapping | (a) Chronicle steps start Activities on the shared `OperationsName` source, only when it has listeners, and CQRS drops its step-52 `ActivitySource` because the Chronicle step now produces that Activity; (b) a new source `SolTechnology.Core.Chronicle`; (c) no Activities | **(a).** Consumers' existing `AddSource(OperationsName)` keeps working, with no duplicate CQRS span. HTTP steps are logical spans around HttpClient's physical spans; documented. Opt-out: `EmitActivities = false`. The HTTP request root never starts an Activity, because ASP.NET Core already has one. |
| B8 | Sampling | (a) Tail-based at root end: always emit when the status is not `Ok`, the level is ≥ Warning, or N+1 is suspected; otherwise emit when `Random.Shared.NextDouble() < SampleRate`; (b) head-based at `Begin` | **(a).** Head-based sampling cannot keep failures. Default `SampleRate = 1.0`, so nothing is dropped unless the user opts in (LC-04). Activities are not affected by Chronicle sampling. |
| B9 | Nested `Begin` | (a) `Begin` joins the open ambient root as a child, and starts a new root when there is none or the ambient root is already closed (fire-and-forget continuations); (b) `Begin` always starts a new root | **(a).** It keeps one report per request when CQRS runs inside a request, and gives CQRS-only workers a root per top-level command. A `Step` under an already closed parent step, while the root is still open, is recorded and counted (`LateStepCount`, shown as `+{n} late steps`). A `Step` after its root has closed cannot appear in any report; it is dropped without allocation (plan review: the original "count on the closed root" was always 0 in the emitted report). |
| B10 | `ITimingService` | Replace and delete (U3) | `StartContext("db")` → `chronicle.Step("db")`; `GetTimings()` → the report; `Reset()` → not needed (one root per request). The internal `TimingService` and its tests are deleted in step 09. |
| B11 | Fire-and-forget and job/message roots | (a) NEW `IChronicle.BeginRoot(name)` always starts a new root, whatever the ambient node, and restores the previous ambient node on `Dispose`; used by `InMemoryEventPublisher`, MessageBus, and Hangfire; (b) `ExecutionContext.SuppressFlow()` around `Task.Run` in `InMemoryEventPublisher`; (c) rely on `Begin` (B9) only | **(a)**, step 08. (b) also stops `CorrelationId` and `Activity.Current` from flowing, which breaks correlation. (c) makes a fire-and-forget event a child of the publishing handler's step, which usually closes first: the event is reported as `(unfinished)`/late in the request report and gets no report of its own. The detached root carries `DetachedFrom` (the ambient root's name) and the same `TraceId`, so reports stay linkable. Adds `CH-11`. |

### Behaviour and defaults (planned)

| Option (`ChronicleOptions`, section `Logging:Core:Chronicle`) | Default | Validation | Step |
|---|---|---|---|
| `Enabled` | `true` | — | 01 |
| `MaxSteps` | `256` | 1–10000 | 02 |
| `MaxDepth` | `12` | 1–64 | 02 |
| `MaxAttributesPerStep` | `8` | 0–64 | 02 |
| `MaxAttributeValueLength` | `200` | 16–4096 | 02 |
| `AggregateThreshold` | `3` | ≥ 2 | 03 |
| `NPlusOneThreshold` | `10` | ≥ `AggregateThreshold` | 03 |
| `SlowThreshold` | `00:00:01` | > 0 | 04 |
| `SampleRate` | `1.0` | 0.0–1.0 | 04 |
| `EmitActivities` | `true` | — | 05 |
| `FlightRecorder.Enabled` | `false` (opt-in: buffering formats every Debug entry inside a root) | — | 06 |
| `FlightRecorder.MinimumLevel` | `Debug` | `Trace` or `Debug` | 06 |
| `FlightRecorder.MaxEntries` | `100` | 1–1000 | 06 |
| `FlightRecorder.MaxMessageLength` | `500` | 32–4096 | 06 |
| `CaptureSQLCommands` | `true` | — | 07 |
| `CaptureSQLCommandText` | `false` (PII) | — | 07 |

Rendering and levels:

- **Level:** `Failed` → Error; `Cancelled` → Warning; `Ok` and duration ≥ `SlowThreshold` →
  Warning; HTTP 4xx → Warning (as today); otherwise Information.
- **Flight recorder:** attached only when the root is `Failed`.
- **Tree:** ASCII only (`|-`, `` `- ``, `x42`) so that every sink and encoding renders it.
- **Child order:** siblings are ordered by start offset.
- **Aggregation:** siblings with the same group key (name, plus a statement hash for SQL) and
  count ≥ `AggregateThreshold` collapse into one line: `x{count}, total, max, failed count`.
- **N+1 hint:** a count ≥ `NPlusOneThreshold` adds `N+1 suspected` and sets
  `IsNPlusOneSuspected = true`.
- **Truncation markers:** `[truncated]` (attribute value or failure message), `+{n} steps dropped` (`MaxSteps`;
  dropped steps still feed their parent's per-group aggregate), `+{n} deeper steps` (`MaxDepth`),
  `+{n} attributes dropped` (`MaxAttributesPerStep`), `+{n} nested` (children of an aggregated
  line), `+{n} late steps` (steps started under an already closed parent step, B9), and
  `(unfinished)` (a step still open when the root ends).
- **Logger categories:** HTTP roots log under the existing `LoggingMiddleware` category, so the
  existing level configuration keeps working. Other roots log under
  `SolTechnology.Core.Logging.Chronicle`.

### Public API changes requiring `CLAUDE.md §2` confirmation

Namespace `SolTechnology.Core.Logging.Chronicles` unless stated otherwise. The user confirmed
`CH-01`…`CH-11` in one `CLAUDE.md §2` round on 2026-11-02 (OQ-1).

| ID | Package | Exact change | Step |
|---|---|---|---|
| CH-01 | Logging | NEW `public interface IChronicle` { `ChronicleStep Begin(string name)`; `ChronicleStep Step(string name)`; `ChronicleStep Current { get; }`; `bool IsActive { get; }` } | 01 |
| CH-02 | Logging | NEW `public readonly struct ChronicleStep : IDisposable` { `bool IsRecording { get; }`; `ChronicleStep Set(string key, object? value)`; `ChronicleStep SetMasked(string key, string? value, MaskMode mode = MaskMode.Full, int keepChars = 3)`; `void Fail(Exception exception)`; `void Fail(string errorType, string? message = null)`; `void Cancel()`; `void Dispose()` } | 01 |
| CH-03 | Logging | NEW `public enum ChronicleStepStatus { Ok, Failed, Cancelled }` | 01 |
| CH-04 | Logging | NEW `public sealed class ChronicleOptions` with `public const string SectionName = "Logging:Core:Chronicle"` and the `{ get; set; }` properties in the defaults table (`Enabled`, `MaxSteps`, `MaxDepth`, `MaxAttributesPerStep`, `MaxAttributeValueLength`, `AggregateThreshold`, `NPlusOneThreshold`, `SlowThreshold`, `SampleRate`, `EmitActivities`, `FlightRecorder`, `CaptureSQLCommands`, `CaptureSQLCommandText`) | 01–07 |
| CH-05 | Logging | NEW `public sealed class ChronicleFlightRecorderOptions` { `bool Enabled`; `LogLevel MinimumLevel`; `int MaxEntries`; `int MaxMessageLength` } | 06 |
| CH-06 | Logging | NEW read-only report model: `public sealed class ChronicleReport` { `string Name`; `ChronicleStepStatus Status`; `DateTimeOffset StartedAt`; `long DurationMs`; `string? TraceId`; `IReadOnlyList<ChronicleReportStep> Steps`; `int StepCount`; `int DroppedStepCount`; `int LateStepCount`; `bool IsNPlusOneSuspected`; `IReadOnlyList<ChronicleFlightRecord> FlightRecords`; `override string ToString()` (ASCII tree) }; `public sealed class ChronicleReportStep` { `string Name`; `long OffsetMs`; `long DurationMs`; `ChronicleStepStatus Status`; `int Count`; `int FailedCount`; `bool IsUnfinished`; `bool IsNPlusOneSuspected`; `IReadOnlyDictionary<string, string> Attributes`; `IReadOnlyList<ChronicleReportStep> Children` }; `public sealed class ChronicleFlightRecord` { `long OffsetMs`; `LogLevel Level`; `string Category`; `int EventId`; `string Message`; `string? ExceptionType` } | 03 (report, step), 06 (`FlightRecords`, `ChronicleFlightRecord`) |
| CH-07 | Logging | NEW members of the `ModuleInstaller` `extension(IServiceCollection services)` block: `AddSolChronicle(Action<ChronicleOptions>? configure = null)` and `AddSolChronicle(IConfiguration configuration)` (binds `ChronicleOptions.SectionName`). Both `AddSolLogging` overloads call them. | 04 |
| CH-08 | Logging | DELETE `public interface ITimingService` (`StartContext`, `GetTimings`, `Reset`). The internal `TimingService` is deleted without confirmation. | 09 |
| CH-09 | Logging | `LoggingMiddleware.InvokeAsync(HttpContext, ILogger<LoggingMiddleware>, ICorrelationIdService, IEnumerable<ILogScopeEnricher>, ITimingService)` → last parameter `IChronicle chronicle` | 09 |
| CH-10 | CQRS | `LoggingPipelineBehavior<TRequest, TResponse>` constructor `(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)` → `(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger, IChronicle chronicle)` | 10 |
| CH-11 | Logging | NEW `IChronicle` member `ChronicleStep BeginRoot(string name)`: always starts a new root and restores the previous ambient node on `Dispose` (B11) | 08 |

**Not changed:** `ValueStopwatch`, `CoreLoggingActivitySources.OperationsName` (its XML doc now
also names Chronicle steps), `LoggerExtensions` (flagship C-20), and the EventIds 2137–2140.
Integration types are internal: `ChronicleHTTPHandler`, `ChronicleHTTPAttemptHandler`,
`ChronicleJobFilter`, the SqlClient observer, the flight-recorder provider, and the reporter.
`EventDispatcher` and `InMemoryEventPublisher` stay internal; their constructors gain an optional
`IChronicle? chronicle = null` without confirmation.

### Consumer-visible package and behaviour changes (premortem focus)

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| CP-01 | `SolTechnology.Core.Tale` depends on `SolTechnology.Core.Logging` (new nuspec edge) | Tale consumers restore Logging. No new framework: both packages already have `Microsoft.AspNetCore.App`. | 11 |
| CP-02 | `OperationsName` spans now include manual steps, HTTP calls, Tale chapters, and job/message roots | More spans for consumers already subscribed. Opt-out: `EmitActivities = false`. | 05, 08, 10–14 |
| CP-03 | `AddSolCQRS` (when `UseLogging`), `AddSolTale`, `AddSolHTTPClient`, `AddSolMessageBus`, `AddSolPersistentEvents`, and `AddSolRecurringJob` call `AddSolChronicle()` | `ChronicleOptions` validated on start in those hosts | 04, 10–14 |
| CP-04 | Logging subscribes to `SqlClientDiagnosticListener` when `CaptureSQLCommands` is `true` | SqlClient writes diagnostic payloads only while a root is active (`IsEnabled` predicate) | 07 |
| CP-05 | A flight-recorder `ILoggerProvider` is registered; `IsEnabled(Debug)` returns `true` inside roots when `FlightRecorder.Enabled` | Debug templates are formatted inside roots only when opted in | 06 |
| CP-06 | Fire-and-forget CQRS events dispatched by the default `InMemoryEventPublisher` each produce their own root (`Event {EventType}`, EventId 2154) when `IChronicle` is registered | One extra 2154 entry per published event (subject to sampling) | 10 |

### Log contract changes (Guide §15 "report only", accepted by the user 2026-11-02, OQ-2)

- **LC-01** — HTTP finish entry. EventId **2155** `RequestChronicleReported`, template
  `Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]. Chronicle: [{ChronicleStatus}] [{@Chronicle}]`.
  `{@Timings}` is removed. A successful request slower than `SlowThreshold` logs at Warning
  (Information before). Step 09.
- **LC-02** — A failed request logs **one** Error entry (EventId 2155, exception attached,
  `ChronicleStatus = Failed`), which replaces "Request [..] failed after [..]" and keeps P-15. A
  client abort logs Warning 2155 with `ChronicleStatus = Cancelled`, which replaces "aborted by
  client". With `Enabled = false`, the entries defined by `2026-11-02-core-2-0-logging` step "Logging — segment-aware path
  matching; one Error entry per failure" remain. Step 09.
- **LC-03** — NEW EventId **2154** `ChronicleReported` per non-HTTP root, template
  `Chronicle [{ChronicleName}] [{ChronicleStatus}] in [{DurationMs} ms], [{StepCount}] steps: [{@Chronicle}]`.
  Roots: CQRS commands outside requests, fire-and-forget events (CP-06), MessageBus messages,
  Hangfire jobs, and manual `Begin`/`BeginRoot`. Step 04.
- **LC-04** — With `SampleRate < 1`, fast successful roots, including the HTTP finish entry, may
  emit nothing (opt-in; the default `1.0` emits everything). Step 04.
- **LC-05** — With `FlightRecorder.Enabled`, Debug/Trace entries from inside a failed root appear
  in its report. Other providers still apply their own filters. Step 06.
- EventIds 2154/2155 sit after the ids already shipped: 2137–2148 (Logging), 2149 (CQRS
  `OperationFailedWithResult`), and 2150–2153 (Hangfire start-up logs, `2026-11-02-core-2-0-hangfire`). The plan
  reserved 2150/2151 until Hangfire, which shipped first, took them on 2026-11-09. Steps 04 and 09 check uniqueness
  with a grep.

### Open questions (resolved 2026-11-02)

| ID | Question | User answer (2026-11-02) | Steps unblocked |
|---|---|---|---|
| OQ-1 | Confirm the `CLAUDE.md §2` public API changes `CH-01`…`CH-11` (one round). | **Confirmed**: `IChronicle`, `ChronicleStep`, `ChronicleStepStatus`, `ChronicleOptions`, `ChronicleFlightRecorderOptions`, `ChronicleReport`/`ChronicleReportStep`/`ChronicleFlightRecord`, both `AddSolChronicle` overloads, DELETE `ITimingService`, `LoggingMiddleware.InvokeAsync` `ITimingService` → `IChronicle`, the `LoggingPipelineBehavior` constructor gaining `IChronicle`, and `IChronicle.BeginRoot`. | 01–10 |
| OQ-2 | Accept the Guide §15 "report only" log contract changes `LC-01`…`LC-05` for 2.0. | **Accepted**: EventId 2155 report replaces "Finished request"; one Error entry per failure; new EventId 2154 for non-HTTP roots; sampling; flight recorder. | 04, 06, 09 |

Earlier user decisions U1–U8 stand. The plan-reviewer accepted the planner decisions `CP-01`
(Tale → Logging edge, B3) and B11 (`BeginRoot`) on 2026-11-02. Neither creates a cycle, and both
add no external package.

### Cross-feature dependencies

This feature does **not** edit other features' files. The other features' planners wire the
reverse edges.

Core 2.0 is split into a foundation feature and per-library features. Their step numbers were
being renumbered while this plan was written, so every dependency below names the feature and the
step **title**, never a step number. The per-library briefs are named `<feature>.md` next to this
brief; they are referenced by name because some did not exist yet on 2026-11-02.

| Feature | Role for Chronicle |
|---|---|
| [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) | Foundation: tag-only publishing, 2.0.0 versions, zero-dependency base package, central XML-docs switch |
| `2026-11-02-core-2-0-logging` | Owns every file Chronicle builds on in Logging (registrations, middleware entries, masking, `Operations`) |
| `2026-11-02-core-2-0-cqrs` | Owns `LoggingPipelineBehavior`, dispatch, and failed-`Result` logging |
| `2026-11-02-core-2-0-tale` | Owns the Tale engine split, chapter cancellation, and history keys |
| `2026-11-02-core-2-0-http` | Owns the HTTP handler pipeline and retry options |
| `2026-11-02-core-2-0-sql` | Owns the 2.0 SQL surface and the `SQL.Testing` reference in `SQL.Tests` |
| `2026-11-02-core-2-0-messagebus` | Owns the receiver, settlement, and internal types |
| `2026-11-02-core-2-0-hangfire` | Owns the job filters, ambient-state restore, and job contracts |

**Release.** [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md)
must depend on this feature being `status: completed`. Its pre-check step ("Transfer the
repository and set up the nuget.org organization (owner runbook)") should verify that, and its
step "Release 2.0.0" takes the `CH-nn`, `CP-nn`, and `LC-nn` tables and the migration notes below
as release-notes inputs.

**Merge order.** Each Chronicle step merges only after the listed steps are `done`.

| Chronicle step | Must follow (feature — step title) | Reason |
|---|---|---|
| all | flagship-hardening — "Publish to nuget.org only from `v*` tags behind a protected environment"; flagship-hardening — "Bump every package to 2.0.0" | No 1.x package contains Chronicle or the `ITimingService` removal. |
| 01–03 | logging — "Logging — sanitize inbound correlation IDs; safe timings and masking"; logging — "Logging — public surface cleanup (`LoggerExtensions`, internal `Operations`)" | `PiiMask.Partial` semantics used by `SetMasked`; `LoggerExtensions` and internal `Operations` (C-20). These steps add new files only. |
| 04 | logging — "DI resolution test — Logging"; logging — "Logging — `AddSolLogging(IConfiguration)` registers its middleware dependencies"; logging — "Guide conformance — Logging"; cqrs — "CQRS — log failed `Result`s as failures" | `ModuleResolutionTests.cs`; one registration path; `LoggingServiceRegistrations.cs` and the 150-line `ModuleInstaller.cs`; EventId allocation after `OperationFailedWithResult`. |
| 05 | logging — "Logging — public surface cleanup (`LoggerExtensions`, internal `Operations`)" | RQ9: CQRS owns an `OperationsName` source; step 10 removes it again. |
| 06 | logging — "Guide conformance — Logging" | Registrations file. |
| 07 | flagship-hardening — "Clear NU1903 — Testcontainers and DreamTravel parent bumps, Pkcs override in SQL"; sql — "SQL — `Begin` on `IUnitOfWork`"; sql — "SQL — transient-only connection retry, safer error translation, options validation"; sql — "SQL — internal implementations; safe-by-default DACPAC deployment" | Pinned SqlClient and Pkcs; `SQL.Tests` → `SQL.Testing` reference; the integration test targets the 2.0 SQL surface. |
| 08 | none outside this feature (after Chronicle 01–05) | Logging-internal. |
| 09 | logging — "Logging — segment-aware path matching; one Error entry per failure"; logging — "Logging — sanitize inbound correlation IDs; safe timings and masking"; logging — "Guide conformance — Logging"; flagship-showcase-hygiene — "DreamTravel — mediator-only, thin controllers"; flagship-showcase-hygiene — "DreamTravel — logic and domain cleanup" | Middleware entries and P-15; that logging step's `ITimingService` snapshot edit becomes obsolete when step 09 deletes the file; `RequestEnvelopeLogger.cs` split, `TimingService` primary constructor, Logging templates; `FindCityByNameHandler.cs` overlap. |
| 10 | cqrs — "CQRS — descriptive missing-handler errors and interface-based dispatch"; cqrs — "CQRS — log failed `Result`s as failures"; cqrs — "Guide conformance — CQRS"; logging — "Logging — public surface cleanup (`LoggerExtensions`, internal `Operations`)" | Dispatch delegates; failed-`Result` logging (RM-15: type and message only); primary constructors; the CQRS `ActivitySource` this step removes. |
| 11 | tale — "Tale — per-run state isolation; persist only `TaleManager`-driven runs"; tale — "Tale — registration and schema safety, correct log scope"; tale — "Tale engine — caller-only cancellation, no resume of failed tales, deterministic tests"; tale — "Tale — `CancellationToken` on chapters"; tale — "Tale — chapter history keyed by step index and full type name"; tale — "Guide conformance — Tale engine split"; tale — "Guide conformance — Tale controller, manager, handler, `Tale`, and tests"; tale — "Log templates — `[{Placeholder}]` in Tale (L-03)" | Per-run state; caller-only cancellation decides `Cancelled`; chapter token; history key (the step name stays `ChapterId`); `TaleStepInterpreter.cs`; `extension` blocks in `ModuleInstaller.cs`; templates. |
| 12 | http — "HTTP — never retry unsafe verbs after an exception or timeout"; http — "HTTP — request pipeline correctness"; http — "HTTP — own namespace for `HttpClientExtensions`; factory-based upstream health check"; http — "HTTP — drop `Async` from `RequestBuilder` verbs (optional)"; http — "Guide conformance — HTTP"; http — "Log templates — `[{Placeholder}]` in HTTP (L-03)" | Retry predicate and handler order; namespace; `RequestBuilder` names; `RetryStrategyOptionsBuilder.cs` and installer helpers; templates. |
| 13 | messagebus — "MessageBus — resolvable broker, registered health-check client, correlation service"; messagebus — "MessageBus — receiver settlement matches its contract; publish→receive round-trip"; messagebus — "MessageBus — internal implementation types"; messagebus — "Guide conformance — MessageBus"; messagebus — "Log templates — `[{Placeholder}]` in MessageBus (L-03)" | Resolvable broker; settlement outcomes map to root status; internal receiver; conformance; templates. |
| 14 | hangfire — "Hangfire — implement `preventOverlap`; register the correlation service"; hangfire — "Hangfire — restore ambient correlation state; async, cancellable event dispatch"; hangfire — "Hangfire — recurring job contract for 2.0"; hangfire — "Guide conformance — Hangfire" | Filter registration; AsyncLocal capture and restore pattern; `IJob` contract; conformance. |
| 15 | flagship-hardening — "XML docs — one central `GenerateDocumentationFile` switch"; each library's "XML docs shipped — <Module>"; logging — "Module docs — `docs/Log.md`"; cqrs — "Module docs — `docs/CQRS.md`; failed-`Result` logging note in `docs/Log.md`"; tale — "Module docs — `docs/Tale.md` for the 2.0 Tale contract"; http — "Module docs — Clients and the HTTP production checklist"; sql — "Module docs — `docs/SQL.md`"; messagebus — "Module docs — `docs/Bus.md`"; hangfire — "Module docs — `docs/Hangfire.md`" | The module docs are rewritten for 2.0 first; this step adds Chronicle sections on top. |
| 16 | flagship-showcase-hygiene — "DreamTravel — mediator-only, thin controllers"; "DreamTravel — logic and domain cleanup"; "DreamTravel — run-in-two-minutes README and an honest architecture section" | DreamTravel configuration, README, and component-test overlap. |
| 17 (retrospective) | logging — "Retrospective"; the first of sql/blob/messagebus — "Retrospective"; flagship-hardening — "Retrospective" | `docs/architecture/logging.md` and `infrastructure-modules.md` must exist before this retrospective extends them; the foundation retrospective adds the cross-module log contract section. |

**Premortem timing:** run step 00 when the dependencies of steps 01–04 are `done`, so the evidence
is fresh.

### Release-notes inputs (consumed by the release step "Release 2.0.0")

- Migration from `ITimingService`:
  - Inject `IChronicle` instead of `ITimingService`.
  - Replace `using (timingService.StartContext("x"))` with `using (chronicle.Step("x"))`.
  - Delete `GetTimings()`/`Reset()` calls; the report is emitted automatically per root.
  - Code that starts work which outlives its caller (`Task.Run`, timers) uses
    `chronicle.BeginRoot("name")` to get its own report (B11).
  - The `{@Timings}` property in the finish entry is replaced by `{@Chronicle}` (LC-01).
- Serilog hosts that want the flight recorder: `UseSerilog(..., writeToProviders: true)` or
  `builder.Logging.AddSerilog()`. With `writeToProviders`, Serilog's own `MinimumLevel` must admit
  Debug. Serilog destructures `{@Chronicle}` as a structure capped by `MaximumDestructuringDepth`
  (default 10), not as the ASCII tree.
- `CH-nn`, `CP-nn`, and `LC-nn` tables above, as amended by `Deviations`.

## Scope

### In scope

- Chronicle core in Logging: ambient tree, struct handle, bounds, report model, ASCII renderer,
  N+1 aggregation, emission with level escalation and sampling, DI registration and validated
  options, OTel Activities, flight recorder, and SqlClient capture.
- HTTP request roots in `LoggingMiddleware`; removal of `ITimingService` and `TimingService`;
  DreamTravel call-site migration.
- Detached roots (`IChronicle.BeginRoot`, B11).
- Auto-steps and roots: CQRS (`LoggingPipelineBehavior`, event handlers, fire-and-forget event
  roots), Tale runs and chapters, HTTP client calls (one logical step per call, attempts counted),
  MessageBus messages, and Hangfire jobs (recurring jobs and persistent events).
- Documentation: `docs/Log.md` Chronicle section and migration notes; integration notes in
  `CQRS.md`, `Tale.md`, `Clients.md`, `SQL.md`, `Bus.md`, `Hangfire.md`; XML docs on every new
  public member.
- DreamTravel showcase: configuration, README sample report, and a component test.
- Architecture: the retrospective adds a Chronicle section to `docs/architecture/logging.md` and
  updates `cqrs.md`, `tale-framework.md`, `http-client.md`, `background-processing.md`, and
  `infrastructure-modules.md`.

### Out of scope

- A new package or top-level folder (B1).
- Any external NuGet dependency in `src/` (U7).
- Other database providers (Npgsql emits its own `ActivitySource`; follow-up), Blob, Cache, Redis,
  and Azure SDK auto-steps.
- A JSON-payload log state (B6 (b)), per-name slow thresholds, and trace-aligned sampling.
- Metrics or histograms derived from Chronicle.
- Report persistence or UI.
- Trimming/AOT annotations (consistent with the flagship's AOT follow-up). The SqlClient observer
  reads payloads without reflection (plan review), so it adds no new trimming debt.
- A change to the CQRS lifecycle entries 2137–2139. They stay. `Log.md` documents how to silence
  them through the category level when only Chronicle reports are wanted.
- Splitting the CQRS → Logging → ASP.NET Core dependency.

## Implementation plan

Steps live in [`2026-11-02-logging-chronicle/summary.md`](2026-11-02-logging-chronicle/summary.md).
Gates: review and premortem are both required. The `CLAUDE.md §4` premortem trigger is the
`ModuleInstaller.cs` edits: Logging (04 directly; 06, 07, 09 through
`ChronicleServiceRegistrations.cs`/`LoggingServiceRegistrations.cs`, which the installer calls),
CQRS (10), Tale (11), HTTP (12), MessageBus (13), and Hangfire (14). The log contract changes
(LC-01…LC-05), the package edge (CP-01), and the new roots (CP-06) are premortem focus items, not
separate triggers.

| Phase | Steps | Notes |
|---|---|---|
| Gate | 00 | Premortem in a session that did not author this plan, after `review: done`. |
| Core (Logging only, plumbing) | 01–08 | 01–03 add new files only. 04 wires DI and emission. 05–07 are independent extensions on top of 04. 08 adds detached roots on top of 05. |
| Roots and removal | 09 | HTTP request root, `ITimingService` deletion, and DreamTravel call-site migration in one PR, so every build stays green. |
| Integrations | 10–14 | One module per step, each with tests in its own test project. 10 depends on 05 and 08; 13 and 14 depend on 08. 11 and 12 depend on 05. |
| Documentation and showcase | 15–16 | 15 after the per-library "Module docs" steps; 16 after the showcase DreamTravel steps. |
| Close | 17 | Retrospective after the logging, infrastructure, and foundation retrospectives listed under cross-feature dependencies. |

Testing conventions for every step:

- NUnit, FluentAssertions, NSubstitute (`tests/Directory.Build.props`).
- `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing 10.7.0`, already in
  Logging.Tests and HTTP.Tests).
- Log capture through `FakeLogger`/`FakeLogCollector` from `Microsoft.Extensions.Diagnostics.Testing`.
  The version comes from the `package-management` skill; it is test-only, so no `§2` gate applies.
- Names follow `Method_Scenario_ExpectedOutcome` with `// Arrange/Act/Assert` (Guide §8).
- No new test project.
- Report assertions: `FakeLogger` stores structured-state values as strings
  (`FakeLogger.ConsumeTState`). Tests in other modules therefore assert on the rendered tree in
  `FakeLogRecord.Message`, and never cast a state value to `ChronicleReport`. Only Logging.Tests
  reads the `ChronicleReport` object, through a test-local `StateCapturingLoggerProvider` that
  keeps the original `TState`.

### Plan review (2026-11-02)

Changes applied by the plan-reviewer. Each one is reflected in the step files:

- OQ-1 and OQ-2 answered by the user. Steps 01–10 unblocked. CP-01 and B11 accepted.
- Clean room: removed every identifier, package path, and repository name of the inspiration
  library from this brief. The original grep could never pass, because one of its tokens already
  occurs in public DreamTravel configuration for an unrelated payment API. It is replaced by a
  decompiler-trace grep plus a PR attestation.
- B6 crux settled: `{@Name}` has been supported by the generator since .NET 8.
- B3 crux evidence: the SqlClient `v7.0.1` source was verified. Step 07 reads typed payloads
  through `IReadOnlyList<KeyValuePair<string, object>>` plus `IDbCommand`, with no reflection. It
  pairs steps by `OperationId`, and it gates only `Before` on the ambient root.
- `LateStepCount` (CH-06, unchanged signature) now counts steps started under an already closed
  parent step while the root is open. Counting on a closed root could never surface in a report.
- `IChronicle.Current` returns a non-owning handle (its `Dispose` is a no-op). `Dispose` never
  throws (listener failures are caught) and restores the ambient node only when that node is
  still current.
- Test plans: assertions use `FakeLogRecord.Message`; allocation tests warm up and also cover
  `Enabled = true` with no root; Hangfire ambient flow is proven through a real
  `BackgroundJobPerformer`; DreamTravel asserts an EF Core `SQL` step.
- Step 09: when `IChronicle` is replaced, the middleware falls back to the `Enabled = false`
  entries instead of logging nothing. Step 13: stop for `§2` if `MessageBusReceiver` is still
  public.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx -c Release` succeeds, and
      `cd sample-tale-code-apps/DreamTravel && dotnet build` succeeds.
- [ ] `pwsh ./.github/runTests.ps1` exits 0.
- [ ] `grep -rn "ITimingService\|TimingService" src tests sample-tale-code-apps --include=*.cs | grep -v "/bin/\|/obj/"` prints nothing, and `grep -rln "ITimingService" docs --include=*.md | grep -v "docs/features/"` prints only `docs/Log.md` (its migration subsection).
- [ ] `grep -n "ProjectReference" src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` prints nothing (no dependency cycle).
- [ ] `git diff <feature-base>..HEAD -- src/*/*.csproj | grep "^+.*PackageReference"` prints nothing (no new external dependency in `src/`).
- [ ] `grep -rnE "EventId = (2154|2155)" src --include=*.cs` lists exactly one `ChronicleReported` (2154) and one `RequestChronicleReported` (2155).
- [ ] `git grep -niE "decompil|ilspy|dotpeek" -- src tests sample-tale-code-apps docs ':!docs/features/2026-11-02-logging-chronicle*'` prints nothing, and every Chronicle PR description carries the clean-room attestation (clean room).
- [ ] `find src/SolTechnology.Core.Logging/Chronicles -name "*.cs" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Logging.Tests has these tests green:
  - `Step_ChronicleDisabled_AllocatesNothing` and `Step_EnabledNoRoot_AllocatesNothing`: 0 bytes from `GC.GetAllocatedBytesForCurrentThread()` over 1,000 steps after a warm-up, no listener.
  - `WhenAll_ParallelBranches_BuildsIsolatedSubtrees`.
  - `Report_FortyTwoIdenticalSQLSteps_AggregatesWithNPlusOneHint`.
- [ ] One root per HTTP request, CQRS-only command, fire-and-forget event, Hangfire job, and
      MessageBus message, each proven by a test in its own module's test project (steps 09, 10,
      13, 14).
- [ ] CQRS handler, Tale chapter, HTTP client call, and SqlClient command steps are proven by
      tests (steps 07, 10, 11, 12).
- [ ] `docs/Log.md` contains a `Chronicle` section; `ITimingService` appears only in its `Migrating from ITimingService (1.x)` subsection.
- [ ] DreamTravel `FindCityByName` produces one 2155 entry with nested CQRS and HTTP steps
      (component test, step 16).
- [ ] `CH-01`…`CH-11` and `LC-01`…`LC-05` were confirmed by the user on 2026-11-02 (OQ-1, OQ-2
      resolved in this brief); no further public symbol changed without a new confirmation.

## Completion summary

## Deviations

## Follow-ups
