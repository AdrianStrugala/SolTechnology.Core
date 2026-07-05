# ADR-009: Persistent events and recurring jobs via `SolTechnology.Core.Hangfire`

> **Status:** Accepted
> **Decision Date:** 2026-06-09
> **Decision Maker:** Repository maintainers
> **Stakeholders:** CQRS consumers (NuGet + DreamTravel), Scheduler consumers

---

## Context

When something happens in an application (`CitySearched`), the author wants to "throw an event"
and have any registered follow-up actions run — durably, surviving process restarts. Today the
CQRS module dispatches notifications **fire-and-forget** in-process: `CQRSMediator.Publish<T>`
runs `_ = Task.Run(...)` over the registered handlers
([`src/SolTechnology.Core.CQRS/Internal/Mediator.cs:41`](../../src/SolTechnology.Core.CQRS/Internal/Mediator.cs)).
If the process dies mid-handler, the work is lost — there is no retry, no persistence, no queue.

DreamTravel already worked around this with a bespoke seam:
[`IHangfireNotificationPublisher`](../../sample-tale-code-apps/DreamTravel/src/Infrastructure/DreamTravel.Infrastructure/Events/EventPublisher.cs)
enqueues a Hangfire background job that re-resolves `IMediator` and calls `Publish`. That pattern
is proven (`Hangfire.Core` 1.8.22 in `DreamTravel.Infrastructure.csproj`) but lives in a sample
app, is hand-rolled per consumer, and leaks Hangfire into application handlers.

Three problems must be solved together:

1. **Persistence is not reusable.** The DreamTravel seam should become a shared, opt-in capability.
2. **CQRS must stay Hangfire-free.** `SolTechnology.Core.CQRS` has no business referencing a job
   scheduler; the dependency belongs in a plugin.
3. **The marker interfaces are mis-housed and mis-named.** `ICommand`, `ICommand<T>`, `IQuery<T>`
   and `INotification` all live in one file
   ([`src/SolTechnology.Core.CQRS/ICommand.cs`](../../src/SolTechnology.Core.CQRS/ICommand.cs)),
   and `INotification` reads like infrastructure jargon rather than the domain concept (an *event*).

Constraints fixed by the maintainer before this ADR:

- **.NET-only, Hangfire-based.** Cross-technology dispatch (a broker, an outbox) was considered and
  rejected for now; the solution is a Hangfire plugin.
- **Pre-1.0.** Breaking renames are acceptable without an `[Obsolete]` shim.
- **`Hangfire.Core` only.** Storage, server and dashboard bootstrap stay app-owned (they pull
  `Hangfire.AspNetCore` / `Hangfire.SqlServer`, which are deployment concerns).
- **No automatic retry** (settled 2026-06-10). A persistent event is dispatched **once**
  (`[AutomaticRetry(Attempts = 0)]`); resilience is a **handler-level** concern. Persistence is for
  durability + dashboard visibility, not auto-retry.
- **No rate limit** (settled 2026-06-10). Out of scope for now; only app-owned concurrency
  (`AddHangfireServer(o => o.WorkerCount = N)` / named queues) applies.

**Affected core modules:** `SolTechnology.Core.CQRS` (rename + dispatch seam),
`SolTechnology.Core.Hangfire` (new), `SolTechnology.Core.Scheduler` (deprecated).
**Affected sample apps:** `sample-tale-code-apps/DreamTravel` (marker rename + seam migration).

## Decision

Introduce a new package **`SolTechnology.Core.Hangfire`** that plugs into CQRS via a public
dispatch seam, and reshape the CQRS event contract:

1. **Rename `INotification` → `IEvent` and `INotificationHandler<T>` → `IEventHandler<T>`**
   (breaking, no shim). Keep the `IMediator.Publish` method name. Split the markers out of
   `ICommand.cs` into cohesive files (`IQuery.cs`, `IEvent.cs`).
2. **Add a public dispatch seam to CQRS** — `IEventPublisher` (how an event leaves `Publish`) and
   `IEventDispatcher` (how an event fans out to its handlers). The current `Task.Run` fan-out moves
   into internal defaults `InMemoryEventPublisher` + `EventDispatcher`, registered via `TryAdd` so a
   plugin can replace the publisher. `CQRSMediator.Publish` delegates to `IEventPublisher`. Behaviour
   is byte-for-byte unchanged when no plugin is installed.
3. **`SolTechnology.Core.Hangfire` ships `HangfireEventPublisher : IEventPublisher`** (singleton +
   `IServiceScopeFactory`) that enqueues **one Hangfire background job per event** — a call to the
   plugin's own `DispatchInScope` method, which opens a fresh DI scope and runs
   `IEventDispatcher.Dispatch(evt)` (all handlers for that event), mirroring DreamTravel's
   `DispatchEvent`. **No automatic retry** (`[AutomaticRetry(Attempts = 0)]`): an event is dispatched
   once; resilience is a handler concern, while persistence still buys durability and at-least-once
   delivery. Opt in with `services.AddCQRS().AddPersistentEvents()`, configured by
   `PersistentEventsOptions` (queue name).
4. **`IJob` is a separate abstraction** (not an `IEvent`), owned by the plugin, registered with
   `services.AddRecurringJob<TJob>(cron)` via Hangfire's `IRecurringJobManager`.
5. **App owns the Hangfire bootstrap.** The consuming app calls `AddHangfire` / `UseXxxStorage` /
   `AddHangfireServer` / `MapHangfireDashboard`. `AddPersistentEvents()` only swaps the publisher and
   binds options; `AddRecurringJob<TJob>` only registers a recurring job. The plugin references
   `Hangfire.Core` only.
6. **Deprecate `SolTechnology.Core.Scheduler`** — mark `AddScheduledJob<T>` and `ScheduledJob`
   `[Obsolete]`, pointing to `SolTechnology.Core.Hangfire`.
7. **Delete the orphan `src/SolTechnology.Core.Jobs/`** folder (bin/obj artifacts only; no `.csproj`,
   not in `.slnx`).

## Alternatives Considered

The Blue/Red argument below is condensed from the [`blue-red-team`](../../.github/skills/blue-red-team/SKILL.md)
skill; the implementer re-runs it only if a crux changes.

1. **Per-event dispatch job (chosen).** One Hangfire job per `IEvent`; the job dispatches every handler
   for that event **once** (no automatic retry).
   *Pros:* mirrors the proven `HangfireNotificationPublisher.DispatchEvent`; one persisted unit ==
   one published event; smallest delta from today's behaviour.
   *Cons:* at-least-once delivery (crash-recovery or a manual dashboard re-queue) can re-run
   already-succeeded sibling handlers — handlers must be idempotent (true of fire-and-forget today too).

2. **Per-handler dispatch job.** Enqueue one Hangfire job per `(event, handler)` pair so each handler
   retries independently.
   *Pros:* failure isolation — a poisoned handler never re-runs its siblings.
   *Cons:* the dispatcher must enumerate handler types at publish time and the plugin would need to
   reach into CQRS handler registration; N jobs per event multiplies dashboard noise and storage. The
   maintainer closed this fork in favour of (1)'s simplicity.

3. **Keep `INotification`, add Hangfire inside CQRS.** No rename, no plugin; `AddCQRS` takes a
   `usePersistence` flag and references `Hangfire.Core` directly.
   *Pros:* no breaking change; one package.
   *Cons:* forces `Hangfire.Core` (+ the `Newtonsoft.Json` 11.0.1 CVE floor — see Consequences) onto
   **every** CQRS consumer, including those who only send commands. Rejected: violates "CQRS stays
   Hangfire-free".

4. **Generic `IMessage` covering events + jobs.** One marker for both.
   *Cons:* an event (something happened, fan out) and a recurring job (run this on a cron) have
   different lifecycles and registration. Collapsing them reads worse, not better. Rejected per the
   maintainer's "RecurringJob should be `IJob`, not `IEvent`".

**Cruxes:** (a) handler idempotency — settled: fire-and-forget already requires it, so per-event
at-least-once dispatch adds no new contract; (b) dependency cost — settled: the plugin isolates `Hangfire.Core` so
command-only consumers pay nothing.

## Consequences

**Positive**

- Persistent, durable event dispatch is a one-liner (`AddPersistentEvents()`) for any CQRS app.
- `SolTechnology.Core.CQRS` stays dependency-light; `Hangfire.Core` lives only in the plugin.
- `IEvent` / `IEventHandler<T>` read as domain concepts; markers live in cohesive files.
- The dispatch seam (`IEventPublisher` / `IEventDispatcher`) is a reuse point for future transports
  (outbox, broker) without re-touching the mediator.
- `Scheduler` and `Hangfire` recurring jobs converge on one mechanism; the orphan `Jobs` folder is gone.

**Negative**

- **Breaking rename.** Every CQRS consumer that names `INotification` / `INotificationHandler<T>`
  must rename. In-repo: CQRS itself, CQRS tests, DreamTravel (5 files), and docs.
- **New external dependency** for `src/SolTechnology.Core.*`: `Hangfire.Core` 1.8.22 is **absent from
  `nuget-stats.json`** and pulls a transitive `Newtonsoft.Json` **11.0.1** floor carrying
  **CVE-2024-21907 (HIGH, NU1903)**. The plugin MUST pin `Newtonsoft.Json` to **13.0.x** at the seam,
  exactly as `DreamTravel.Infrastructure.csproj` pins 13.0.4. CLAUDE.md §2 dependency-impact reporting
  applies.
- Apps still own the Hangfire storage/server/dashboard bootstrap — `AddPersistentEvents()` does not
  hide it. This is deliberate (deployment concern) but is one more thing the consumer must wire.
- `Scheduler` consumers get `[Obsolete]` warnings (no in-`slnx` consumers exist, so the `slnx` build
  stays green; `Scheduler.csproj` already sets `TreatWarningsAsErrors=false`).

**Semver impact:** **MAJOR** for `SolTechnology.Core.CQRS` (breaking public rename) — under 0.x this
lands as a `0.8.0 → 0.9.0` minor bump. New `SolTechnology.Core.Hangfire` `0.1.0`. `SolTechnology.Core.Scheduler`
**MINOR** (`0.5.0 → 0.6.0`, additive `[Obsolete]`).

## Related

- [ADR-007](007-cqrs-production-hardening.md) — the in-house mediator and fire-and-forget dispatch this
  ADR re-seams.
- [ADR-006](006-implementation-plan-workflow.md) — plan layout and folder-state rules this plan follows.
- [`docs/CQRS.md`](../CQRS.md) — consumer docs updated by this change.
- [`docs/Cron.md`](../Cron.md) — Scheduler docs, marked deprecated by this change.
- [`CLAUDE.md` §1](../../CLAUDE.md) — dependency-impact reporting for the new `Hangfire.Core` adoption.

## Implementation summary

Completed 2026-06-11. The per-step working folder
(`docs/adr/009-hangfire-persistent-events-and-jobs/`) was deleted per the ADR-006 collapse-on-completion rule.

| # | Step | Shipped |
|---|---|---|
| 01 | CQRS event marker split + `IEvent` rename | `INotification` → `IEvent`, `INotificationHandler<T>` → `IEventHandler<T>` in `src/SolTechnology.Core.CQRS/` |
| 02 | CQRS dispatch seam | `IEventPublisher`, `IEventDispatcher`, `InMemoryEventPublisher`, `EventDispatcher` in `src/SolTechnology.Core.CQRS/` |
| 03 | Hangfire plugin project skeleton | `src/SolTechnology.Core.Hangfire/` — `Hangfire.Core` 1.8.22, `Newtonsoft.Json` 13.0.4 pin |
| 04 | Persistent events publisher | `HangfireEventPublisher`, `AddPersistentEvents()`, `PersistentEventsOptions` |
| 05 | Recurring jobs | `IJob`, `AddRecurringJob<TJob>(cron)`, `RecurringJobRunner<T>`, `RecurringJobRegistrar` (IHostedService) |
| 06 | Deprecate Scheduler + delete orphan Jobs | `[Obsolete]` on `AddScheduledJob<T>` + `ScheduledJob`, version 0.6.0; deleted `src/SolTechnology.Core.Jobs/` |
| 07 | Documentation | `docs/Hangfire.md`, updated `CQRS.md`, `theDesign.md`, `Cron.md` deprecation banner, README table |
| 08 | DreamTravel migration | Deleted `IHangfireNotificationPublisher`; handlers use `IMediator.Publish`; Worker calls `AddPersistentEvents()` |
| 09 | Plugin tests | `tests/SolTechnology.Core.Hangfire.Tests/` — 16 tests (NUnit + FluentAssertions + NSubstitute) |
| 10 | Premortem | Go with mitigations (CVE pin, no in-repo Scheduler consumers, idempotency documented) |

### Preserved deviations

- **Step 08** — `AddPersistentEvents()` lives in Worker's `Program.cs` (not `InstallInfrastructure`) because it requires `AddCQRS()` first and the install order in both hosts calls Infrastructure before CQRS.
- **Step 08** — API host uses default in-memory publisher (no Hangfire infra); only Worker persists events.
- **Step 08** — Removed `DreamTravel.Queries → DreamTravel.Infrastructure` project reference (handlers no longer depend on Infrastructure).

