# ADR-009: Persistent events and recurring jobs via `SolTechnology.Core.Hangfire` — Implementation Summary

Tracking the implementation steps for [ADR-009](../009-hangfire-persistent-events-and-jobs.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | CQRS event marker split + `IEvent` rename | [`done/01-cqrs-event-marker-rename.md`](done/01-cqrs-event-marker-rename.md) | ✅ done |
| 02 | CQRS dispatch seam (`IEventPublisher` / `IEventDispatcher`) | [`reviewed/02-cqrs-dispatch-seam.md`](reviewed/02-cqrs-dispatch-seam.md) | 🔍 reviewed |
| 03 | Hangfire plugin project skeleton + dependency report | [`reviewed/03-hangfire-plugin-project-skeleton.md`](reviewed/03-hangfire-plugin-project-skeleton.md) | 🔍 reviewed |
| 04 | Persistent events publisher + `AddPersistentEvents()` | [`reviewed/04-persistent-events-publisher.md`](reviewed/04-persistent-events-publisher.md) | 🔍 reviewed |
| 05 | Recurring jobs — `IJob` + `AddRecurringJob<TJob>(cron)` | [`reviewed/05-recurring-jobs.md`](reviewed/05-recurring-jobs.md) | 🔍 reviewed |
| 06 | Deprecate `Scheduler` + delete orphan `Jobs` | [`reviewed/06-deprecate-scheduler-remove-orphan.md`](reviewed/06-deprecate-scheduler-remove-orphan.md) | 🔍 reviewed |
| 07 | Documentation (`Hangfire.md` + parity) | [`reviewed/07-documentation.md`](reviewed/07-documentation.md) | 🔍 reviewed |
| 08 | DreamTravel migration to the new seam | [`reviewed/08-dreamtravel-migration.md`](reviewed/08-dreamtravel-migration.md) | 🔍 reviewed |
| 09 | `SolTechnology.Core.Hangfire.Tests` | [`reviewed/09-plugin-tests.md`](reviewed/09-plugin-tests.md) | 🔍 reviewed |
| 10 | Run premortem (mandatory gate) | [`done/10-run-premortem.md`](done/10-run-premortem.md) | ✅ done |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's
current location (`to-do/` / `reviewed/` / `done/`).

## Review notes (plan-reviewer, 2026-06-10)

All ten steps were reviewed against the live codebase and moved to `reviewed/`. Key changes:

- **Step 02 / 04 (blocker):** pinned the publisher/dispatcher lifetimes to maintainer decision **B2**
  (singleton publisher **+ `IServiceScopeFactory`**, fresh scope per dispatch). Corrected two false
  claims in step 04 — `CQRSMediator` is registered **scoped** (not singleton), and the "mirrors
  DreamTravel" mechanism was changed to **actually** mirror it (enqueue the plugin's own dispatch
  method, not `IEventDispatcher.Dispatch`). Fixed the singleton→scoped captive-dependency trap.
- **Step 04 / 07 (major):** documented two app-side requirements the `Hangfire.Core`-only plugin
  cannot satisfy — a **DI-aware `JobActivator`** and **type-aware `IEvent` serialisation**
  (`UseRecommendedSerializerSettings()`); **reopened** the configurable-`RetryAttempts` mechanism
  (cannot be a compile-time attribute and must not mutate process-global `GlobalJobFilters`) — **since
  resolved by the maintainer: no automatic retry, `[AutomaticRetry(Attempts = 0)]`, no retry knob.**
- **Step 02 / 04 (major):** pinned the override mechanism to order-independent
  `RemoveAll<IEventPublisher>() + Add`.
- **Step 07 (major):** README **does** carry a module table — add a `Hangfire` row and mark the
  `Scheduler` row deprecated. Fixed two broken relative links (`ClaudeCodingGuide`,
  `documentation-cleanup`).
- **Relative links:** corrected off-by-one `../` depth on the skill links in steps **03, 07, 09, 10**.
- **Steps 01 / 05 / 06 / 08 (minor):** corrected the `IMediator.Publish` behaviour description
  (one `Task.Run`, one shared scope, sequential `foreach`); fixed step 05's `IHostedService`
  rationale; verified Scheduler `0.5.0`→`0.6.0` and the orphan `Jobs/` deletion; fixed step 08's
  "blocks like today" contradiction (today's sync double routes through fire-and-forget `Publish`).
- **Verified clean:** test stack is NUnit 4.3.2 + FluentAssertions 6.12.2 + NSubstitute 5.3.0 +
  AutoFixture 4.18.1 (no stale xUnit/Moq directives); the `Newtonsoft.Json` 13.0.4 pin clears
  CVE-2024-21907 (advisory first-fixed = 13.0.1).

**Resolved by the maintainer (2026-06-10):** (1) **No rate limit** for now — out of scope for a
`Hangfire.Core`-only plugin; only app-owned concurrency (`WorkerCount` / named queues) applies.
(2) **No automatic retry** — persistent events dispatch once (`[AutomaticRetry(Attempts = 0)]`); retry
is a handler-level concern, and `PersistentEventsOptions` ships **without** a retry knob. (3) Flipping
ADR-009's status row in `docs/adr/README.md` is owned by the **plan close-out**, not step 07. Steps
04 / 07 / 10 and the ADR were updated accordingly.

> **Premortem runs first.** Step 10 is numbered last but is a **gate** — it must run before any of
> steps 01–09 begins, and blocks implementation until *Go* / *Go with mitigations*.

## Dependency graph (suggested order)

```
01 (rename) ──► 02 (seam) ──► 04 (persistent events) ──► 08 (DreamTravel) ──► 09 (tests)
                   │                ▲                          ▲
03 (project) ──────┴──► 05 (jobs) ──┘                          │
06 (deprecate Scheduler) ─────────────────────────────────────┘  (independent; any time after 03)
07 (docs) ── after the API it documents is final (≥ 05)
10 (premortem) ── LAST; blocks all implementation until Go / Go-with-mitigations
```

## Notes for the implementer

- **Keep `dotnet build SolTechnology.Core.slnx` green at every step.** The `.slnx` includes the
  DreamTravel projects, so the breaking rename in step 01 must propagate its **mechanical** marker
  references into DreamTravel in the same PR. The **behavioural** seam migration is step 08.
- **Premortem is the gate, not a formality.** This change touches a public CQRS API, two
  `ModuleInstaller.cs` files, and adopts a new external dependency — exactly the triggers in the
  premortem skill's "Mandatory before merging" list.
- **Plan close-out owns the ADR index.** After steps 01–09 land, the close-out flips ADR-009's status
  row in [`docs/adr/README.md`](../README.md) (Proposed → Accepted/Implemented) and updates the ADR's
  own `Status` header. Step 07 deliberately does **not** touch the ADR index.

