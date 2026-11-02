---
spec: 2026-11-02-core-2-0-tale
step: 16
status: to-do
---

# Step 16: Log templates — `[{Placeholder}]` in Tale (L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Templates in Tale are not (`cqrs-tale#24`). Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0 accepts the change (D2), and it is listed in the release notes as L-03.

Former core step 70 (Tale part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Tale/**` — EDIT — templates at the former `TaleEngine.cs:95,104,228,241,284,377`, `TaleHandler.cs:138,179,193`, `TaleManager.cs:180,205`, `TaleController.cs:45,83,88,100,135,140,209` (locations moved by steps 14–15; find with `grep -rnE "Log(Information|Warning|Error|Debug|Critical)\(" src/SolTechnology.Core.Tale`)

## Changes
- Property names stay the same.
- A check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Tale --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [ ] The grep check above prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green (log-asserting tests updated).

## Open questions
- none

## Deviations
