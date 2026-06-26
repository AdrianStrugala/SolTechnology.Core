# FI-005: Medallion.Threading Postgres + SqlServer lock backends

> **Status:** ⏸️ Parked
> **Parked date:** 2026-06-25
> **Source:** [ADR-012 step 05](../adr/012-production-pattern-adoption-wave-2.md#implementation-summary) — superseded by Option B
> **Would-be target:** `SolTechnology.Core.Cache` (or a dedicated package if Redis isn't available)
> **Semver / Effort:** MINOR · M

---

## Context — what it is

Production lock backends using `DistributedLock.*` (Medallion.Threading) for environments where
Redis is not available but Postgres or SQL Server is:

- **Postgres advisory locks** (`DistributedLock.Postgres`) — zero-table, kernel-level mutual exclusion.
- **SQL Server application locks** (`DistributedLock.SqlServer`) — `sp_getapplock` / `sp_releaseapplock`.
- **File-system locks** (`DistributedLock.FileSystem`) — single-box local dev without Redis.

Each backend would honour the same `IDistributedLockService` contract (degrade-to-`null`,
never-throw, fencing) already shipped in `Core.Cache` (ADR-012 step 04, Option B).

## Why parked

- **Redis covers all current production apps.** The Option-B implementation (`SET NX EX` in
  `Core.Cache`) is sufficient — every app that needs a distributed lock already has Redis.
- Adding Medallion introduces **3 new third-party NuGet dependencies** with no current consumer.
- The connection-source design (local options accessor vs. reusing `Core.SQL`) adds coupling
  decisions that aren't needed today.

## What would unpark it

- A production app that needs distributed locking but does **not** have Redis (only Postgres or
  SQL Server).
- A scenario where Redis advisory semantics are insufficient (e.g. need DB-transaction-scoped locks
  that auto-release on rollback — Postgres advisory locks inside a transaction do this natively).

## Implementation notes (from the original step 05)

- Pin versions via `package-management` skill; CVE-check before merge.
- Connection string sourced from local `DistributedLockOptions` — do NOT couple to `Core.SQL`.
- Key namespacing: `{prefix}/{name}` (same as Redis backend).
- `AddDistributedLock()` would gain a backend selector enum in options.

