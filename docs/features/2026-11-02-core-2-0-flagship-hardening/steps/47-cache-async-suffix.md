---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 47
status: to-do
---

# Step 47: Cache — drop `Async` from lock and idempotency contracts

## Summary
`IDistributedLockService.TryAcquireLockAsync` (`IDistributedLockService.cs:23`) and
`IIdempotencyStore.TryAddAsync/GetAsync/SetResponseAsync/RemoveAsync` (`IIdempotencyStore.cs:31,37,43,48`)
carry the `Async` suffix with no sync overload (Guide §10). The four implementations are already
`internal sealed`, so only the two public interfaces change (`C-13`).

## Affected components
- `src/SolTechnology.Core.Cache/IDistributedLockService.cs` — EDIT
- `src/SolTechnology.Core.Cache/IIdempotencyStore.cs` — EDIT
- `src/SolTechnology.Core.Cache/LocalDistributedLockService.cs`, `RedisDistributedLockService.cs`, `LocalIdempotencyStore.cs`, `RedisIdempotencyStore.cs` — EDIT
- `src/SolTechnology.Core.API/**`, `src/SolTechnology.Core.Hangfire/**` — EDIT — call sites, if any (`grep -rn "TryAcquireLockAsync\|TryAddAsync\|SetResponseAsync" src`)
- `tests/SolTechnology.Core.Cache.Tests/**` — EDIT

## Changes
- Pure rename. No forwarders.

## Acceptance criteria
- [ ] `grep -rnE "TryAcquireLockAsync|TryAddAsync|SetResponseAsync" src tests sample-tale-code-apps` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green; `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none (`C-13` confirmed by the user on 2026-11-02)

## Deviations
