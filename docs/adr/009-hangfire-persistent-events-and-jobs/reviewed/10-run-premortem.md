---
adr: 009-hangfire-persistent-events-and-jobs
step: 10 of 10
status: reviewed
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

