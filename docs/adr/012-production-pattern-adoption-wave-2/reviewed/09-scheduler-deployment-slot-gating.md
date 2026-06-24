---
adr: 012-production-pattern-adoption-wave-2
step: 09 of 24
status: reviewed
---

# Step 09: C1 — Deployment-slot gating for background work (`Core.Scheduler`)

## Summary
Add an `IDeploymentSlotProvider` plus a guard helper so any `BackgroundService` / Hangfire server can
run periodic work **only on the "live" slot** — a warmed-but-not-live blue/green/staging slot should
not also run pollers and cleanup jobs. Small, self-contained; lands before the leader-elected poller
(step 10) which composes with it.

> **Home confirmed (decision): `Core.Scheduler`.** Slot gating is a hosting concern and stays in
> `Core.Scheduler`. It takes **no** dependency on `Core.DistributedLock` (gating is orthogonal to
> leader election — see below); `Core.DistributedLock` itself stays hosting-free.

## Affected components
- `src/SolTechnology.Core.Scheduler/Slots/IDeploymentSlotProvider.cs` — abstraction
  (`bool IsLiveSlot` / `string CurrentSlot`).
- `src/SolTechnology.Core.Scheduler/Slots/DeploymentSlotProvider.cs` — default implementation
  reading the slot from configuration / environment (e.g. an app-setting or env var).
- `src/SolTechnology.Core.Scheduler/Slots/DeploymentSlotOptions.cs` — options (live-slot name,
  env-var/config key).
- `src/SolTechnology.Core.Scheduler/Slots/DeploymentSlotGuard.cs` — guard helper (e.g.
  `ShouldRun()` / a small extension a hosted service calls at the top of each tick).
- `src/SolTechnology.Core.Scheduler/ModuleInstaller.cs` — `AddDeploymentSlotGating(...)` DI entry,
  options bound with `ValidateOnStart()`.
- `docs/Cron.md` (Scheduler doc) — "Deployment-slot gating" section.
- `tests/SolTechnology.Core.Scheduler.Tests/` — **new** NUnit test project (none exists today):
  provider + guard tests (live vs non-live). Wire it into `SolTechnology.Core.slnx` under `/Tests/`.
  (CLAUDE.md §1 new-test-folder confirmation **GIVEN** for this wave; shared with step 10.)

## Details
- The provider reads the current slot once (or per-tick) and exposes whether it is the configured
  live slot. Keep the source pluggable (config key / env var) — different hosts surface the slot
  differently (App Service `WEBSITE_SLOT_NAME`, a custom env var, etc.).
- The guard helper is what hosted services call: if not the live slot, skip the tick (and, in
  step 10, also ensure the leader lock is released and timers stopped on slot change).
- **`Core.Scheduler` has `TreatWarningsAsErrors=false`** today — keep additions warning-clean so the
  step-21 build-hygiene guard's allow-list does not need a new entry on this account.
- No dependency on `Core.DistributedLock` — slot gating is orthogonal to leader election (gating =
  "should this instance run at all"; election = "of those that should, exactly one runs"). They
  compose in step 10.

## Acceptance criteria
- `IDeploymentSlotProvider` reports live vs non-live from the configured source.
- The guard helper lets a hosted service cheaply skip work on a non-live slot.
- `AddDeploymentSlotGating` binds + validates options on start.
- `docs/Cron.md` documents the gating pattern.
- The new `tests/SolTechnology.Core.Scheduler.Tests` project is in `SolTechnology.Core.slnx`
  (`/Tests/`) and covers live-slot (runs) and non-live-slot (skips).

## Open questions
- Default slot source key. Recommend supporting a configurable key with a sensible default; document
  the App Service `WEBSITE_SLOT_NAME` convention as an example.

