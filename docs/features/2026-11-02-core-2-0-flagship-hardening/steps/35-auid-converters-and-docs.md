---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 35
status: to-do
---

# Step 35: AUID — standards-compliant converters; honest uniqueness and ordering docs

## Summary
`AuidJsonConverter` mishandles bad input and dictionary keys:

- Invalid input throws `FormatException` with no JSON path, and an empty string silently becomes
  `Auid.Empty`.
- There is no `ReadAsPropertyName`/`WriteAsPropertyName`, so `Dictionary<Auid,T>` fails to
  serialize. Neither converter has tests (`logging-cache#22`, P-08).

`docs/AUID.md` also overstates uniqueness and ordering:

- It omits the 17-random-bit collision bound (`logging-cache#13`).
- It claims time ordering across codes, but codes ≥ `YGE` set the sign bit (`logging-cache#25`).

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
- [ ] `dotnet test tests/SolTechnology.Core.AUID.Tests` is green.
- [ ] The brief's P-08 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations
