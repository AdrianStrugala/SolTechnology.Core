---
spec: 2026-11-02-core-2-0-cache
step: 08
status: to-do
---

# Step 08: Log templates — `[{Placeholder}]` in Cache (L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Templates in the Cache lock and idempotency services are not (`logging-cache#20`). Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0 accepts the change (D2), and it is listed in the release notes as L-03.

Former core step 70 (Cache part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Cache/RedisDistributedLockService.cs` (`:44,48,55,80,85`), `LocalDistributedLockService.cs` (`:28,32,38,48`), `RedisIdempotencyStore.cs` (`:54,75,90,103`) — EDIT — `[{LockKey}]`, `[{IdempotencyKey}]`

## Changes
- Property names stay the same except `Key` → `IdempotencyKey`, which is listed in the release notes.
- A check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Cache --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [ ] The grep check above prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green (log-asserting tests updated).
- [ ] The core brief's L-03 entry lists the `Key` → `IdempotencyKey` rename; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations
