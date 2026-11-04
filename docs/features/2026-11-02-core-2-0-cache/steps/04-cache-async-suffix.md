---
spec: 2026-11-02-core-2-0-cache
step: 04
status: done
---

# Step 04: Cache — drop `Async` from lock and idempotency contracts

## Summary
`IDistributedLockService.TryAcquireLockAsync` (`IDistributedLockService.cs:23`) and
`IIdempotencyStore.TryAddAsync/GetAsync/SetResponseAsync/RemoveAsync` (`IIdempotencyStore.cs:31,37,43,48`)
carry the `Async` suffix with no sync overload (Guide §10). The four implementations are already
`internal sealed`, so only the two public interfaces change (`C-13`).

Former core step 47; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Cache/IDistributedLockService.cs` — EDIT
- `src/SolTechnology.Core.Cache/IIdempotencyStore.cs` — EDIT
- `src/SolTechnology.Core.Cache/LocalDistributedLockService.cs`, `RedisDistributedLockService.cs`, `LocalIdempotencyStore.cs`, `RedisIdempotencyStore.cs` — EDIT
- `src/SolTechnology.Core.API/**`, `src/SolTechnology.Core.Hangfire/**` — EDIT — call sites, if any (`grep -rn "TryAcquireLockAsync\|TryAddAsync\|SetResponseAsync" src`)
- `tests/SolTechnology.Core.Cache.Tests/**` — EDIT

## Changes
- Pure rename. No forwarders.

## Acceptance criteria
- [x] `grep -rnE "TryAcquireLockAsync|TryAddAsync|SetResponseAsync" src tests sample-tale-code-apps` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green; `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none (`C-13` confirmed by the user on 2026-11-02)

## Deviations

### Test names and verification scope

**Original plan:** a pure rename in the two interfaces, the four implementations, and call sites in Api and Hangfire.

**Actual implementation:** Api and Hangfire have no call site; besides the interfaces and implementations, the rename
touched the `<see cref>` tags and comments that named the old members and every test method name that began with an old
member name (`TryAddAsync_…` → `TryAdd_…`, `GetAsync_…` → `Get_…`), because the acceptance grep covers `tests`. The
`docs/Cache.md` names change in step 06. The acceptance grep prints nothing with `--exclude-dir=bin --exclude-dir=obj`;
without it, stale binaries from an earlier Release build matched until the next Release build. A reflection dump of the
public surface differs from `d9552f6b` only in the five `C-13` method names.

