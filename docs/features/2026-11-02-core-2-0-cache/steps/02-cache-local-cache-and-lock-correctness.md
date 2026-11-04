---
spec: 2026-11-02-core-2-0-cache
step: 02
status: done
---

# Step 02: Cache — local cache fault eviction, single flight, and a correct local lock

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

Former core step 28; moved here by the per-library regroup (user, 2026-11-02).

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
- Key composition is unchanged here (P-07 is step 03).

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green, and the fault test and the double-dispose test fail on `4097d47f`.
- [x] No public or protected signature changed.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### Lock entries instead of semaphores

**Original plan:** keep the per-key `SemaphoreSlim`, add an `Interlocked.Exchange` release guard and a per-entry
deadline, and remove the entry on uncontended release.

**Actual implementation:** acquisition never waits (`TimeSpan.Zero`), so a semaphore added nothing. The service keeps
one holder object per name with its `TimeProvider` deadline: `TryAdd` claims a free name, `TryUpdate` takes over an
expired holder, and a handle releases with `Interlocked.Exchange` plus `TryRemove` of its own holder only. Every release
removes the entry, so no name leaks, and an expired handle cannot release the next holder (the Redis fencing contract).
An `internal HeldLockCount` lets `Release_Uncontended_RemovesEntry` see the dictionary. A cancelled token returns a
cancelled `ValueTask`, as the async method did.

### Verification

`Cache` source on `master` differed from `4097d47f` only in its `.csproj` (central package versions). Before the fix,
with the tests compiled against the one-argument constructor, `GetOrAdd_FactoryFaults_NextCallRetriesFactory` (both
cases), `GetOrAdd_ConcurrentMisses_FactoryRunsOnce`, `GetOrAdd_ScopedFactoryFaults_NextCallRetries`,
`Dispose_Twice_DoesNotReleaseOtherHolder`, and `TryAcquire_AfterExpiry_Succeeds` failed; after it all 25 Cache tests
pass (the concurrency and lock tests five runs in a row). A reflection dump of public and protected members of the
built `SolTechnology.Core.Cache.dll` is identical before and after. Premortem `di.md` sweep for the installer: the
registration keeps its service type and singleton lifetime, takes the same `TimeProvider` fallback as
`AddSolLocalIdempotency`, and `ModuleResolutionTests` resolves it under `ValidateOnBuild`. The dead whitespace-key check
stays in `ScopedCache` and `RedisCache`, which the step did not list.

