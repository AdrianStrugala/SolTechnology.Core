# FI-004: Leader-elected polling service base

> **Status:** ⏸️ Parked
> **Parked date:** 2026-06-25
> **Source:** [Feature-002 step 10](../features/002-production-pattern-adoption-wave-2.md#implementation-summary)
> **Would-be target:** `SolTechnology.Core.Scheduler`
> **Semver / Effort:** MINOR · M

---

## Context — what it is

A `LeaderElectedPollerBase<T>` that composes:
- `IDistributedLockService` (from `Core.Cache`, Feature-002 step 04) — exactly one instance acquires
- `DeploymentSlotGuard` (FI-003) — only the live slot attempts acquisition
- A configurable poll interval + jitter

The base class provides a `BackgroundService` that periodically attempts the lock and, if acquired,
calls the subclass's `ExecuteAsync` for one cycle. On failure or lock-not-acquired, it backs off
and retries next tick.

## Why parked

- Depends on FI-003 (deployment-slot gating).
- Hangfire recurring jobs with the distributed lock already cover the "only one instance processes"
  pattern (and Hangfire gives dashboard visibility, retry, dead-letter for free).
- No current consumer is asking for a raw poller base outside of Hangfire.

## What would unpark it

- A scenario where Hangfire is too heavy (e.g. a lightweight sidecar that polls an external queue
  every 5s and needs leader election but not the full Hangfire dashboard/persistence stack).
- A consumer explicitly requesting a non-Hangfire, lock-based poller primitive.

