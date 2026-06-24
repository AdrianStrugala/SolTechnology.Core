---
adr: 012-production-pattern-adoption-wave-2
step: 10 of 24
status: reviewed
---

# Step 10: C2 — Leader-elected polling service base (`Core.Scheduler`)

## Summary
Generalise the production-resilient hosted-service pattern into a `LeaderElectedPollingService` base
in `Core.Scheduler`: across many instances/regions, exactly one node runs the periodic loop, guarded
by the distributed lock from `Core.DistributedLock` (steps 04–05) and, optionally, the
deployment-slot gate (step 09). **Depends on A2.**

> **Home + dependency direction confirmed (decision).** The base lives in `Core.Scheduler` (a hosting
> concern), which takes a `ProjectReference` to `Core.DistributedLock`. The dependency flows
> **`Core.Scheduler → Core.DistributedLock`** only — `Core.DistributedLock` stays hosting-free and
> never references `Core.Scheduler`.

## Affected components
- `src/SolTechnology.Core.Scheduler/LeaderElection/LeaderElectedPollingService.cs` — abstract
  `BackgroundService` base.
- `src/SolTechnology.Core.Scheduler/LeaderElection/LeaderElectionOptions.cs` — options (lock name,
  acquire timeout, lock re-check interval, poll interval).
- `src/SolTechnology.Core.Scheduler/ModuleInstaller.cs` — `AddLeaderElectedPollingService<T>(...)`
  DI entry; options bound with `ValidateOnStart()`.
- `src/SolTechnology.Core.Scheduler/SolTechnology.Core.Scheduler.csproj` — `ProjectReference` to
  `SolTechnology.Core.DistributedLock`.
- `docs/Cron.md` — "Leader-elected polling service" section.
- `tests/SolTechnology.Core.Scheduler.Tests/` — **new** NUnit test project (shared with step 09;
  wire into `SolTechnology.Core.slnx` `/Tests/` if step 09 has not already): lifecycle + guard-rail
  tests (acquire/lose leadership, StopAsync ordering, swallow-and-continue on tick exception).

## Details
- **Lifecycle:** linked CTS; a `Task.Run` execution loop; leader election via
  `IDistributedLockService.TryAcquireLockAsync`; per-tick work; lock re-check on an interval so a
  crashed leader is taken over; graceful `StopAsync` = **cancel → release lock → stop timers**;
  `IAsyncDisposable` with a locked timer list.
- **Guard-rails (acceptance-critical):**
  - `async void` timer callbacks (if any timers are used) MUST be wrapped in try/catch and **never
    throw** out of `async void`.
  - All loops **swallow + log + continue** — one bad tick never kills the service.
  - A failed lock acquisition (`null` handle from `Core.DistributedLock`) means "not leader this
    tick" — log and retry next interval; never throw.
- **Compose with step 09:** optionally consult `IDeploymentSlotProvider` so a non-live slot does not
  even contend for leadership; on slot change, release the lock and stop timers.
- Keep the base generic over the unit of work (an abstract `ExecuteTickAsync(CancellationToken)` the
  subclass implements).
- **`Core.Scheduler` `TreatWarningsAsErrors=false`** — keep additions warning-clean.

## Acceptance criteria
- Exactly one instance executes the loop while holding the lock; others wait and take over when the
  lock frees (verified with two in-process instances sharing a lock backend).
- A thrown exception inside a tick is logged and the loop continues.
- `StopAsync` cancels, releases the lock, and stops timers in that order.
- A `null` lock acquisition does not throw and retries next interval.
- `Core.Scheduler` references `Core.DistributedLock` (one-way) and builds green.
- `docs/Cron.md` documents the base + guard-rails.
- Lifecycle + guard-rail tests live in `tests/SolTechnology.Core.Scheduler.Tests`.

## Open questions
- Timer-based vs pure `await Task.Delay` loop for the poll interval. Recommend a `Task.Delay` loop
  (simpler, no `async void`) unless per-message-type timers are explicitly needed; flag the choice.

