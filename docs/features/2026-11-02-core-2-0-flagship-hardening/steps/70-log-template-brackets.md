---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 70
status: to-do
---

# Step 70: Log templates — `[{Placeholder}]` everywhere (log contract change L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Many templates in Tale, Api, HTTP,
Cache, and MessageBus are not (`cqrs-tale#24`, `http-api#18`, `logging-cache#20`, `infra#20`).
Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0
accepts the change (D2), and it is listed in the release notes as L-03.

## Affected components
- `src/SolTechnology.Core.Tale/**` — EDIT — templates at the former `TaleEngine.cs:95,104,228,241,284,377`, `TaleHandler.cs:138,179,193`, `TaleManager.cs:180,205`, `TaleController.cs:45,83,88,100,135,140,209` (locations moved by steps 62–63; find with `grep -rnE "Log(Information|Warning|Error|Debug|Critical)\(" src/SolTechnology.Core.Tale`)
- `src/SolTechnology.Core.API/Filters/ExceptionFilter.cs` — EDIT — `:67,93`
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — `:143` `TimeoutSeconds=[{Seconds}]s`
- `src/SolTechnology.Core.HTTP/HealthChecks/BaseUpstreamServiceHealthCheck.cs` — EDIT — `:110,119,126` `{HealthPath}` → `[{UpstreamURL}]`
- `src/SolTechnology.Core.Cache/RedisDistributedLockService.cs` (`:44,48,55,80,85`), `LocalDistributedLockService.cs` (`:28,32,38,48`), `RedisIdempotencyStore.cs` (`:54,75,90,103`) — EDIT — `[{LockKey}]`, `[{IdempotencyKey}]`
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` (`:38-40,96-98,111-113,145-147,163-165`), `Publish/MessagePublisher.cs` (`:40,46`) — EDIT — `[{MessageType}]`, `[{Entity}]`

## Changes
- Property names stay the same except `HealthPath` → `UpstreamURL` and `Key` → `IdempotencyKey`. Both are listed in the release notes.
- A repo-wide check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [ ] The grep check above prints nothing.
- [ ] `pwsh ./.github/runTests.ps1` is green (log-asserting tests updated).
- [ ] The brief's L-03 entry lists the two property renames; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations
