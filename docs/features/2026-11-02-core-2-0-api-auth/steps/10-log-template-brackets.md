---
spec: 2026-11-02-core-2-0-api-auth
step: 10
status: to-do
---

# Step 10: Log templates — `[{Placeholder}]` in Api (L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Templates in Api are not (`http-api#18`). Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0 accepts the change (D2), and it is listed in the release notes as L-03.

Former core step 70 (Api part); split per library on 2026-11-02. Renumbered from 09 on 2026-11-03 (new step 01, `C-28`).

## Affected components
- `src/SolTechnology.Core.API/Filters/ExceptionFilter.cs` — EDIT — `:67,93`

## Changes
- Property names stay the same.
- A check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.API src/SolTechnology.Core.Auth --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [ ] The grep check above prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.Auth.Tests` are green (log-asserting tests updated).

## Open questions
- none

## Deviations
