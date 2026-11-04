---
spec: 2026-11-02-core-2-0-cache
step: 08
status: done
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
- [x] The grep check above prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green (log-asserting tests updated).
- [x] The core brief's L-03 entry lists the `Key` → `IdempotencyKey` rename; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations

### Template check and pinning tests

**Original plan:** the `grep -rnE` check from Changes; update the log-asserting tests.

**Actual implementation:** with BSD `grep` on macOS the planned regex matched none of the 13 old templates either, so it
proves nothing; the check that ran is
`perl -ne 'print if /Log\w*\(.*"[^"]*(?<!\[)\{[A-Z]\w*\}(?!\])/'` over every `.cs` file of the project: 13 hits on
`5b6aef31`, none after the change. No test asserted these templates, so three tests now pin every template, level,
and property value: `LocalDistributedLockServiceTests.TryAcquireLock_LogsBracketedLockKey` (four templates, the
warning forced by an overflowing `TimeSpan.MaxValue` expiry), `RedisDistributedLockServiceTests.TryAcquireLock_LogsBracketedLockKey`
(five templates through a substituted `IDatabase`), and `RedisIdempotencyStoreTests.Operations_RedisDown_LogBracketedIdempotencyKey`
(four templates). All three failed before the change and pass after it; they use a small `CapturingLogger<T>` in the
test project. `LockKey` keeps its name; `Key` → `IdempotencyKey`, which the L-03 entries of this brief and the core
brief now spell out.

