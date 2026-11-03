---
spec: 2026-11-02-core-2-0-tale
step: 16
status: done
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
- [x] The grep check above prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green (log-asserting tests updated).

## Open questions
- none

## Deviations

### Tuned check and a regression test

**Original plan:** The single-line grep prints nothing; log-asserting tests updated.

**Actual implementation:** 18 templates were bracketed (property names unchanged), plus the
`WhenLost` example in the `Tale<TContext,TOutput>` XML documentation. The step's grep only sees
single-line calls, so a tuned check also scans the two lines after each `Log*(` call:
`grep -rnE -A2 "Log(Information|Warning|Error|Debug|Critical|Trace)\(" src/SolTechnology.Core.Tale --include=*.cs | grep -E '"[^"$]*([^[]|^)\{[A-Z][A-Za-z]*\}' | grep -vE '\$"'`.
Both print nothing now; the tuned one listed the old templates on `6c0d979c`. No existing test
asserted an unbracketed template (the RM-14 and RM-17 warnings were bracketed from the start), so
`TaleHandlerTests.TaleLogs_StartAndResume_BracketEveryPlaceholder` was added: it records every
template of a start and a resume and fails on `6c0d979c`. The final Tale templates are
`Tale [{Handler}] processing...`, `Tale started with ID [{TaleId}]`, `Tale [{TaleId}] resuming from
saved state`, `Resuming tale at chapter [{ChapterId}]`, `Executing chapter: [{ChapterId}]`,
`Chapter [{ChapterId}] failed`, `Tale [{Handler}] failed`, `Failed to persist terminal tale state
for [{TaleId}]`, `Input for chapter [{ChapterId}] could not be deserialized`, `Interactive chapter
[{ChapterId}] rejected input: [{Error}]`, `Error executing tale [{Handler}]`, `Failed to load tale
state for [{TaleId}]`, `Starting tale: [{HandlerType}]`, `Failed to start tale [{HandlerType}]`,
`Resuming tale: [{TaleId}]`, `Failed to resume tale [{TaleId}]`, `Failed to extract result for tale
[{TaleId}]`, and the RM-14 and RM-17 warnings.
