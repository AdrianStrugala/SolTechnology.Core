---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 19
status: to-do
---

# Step 19: AUID — culture-invariant formatting, range-checked parsing, English overflow message

## Summary
`Auid.TryFormat` formats the timestamp with the current culture's calendar. Under `th-TH` the
year comes out as 2567, and the ID no longer round-trips (`B10`, `src/SolTechnology.Core.AUID/Auid.cs:286,291`).
`TryParse` never range-checks the time value, so dates before 2001 or after 2137-02-07 overwrite
the code bits and still return `true` (`B11`, `:345`). The overflow exception message is a Polish
meme (`P1-5`, `logging-cache#11`, `:186`).

## Affected components
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — `:286,291` `CultureInfo.InvariantCulture`; `:345` range check; `:186` message
- `tests/SolTechnology.Core.AUID.Tests/AuidNewTests.cs` — EDIT — `:197` message assertion; overflow throw through an injected `TimeProvider`
- `tests/SolTechnology.Core.AUID.Tests/AuidParseStringTests.cs` — EDIT — out-of-range cases
- `tests/SolTechnology.Core.AUID.Tests/AuidToStringTests.cs` — EDIT — culture round-trip cases

## Changes
- `if (timeVal < 0 || timeVal > MaskTime) return false;`
- Persisted-contract note (premortem trigger): an out-of-range ID that 1.x parsed and stored now fails `TryParse`. Such IDs had corrupted code bits, so they never round-tripped; the brief's "Release-notes inputs" section records it, and the 2.0 release notes (release step 05, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) mention it.
- Message: `"AUID timestamp overflow: seconds since 2001-01-01 exceed 32 bits (after 2137-02-07T06:28:15Z)."`
- Tests:
  - `[TestCase("th-TH")] [TestCase("ar-SA")] ToString_UnderCulture_RoundTrips`.
  - `[TestCase("20000101000000")] [TestCase("21380101000000")] TryParse_OutOfRange_ReturnsFalse`.
- Premortem RM-11 (step 00, 2026-11-02, scenario 11): 1.x hosts whose current culture uses a non-Gregorian calendar wrote year-shifted strings, and consumers stored those strings as identifiers.
  - Replace the brief's step 19 release-notes input with: "Hosts that ran 1.x under a non-Gregorian current culture (for example th-TH or ar-SA) wrote AUID strings with a shifted year. 2.0 rejects them in `TryParse` and in JSON, so records that store such strings cannot be loaded until the IDs are migrated."
  - Test `TryParse_1xThaiCalendarString_ReturnsFalse` uses a literal produced by `4097d47f` under `th-TH`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.AUID.Tests` is green, and the new tests fail on `4097d47f`.
- [ ] Canonical format of in-range IDs unchanged (existing round-trip tests green).
- [ ] `grep -n "Papie\|2137 " src/SolTechnology.Core.AUID/Auid.cs` shows only the English message.
- [ ] RM-11: `TryParse_1xThaiCalendarString_ReturnsFalse` is green, and the brief's step 19 release-notes input uses the RM-11 wording.

## Open questions
- none

## Deviations
