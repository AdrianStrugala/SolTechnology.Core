---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 28
status: to-do
---

# Step 28: Cache — local cache fault eviction, single flight, and a correct local lock

## Summary
The in-process cache and lock have independent but small defects; they ship as one Cache PR
(merged from the former two steps during plan review, RQ11):

- `SingletonCache` stores `Lazy<Task<T>>` before the task finishes. A factory that fails stays
  cached and is rethrown to every caller for 20 minutes by default (`logging-cache#7`,
  `SingletonCache.cs:31`; `ScopedCache.cs:20` has the same problem within a scope).
  `TryGetValue`-then-`Set` is not atomic, so concurrent misses run the factory twice, and the
  whitespace-key check is dead code (`logging-cache#24`). Neither cache has tests
  (`logging-cache#17`).
- `LocalDistributedLockService`'s handle `DisposeAsync` is not idempotent. A second dispose
  releases another holder's semaphore or throws `SemaphoreFullException` (`logging-cache#5`,
  `LocalDistributedLockService.cs:47`). `expiry` is ignored, so a lock never auto-releases
  despite the `docs/Cache.md:272` contract, and each name leaks a `SemaphoreSlim` forever
  (`logging-cache#6`, `:15,20`).

## Affected components
- `src/SolTechnology.Core.Cache/SingletonCache.cs` — EDIT — atomic get-or-create; `Remove(key)` on fault; delete the dead whitespace check
- `src/SolTechnology.Core.Cache/ScopedCache.cs` — EDIT — `TryRemove` on fault
- `src/SolTechnology.Core.Cache/LocalDistributedLockService.cs` — EDIT — `Interlocked.Exchange` release guard; per-entry deadline from an injected `TimeProvider`; remove the entry on uncontended release
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — the local-lock registration passes `sp.GetService<TimeProvider>() ?? TimeProvider.System`, the same fallback `AddSolLocalIdempotency` uses at `:96` (the installer registers no `TimeProvider`)
- `tests/SolTechnology.Core.Cache.Tests/SingletonCacheTests.cs` — NEW
- `tests/SolTechnology.Core.Cache.Tests/ScopedCacheTests.cs` — NEW
- `tests/SolTechnology.Core.Cache.Tests/LocalDistributedLockServiceTests.cs` — NEW

## Changes
- `LocalDistributedLockService` stays `internal sealed`; adding the `TimeProvider` constructor parameter is not a public change.
- Tests:
  - `GetOrAdd_FactoryFaults_NextCallRetriesFactory`.
  - `GetOrAdd_ConcurrentMisses_FactoryRunsOnce` (100 parallel calls).
  - `GetOrAdd_ScopedFactoryFaults_NextCallRetries`.
  - `Dispose_Twice_DoesNotReleaseOtherHolder`.
  - `TryAcquire_AfterExpiry_Succeeds` (`FakeTimeProvider`, already referenced by Cache.Tests).
  - `Release_Uncontended_RemovesEntry`.
  - `TryAcquire_Cancelled_Throws`.
- Key composition is unchanged here (P-07 is step 29).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green, and the fault test and the double-dispose test fail on `4097d47f`.
- [ ] No public or protected signature changed.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
