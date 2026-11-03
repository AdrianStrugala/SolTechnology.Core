---
spec: 2026-11-02-core-2-0-auid
step: 01
status: to-do
---

# Step 01: AUID — culture-invariant formatting, range-checked parsing

## Summary
`Auid.TryFormat` formats the timestamp with the current culture's calendar. Under `th-TH` the
year comes out as 2567, and the ID no longer round-trips (`B10`, `src/SolTechnology.Core.AUID/Auid.cs:286,291`).
`TryParse` never range-checks the time value, so dates before 2001 or after 2137-02-07 overwrite
the code bits and still return `true` (`B11`, `:345`). The Polish overflow exception message at
`:186` is a kept joke and stays verbatim (Guide §9.10; user, 2026-11-03: "nie dropuj joke z kodu").

Former core step 19; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — `:286,291` `CultureInfo.InvariantCulture`; `:345` range check; `:186` overflow message unchanged
- `tests/SolTechnology.Core.AUID.Tests/AuidParseStringTests.cs` — EDIT — out-of-range cases
- `tests/SolTechnology.Core.AUID.Tests/AuidToStringTests.cs` — EDIT — culture round-trip cases

## Changes
- `if (timeVal < 0 || timeVal > MaskTime) return false;`
- Persisted-contract note (premortem trigger): an out-of-range ID that 1.x parsed and stored now fails `TryParse`. Such IDs had corrupted code bits, so they never round-tripped; the core brief's "Release-notes inputs" section records it, and the 2.0 release notes (release step 05, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) mention it.
- The overflow message at `Auid.cs:186` is not changed; `AuidNewTests.TimestampOverflow_ShouldBeDetected_WhenYear2137Exceeded` and its comment quoting the message stay as written.
- Tests:
  - `[TestCase("th-TH")] [TestCase("ar-SA")] ToString_UnderCulture_RoundTrips`.
  - `[TestCase("20000101000000")] [TestCase("21380101000000")] TryParse_OutOfRange_ReturnsFalse`.
- Premortem RM-11 (core step 00, 2026-11-02, scenario 11): 1.x hosts whose current culture uses a non-Gregorian calendar wrote year-shifted strings, and consumers stored those strings as identifiers.
  - Replace the core brief's `auid 01` release-notes input with: "Hosts that ran 1.x under a non-Gregorian current culture (for example th-TH or ar-SA) wrote AUID strings with a shifted year. 2.0 rejects them in `TryParse` and in JSON, so records that store such strings cannot be loaded until the IDs are migrated."
  - Test `TryParse_1xThaiCalendarString_ReturnsFalse` uses a literal produced by `4097d47f` under `th-TH`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.AUID.Tests` is green, and the new tests fail on `4097d47f`.
- [ ] Canonical format of in-range IDs unchanged (existing round-trip tests green).
- [ ] `grep -c "Papież Polak robi BUM" src/SolTechnology.Core.AUID/Auid.cs` prints `1` (the joke message is unchanged).
- [ ] RM-11: `TryParse_1xThaiCalendarString_ReturnsFalse` is green, and the core brief's `auid 01` release-notes input uses the RM-11 wording.

## Open questions
- none

## Deviations
