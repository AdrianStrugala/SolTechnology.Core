---
spec: 2026-11-02-core-2-0-http
step: 10
status: to-do
---

# Step 10: Log templates — `[{Placeholder}]` in HTTP (L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Templates in HTTP are not (`http-api#18`). Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0 accepts the change (D2), and it is listed in the release notes as L-03.

Former core step 70 (HTTP part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — `:143` `TimeoutSeconds=[{Seconds}]s`
- `src/SolTechnology.Core.HTTP/HealthChecks/BaseUpstreamServiceHealthCheck.cs` — EDIT — `:110,119,126` `{HealthPath}` → `[{UpstreamURL}]`

## Changes
- Property names stay the same except `HealthPath` → `UpstreamURL`, which is listed in the release notes.
- A check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.HTTP --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [ ] The grep check above prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green (log-asserting tests updated).
- [ ] The core brief's L-03 entry lists the `HealthPath` → `UpstreamURL` rename; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
