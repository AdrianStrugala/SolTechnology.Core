---
spec: 2026-11-02-core-2-0-auid
step: 06
status: done
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
- [x] `find src/SolTechnology.Core.AUID -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.AUID.Tests` is green.
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.AUID --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).
- [x] RM-16: `git diff --stat -- tests/SolTechnology.Core.AUID.Tests` for this step prints nothing; the guard tests above are green.

## Open questions
- none

## Deviations

### Two more files than planned

**Original plan:** Split `Auid.cs` into the partials `Auid.Format.cs`, `Auid.Parse.cs` and `Auid.Encoding.cs`.

**Actual implementation:** With only those three, `Auid.cs` would still hold about 175 lines of factories and equality code. So the factories moved into a fourth partial, `Auid.New.cs` (114 lines). `AuidTypeConverter` moved into its own `AuidTypeConverter.cs`, because files mirror their primary type (Guide §10). `Auid.cs` now holds the layout constants, `Value`, `Empty` and equality (73 lines); the largest file is `Auid.Parse.cs` (131). `TryParse` delegates to `TryParseCode`, `TryParseSeconds` and `TryParseRandom`. Code generation uses small `ToUpperASCII`, `IsLetterAToZ` and `IsVowel` helpers. The two code validations share `ValidateCode`, and value assembly uses `Compose`. Braceless bodies got braces (Guide §9.11), and the five-line bit-layout comment became one line. The `New<T>()` XML example now shows the real format (`ORD_20241205123456_012345` instead of `ORD_2B1A3F12_1A2B3`). The Polish overflow message moved verbatim into `Auid.Encoding.cs`: `grep -rc "Papież Polak robi BUM" src/SolTechnology.Core.AUID --include=*.cs` counts 1, in that file.

### Verification details

**Original plan:** Compare public APIs and run the scoped placeholder check.

**Actual implementation:** `Microsoft.DotNet.ApiCompat.Tool` 10.0.403 (installed into the gitignored `.scratch/`) compared the Release `SolTechnology.Core.AUID.dll` before and after the split in both directions with `--strict-mode`. Both runs reported no differences. AUID has no `ILogger` or `[LoggerMessage]` usage, so the placeholder check prints nothing and the core brief's L-03 entry needs no AUID property. RM-16: `git diff --stat -- tests/SolTechnology.Core.AUID.Tests` is empty and all 102 AUID tests are green, including the guard tests.
