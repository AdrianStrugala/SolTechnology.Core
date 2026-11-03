---
spec: 2026-11-02-core-2-0-auid
step: 02
status: done
---

# Step 02: AUID — standards-compliant converters; honest uniqueness and ordering docs

## Summary
`AuidJsonConverter` mishandles bad input and dictionary keys:

- Invalid input throws `FormatException` with no JSON path, and an empty string silently becomes
  `Auid.Empty`.
- There is no `ReadAsPropertyName`/`WriteAsPropertyName`, so `Dictionary<Auid,T>` fails to
  serialize. Neither converter has tests (`logging-cache#22`, P-08).

`docs/AUID.md` also overstates uniqueness and ordering:

- It omits the 17-random-bit collision bound (`logging-cache#13`).
- It claims time ordering across codes, but codes ≥ `YGE` set the sign bit (`logging-cache#25`).

Former core step 35; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.AUID/AuidJsonConverter.cs` — EDIT — `TryParse` + `JsonException`; empty string → `JsonException`; property-name overrides
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — `AuidTypeConverter` (`:441`) `ConvertFrom` uses `TryParse` and throws `NotSupportedException` with the value
- `docs/AUID.md` — EDIT — `:16,110-112` collision bound (~1% at 51 IDs/s per code, 50% at ~426 IDs/s) and ordering only within one code
- `tests/SolTechnology.Core.AUID.Tests/AuidJsonConverterTests.cs` — NEW
- `tests/SolTechnology.Core.AUID.Tests/AuidTypeConverterTests.cs` — NEW

## Changes
- Tests:
  - `Read_Invalid_ThrowsJsonExceptionWithPath`.
  - `Read_Empty_ThrowsJsonException`.
  - `RoundTrip_DictionaryKey`.
  - `TypeConverter_RoundTrip`.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.AUID.Tests` is green.
- [x] The core brief's P-08 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations

### Extra negative test for the type converter

**Original plan:** Four tests; `TypeConverter_RoundTrip` is the only type-converter test.

**Actual implementation:** `AuidTypeConverterTests.ConvertFrom_Invalid_ThrowsNotSupportedExceptionWithValue` was added because the `NotSupportedException` is a behavioural change (1.x threw `FormatException`) and needs a test that fails before the fix. Four of the five tests failed on the unchanged code; `TypeConverter_RoundTrip` is a regression pin. The JSON tests assert the exception `Path` (`$.Id`), and the invalid-input message names the value.

### Docs: the Random bullet, the Features list and a new section

**Original plan:** Edit `docs/AUID.md:16,110-112`.

**Actual implementation:** Besides those lines, the Random bullet links the bound, the Features bullet names dictionary-key support, and a new "Uniqueness and ordering" section holds the collision table (51 IDs/s ≈ 1%, ~426 IDs/s ≈ 50%) and the ordering rule (time order only within one code; codes `YGE`–`ZZZ` set the sign bit and sort before `AAA`). The P-08 row needs no change; the type-converter exception change is recorded for the release notes in the retrospective.
