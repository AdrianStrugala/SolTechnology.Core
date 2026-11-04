---
spec: 2026-11-02-core-2-0-api-auth
step: 10
status: done
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
- [x] The grep check above prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.Auth.Tests` are green (log-asserting tests updated).

## Open questions
- none

## Deviations

### Multi-line template check and a pinning test

**Original plan:** the single-line `grep` prints nothing; log-asserting tests are updated.

**Actual implementation:** both `ExceptionFilter` templates span several lines (the call's arguments and a `+`
concatenation), so the planned `grep` printed nothing even on the unfixed file. A multi-line `perl` check over
`git ls-files 'src/SolTechnology.Core.API/*.cs' 'src/SolTechnology.Core.Auth/*.cs'` (every string literal inside a
`Log*(...)` call, each `{Name}` must sit between `[` and `]`) reported the six bare placeholders of the old file and
prints nothing now; the planned `grep` also prints nothing. No test asserted the old templates, so
`ExceptionFilterTests.OnException_LogsBracketedTemplate` (mapped and unmapped cases) now pins both templates and their
levels; it failed before the change and passes. The Auth templates were already bracketed (step 04 added the third one
with brackets). New templates: `Unhandled exception in [{RequestMethod}] [{RequestPath}] → [{StatusCode}]` and
`Unmapped exception of type [{ExceptionType}] in [{RequestMethod}] [{RequestPath}] — rethrowing to host pipeline. Add an
explicit mapping if this exception type is expected.`

