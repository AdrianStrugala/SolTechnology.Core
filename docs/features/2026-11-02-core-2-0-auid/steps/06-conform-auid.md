---
spec: 2026-11-02-core-2-0-auid
step: 06
status: to-do
---

# Step 06: Guide conformance — AUID

## Summary
`Auid.cs` has 460 lines, and `TryParse` is about 45 (`logging-cache#19,21,26`), far over the
150-line hard cap. This is a behaviour-preserving refactor into partial files.

Former core step 67 (AUID part) plus the AUID-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — partials `Auid.Format.cs`, `Auid.Parse.cs`, `Auid.Encoding.cs` (NEW)

## Changes
- No public or protected signature change; no log-template change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.
- Premortem RM-16 (core step 00, 2026-11-02, scenario 16): the `Auid` partial split must not change the AUID string format or the converter behaviour. These guard tests keep their names and assertions:
  - AUID (steps 01, 02): `ToString_UnderCulture_RoundTrips`, `TryParse_OutOfRange_ReturnsFalse`, `TryParse_1xThaiCalendarString_ReturnsFalse`, `Read_Empty_ThrowsJsonException`, `RoundTrip_DictionaryKey`.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.AUID -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.AUID.Tests` is green.
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.AUID --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [ ] RM-16: `git diff --stat -- tests/SolTechnology.Core.AUID.Tests` for this step prints nothing; the guard tests above are green.

## Open questions
- none

## Deviations
