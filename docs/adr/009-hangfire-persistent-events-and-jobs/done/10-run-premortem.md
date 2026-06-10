---
adr: 009-hangfire-persistent-events-and-jobs
step: 10 of 10
status: done
---

# Step 10: Run premortem (mandatory gate)

## Summary
**Implementation is blocked until this step returns *Go* or *Go with mitigations*.** Although it is
numbered last, this step **runs first** as a gate: before writing any production code for steps 01–09,
invoke the [`premortem`](../../../../.github/skills/premortem/SKILL.md) skill and work backward from
"this change shipped and broke production" through the module-specific failure modes below. This change
hits every trigger in the premortem skill's *mandatory* list: a public CQRS API rename, two
`ModuleInstaller.cs` surfaces, a new external dependency, and a persisted contract (events serialised
into Hangfire storage).

## Affected components
- None directly — this is a **gate**, not a code change. Output is the premortem analysis attached to
  the PR and recorded in this plan.

## Details
Run the premortem against at least these failure modes (extend as the skill directs):

- **Breaking rename blast radius.** A NuGet consumer outside this repo names `INotification` /
  `INotificationHandler<T>` and breaks on upgrade. Mitigation: ADR-009 documents the pre-1.0 break +
  semver bump; release notes call it out explicitly.
- **Dependency / CVE.** `Hangfire.Core` 1.8.22 pulls `Newtonsoft.Json` 11.0.1 (CVE-2024-21907, NU1903).
  Mitigation: the 13.0.4 pin (step 03; advisory first-fixed = 13.0.1). Premortem must confirm
  `dotnet build` shows **no** `NU1901`–`NU1904` after the pin, and that the pin propagates to consumers.
- **DI lifetime / captive dependency (review addition).** A **singleton** `IEventPublisher` that
  constructor-injects the **scoped** `IEventDispatcher` is a captive-dependency bug. Mitigation
  (steps 02/04): the singleton publisher injects only `IServiceScopeFactory` and creates a fresh scope
  per dispatch (B2). Premortem confirms no scoped service is captured by a singleton.
- **Hangfire `JobActivator` not DI-aware (review addition).** With `Hangfire.Core`'s default activator
  (`Activator.CreateInstance`), the server cannot resolve the DI-backed job target and the job throws at
  **execution** time, not publish. Mitigation: document the requirement for a DI activator
  (`Hangfire.AspNetCore`/`Hangfire.NetCore` via `AddHangfireServer`, or `UseActivator`) in step 04/07;
  DreamTravel proves it. Premortem confirms the docs state it and the sample satisfies it.
- **Interface-argument serialisation (review addition / sharpened).** `IEvent` is enqueued through an
  **interface** parameter; Newtonsoft cannot round-trip it to the concrete type unless the app's Hangfire
  serializer emits `$type` (`UseRecommendedSerializerSettings()` / `TypeNameHandling.Auto`). Without it,
  the event fails to deserialise when the job runs. Mitigation: document the serializer requirement
  (step 04/07); step 08 exercises a real round-trip under the app's recommended settings.
- **Silent no-op persistence.** `AddPersistentEvents()` is called but the app never wired
  `AddHangfireServer` / storage, so events enqueue (or throw) and never run. Mitigation: loud XML docs +
  `Hangfire.md` (step 07); fail-fast when `IBackgroundJobClient`/storage is missing.
- **At-least-once re-dispatch re-runs succeeded handlers.** With `[AutomaticRetry(Attempts = 0)]` there
  is **no automatic** retry, but a durable queue is still at-least-once: a worker killed mid-`Dispatch`,
  or a manual dashboard re-queue, re-runs every handler for that event. Mitigation: document the
  idempotency requirement on the at-least-once basis (not on automatic retry). Premortem confirms the
  docs frame it correctly.
- **No retry knob is half-promised (review addition; resolved).** The maintainer dropped configurable
  retry — events dispatch once, retry is a handler concern, and `PersistentEventsOptions` exposes **no**
  retry property. Premortem confirms no acceptance criterion, XML doc, or `Hangfire.md` example claims a
  retry knob that was deliberately not built.
- **Seam-override ordering.** `AddPersistentEvents()` must deterministically replace the in-memory
  `IEventPublisher` regardless of call order vs `AddCQRS()`. Mitigation (steps 02/04): order-independent
  `RemoveAll<IEventPublisher>()` + `Add`. Premortem confirms the in-memory publisher cannot silently
  stay active.
- **Recurring job id collisions / drift.** Two `IJob` types with the same `Name`, or an id change across
  deploys duplicating schedules. Mitigation: stable `nameof(TJob)` id + `AddOrUpdate` idempotency (step
  05). Also confirm `RecurringJobRegistrar` fails fast if `IRecurringJobManager` (i.e. `AddHangfire`) is
  absent.
- **Scheduler obsoletion breaks a build.** A hidden in-repo consumer of `AddScheduledJob<T>` turns the
  new `CS0618` into a `TreatWarningsAsErrors` failure (step 06). Premortem confirms the solution-wide
  search came back clean.
- **Rate limit scope (review addition; resolved).** The maintainer confirmed **no rate limit** for now —
  out of scope for this `Hangfire.Core`-only plugin; only app-owned concurrency (`WorkerCount` / named
  queues) applies, documented in step 07. Premortem confirms no half-built rate-limit surface ships.

## Acceptance criteria
- The `premortem` skill has been run and its verdict (*Go* / *Go with mitigations* / *No-go*) is recorded
  in this file and attached to the PR.
- Every *Go with mitigations* item has a concrete owner step (01–09) or an added follow-up.
- Implementation of steps 01–09 does **not** begin until the verdict is *Go* or *Go with mitigations*
  with mitigations in place.

## Open questions
- none — gate only. The rate-limit and retry decisions are **settled** (no rate limit; no automatic
  retry — `Attempts = 0`, retry is a handler concern); premortem only confirms they shipped as decided.

---

## Premortem — ADR-009: Persistent events and recurring jobs via `SolTechnology.Core.Hangfire`

*Run date: 2026-06-10. Gate for steps 01–09.*

### Frame

- **Modules touched**: `SolTechnology.Core.CQRS` (rename + dispatch seam), `SolTechnology.Core.Hangfire`
  (new), `SolTechnology.Core.Scheduler` (deprecated)
- **API delta**:
  - REMOVED: `INotification`, `INotificationHandler<T>` (public markers)
  - ADDED: `IEvent`, `IEventHandler<T>` (replacements), `IEventPublisher`, `IEventDispatcher` (seam),
    `IJob`, `AddPersistentEvents()`, `AddRecurringJob<TJob>(cron)`, `PersistentEventsOptions`
  - CHANGED: `IMediator.Publish` signature from `TNotification : INotification` to `TEvent : IEvent`
  - OBSOLETED: `AddScheduledJob<T>`, `ScheduledJob`
- **Semver impact**: **MAJOR** for CQRS (0.8.0 → 0.9.0 under pre-1.0); **NEW** for Hangfire (0.1.0);
  **MINOR** for Scheduler (0.5.0 → 0.6.0, additive `[Obsolete]`)
- **Consumers in workspace**: DreamTravel (5 files: `SaveCitySearchJob.cs`, `LogEventInfoJob.cs`,
  `EventPublisher.cs`, `CitySearched.cs`, `Worker/Program.cs`), CQRS.Tests (`TestFixtures.cs`)
- **External consumers**: public NuGet downloaders of `SolTechnology.Core.CQRS` — their code is
  invisible; they break silently on the rename.

### Imagined Failure

*Three months after release.* A community user upgrades `SolTechnology.Core.CQRS` from 0.8.x to 0.9.0
as a transitive pull from another SolTechnology package. Their build breaks with CS0246 (`INotification`
not found) and CS0311 (`INotificationHandler<T>` constraint mismatch). They file an issue. Meanwhile, a
DreamTravel deploy goes green but all Hangfire event jobs fail silently at execution time — the Worker
app called `AddPersistentEvents()` but forgot `UseRecommendedSerializerSettings()`, so every event
deserialises as `null`. The Hangfire dashboard shows failed jobs, but the team doesn't check it for two
weeks. Events are lost.

### Scenarios

| # | Scenario | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation |
|---|---|---|---|---|---|---|---|
| 1 | **Breaking rename breaks external NuGet consumers.** `INotification` / `INotificationHandler<T>` removed without `[Obsolete]` shim. Consumer builds fail with CS0246/CS0311. | `ICommand.cs:24` (marker removed), `INotificationHandler.cs` (renamed) | Public NuGet consumers | H | H | None — pre-1.0 policy allows this | Document in release notes + CHANGELOG; bump minor under 0.x semantics; ADR records the decision. **Accepted risk.** |
| 2 | **Interface-argument serialisation failure.** `IEvent` enqueued as interface parameter; Newtonsoft defaults (`TypeNameHandling.None`) can't round-trip to concrete type. Job fails at execution time. | `HangfireEventPublisher.cs` — `Enqueue(() => DispatchInScope(@event))` | Apps using `AddPersistentEvents()` | H | M | DreamTravel.Sql uses `UseRecommendedSerializerSettings()` (proven) | Document hard requirement in XML doc + `Hangfire.md`; step 09 tests a real round-trip; step 08 validates DreamTravel config. |
| 3 | **Hangfire `JobActivator` not DI-aware.** Default `Hangfire.Core` activator is `Activator.CreateInstance`; plugin job target has constructor dependencies → throws `MissingMethodException` at job execution. | `HangfireEventPublisher` ctor (`IServiceScopeFactory`, `IBackgroundJobClient`) | Apps not using `Hangfire.AspNetCore` | H | L | DreamTravel.Worker references `Hangfire.AspNetCore` (proven) | Document requirement in step 04/07; test (step 09) uses a mock `IBackgroundJobClient` so the CI path never hits this; DreamTravel integration proves it end-to-end. |
| 4 | **Captive dependency: singleton publisher captures scoped dispatcher.** If `HangfireEventPublisher` constructor-injects `IEventDispatcher` (scoped), scope leaks across requests. | Step 02 registration in `ModuleInstaller.cs` | Internal — test-detectable | H | L | Design decision B2 prevents it: publisher injects `IServiceScopeFactory` only | Step 02 acceptance criterion: publisher registered singleton with **only** `IServiceScopeFactory`; step 09 validates fresh scope per dispatch. |
| 5 | **Seam-override ordering: in-memory publisher stays active.** `AddCQRS()` called after `AddPersistentEvents()` — `TryAdd` re-registers the in-memory default over the Hangfire publisher. | CQRS `ModuleInstaller.cs:31` (`TryAddScoped`) | Apps calling `AddCQRS` after plugin | M | M | None yet | Step 02/04 design: `AddPersistentEvents()` uses `RemoveAll<IEventPublisher>() + Add`; CQRS uses `TryAdd` (yields to existing). Step 09 tests both orderings. |
| 6 | **CVE-2024-21907 (Newtonsoft.Json 11.0.1).** `Hangfire.Core` 1.8.22 pulls `Newtonsoft.Json` ≥ 11.0.1. Without a pin, consumers get NU1903 (HIGH). | `SolTechnology.Core.Hangfire.csproj` `<PackageReference>` | Public NuGet consumers | H | H (if pin omitted) | `TreatWarningsAsErrors=true` in `src/Directory.Build.props` would catch NU1903 as build error | Step 03: explicit `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />` pin. Build validates NU1903 is gone. |
| 7 | **Silent no-op: events enqueue but never execute.** App calls `AddPersistentEvents()` without `AddHangfireServer()` / storage. `IBackgroundJobClient.Enqueue` may throw (no storage) or silently store with no server to pick up the job. | `HangfireEventPublisher.Publish` | App with misconfigured Hangfire | M | M | None — Hangfire throws at `Enqueue` if storage is null | Document hard requirement in `Hangfire.md`; XML doc on `AddPersistentEvents` says "requires `AddHangfire` + `AddHangfireServer`". |
| 8 | **At-least-once re-dispatch re-runs succeeded handlers.** Worker killed mid-`Dispatch` → Hangfire re-executes the job → handlers that already committed their work run again. | `DispatchInScope` body (sequential handler foreach) | Apps with non-idempotent handlers | M | L | Today's fire-and-forget has the same contract; handlers are already idempotent-or-lossy | Document idempotency requirement; `Hangfire.md` states at-least-once semantics. **Accepted risk** — same contract as today. |
| 9 | **Recurring job id collision.** Two `IJob` types with the same class name in different namespaces produce the same `nameof(TJob)` id → `AddOrUpdate` overwrites one schedule with the other. | `RecurringJobRegistrar` using `nameof(TJob)` | Apps with ambiguous job names | L | L | `AddOrUpdate` is idempotent per id — collision is silent overwrite | Use `typeof(TJob).FullName` as the job id (includes namespace). Step 05 specifies this. |
| 10 | **Scheduler `[Obsolete]` breaks a hidden consumer build.** `TreatWarningsAsErrors=true` + `CS0618` = build failure. | `Scheduler/ModuleInstaller.cs` (new `[Obsolete]` attribute) | In-repo projects using Scheduler | M | L | Grep confirms **no** in-repo `.cs` file calls `AddScheduledJob<T>` outside the Scheduler itself and docs | No mitigation needed — confirmed clean. |

### Top 3 Risks

1. **#1 — Breaking rename (H/H).** Highest because it is **certain** to break every external consumer
   who names `INotification`. Mitigated only by semver + release notes (no shim by design).
2. **#6 — CVE pin omitted (H/H if missed).** A single-line omission escalates to a HIGH CVE for every
   consumer. Mitigated by the pin in step 03 + `TreatWarningsAsErrors` catching NU1903.
3. **#2 — Serialisation failure (H/M).** A runtime-only failure that manifests as silently broken event
   dispatch. Mitigated by loud documentation + integration test.

### Required Mitigations Before Merge

| Mitigation | Owner step | Verified by |
|---|---|---|
| Pin `Newtonsoft.Json 13.0.4` in the plugin `.csproj`; `dotnet build` passes with no NU1901–04 | 03 | Build + step 09 |
| Document DI-activator + serialiser requirements in XML doc and `Hangfire.md` | 04, 07 | Code review |
| `AddPersistentEvents()` uses `RemoveAll + Add` (order-independent override) | 04 | Step 09 tests both orderings |
| Publisher is singleton + `IServiceScopeFactory` (no scoped capture) | 04 | Step 09 validates fresh scope |
| Step 09 exercises a real serialisation round-trip (enqueue → job arg → dispatch) | 09 | Test green |
| Release notes / CHANGELOG call out the `INotification → IEvent` breaking rename | 07 (+ close-out) | Code review |
| Use `typeof(TJob).FullName` (not `nameof`) for recurring job id | 05 | Step 09 |

### Accepted Risks

- **#1 (breaking rename):** Accepted — pre-1.0 policy; ADR documents it; semver bump signals it.
- **#8 (at-least-once re-dispatch):** Accepted — identical contract to today's fire-and-forget; no
  new handler obligation; documented.

### Decision

**Go with mitigations** — all `H`-severity scenarios have concrete mitigations owned by specific steps.
The plan already encodes every required mitigation. No redesign needed. Implementation of steps 01–09
may proceed.
