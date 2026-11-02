---
spec: 2026-11-02-core-2-0-logging
step: 04
status: to-do
---

# Step 04: Logging — sanitize inbound correlation IDs; safe timings and masking

## Summary
Three input-handling defects in Logging:

- A client's `X-Correlation-Id` is only length-checked, then echoed in the response and stamped
  into every log scope (`logging-cache#23`, `Correlations/CorrelationId.cs:86`, P-14).
- `ITimingService.GetTimings` exposes the live per-instance dictionary, although the docs claim
  `AsyncLocal` (`logging-cache#25`, `ITimingService.cs:5`).
- `PiiMask.Partial` throws on a negative `keepChars` and reveals 6 of 7 characters on short
  values (`logging-cache#25`, `PiiMask.cs:17`).

Former core step 31; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Logging/Correlations/CorrelationId.cs` — EDIT — allow only `[A-Za-z0-9._:-]`, otherwise generate a new ID (the file is already 164 lines; step 07 brings it under 150)
- `src/SolTechnology.Core.Logging/ITimingService.cs` — EDIT — XML doc matches the implementation
- `src/SolTechnology.Core.Logging/TimingService.cs` — EDIT — `GetTimings` returns a read-only snapshot
- `src/SolTechnology.Core.Logging/Masking/PiiMask.cs` — EDIT — `ArgumentOutOfRangeException.ThrowIfNegative(keepChars)`; reveal nothing unless `value.Length > 4 * keepChars`
- `tests/SolTechnology.Core.Logging.Tests/Correlations/CorrelationIdTests.cs` — EDIT
- `tests/SolTechnology.Core.Logging.Tests/Masking/PiiMaskTests.cs` — EDIT
- `tests/SolTechnology.Core.Logging.Tests/TimingServiceTests.cs` — EDIT

## Changes
- Tests:
  - `[TestCase("abc\r\nInjected")] [TestCase("<script>")] Resolve_InvalidInboundId_GeneratesNew`.
  - `GetTimings_ReturnsSnapshot`.
  - `[TestCase("1234567", 3)] Partial_ShortValue_FullyMasked`.
  - `Partial_NegativeKeep_Throws`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] Return types of `GetTimings` and `PiiMask.Partial` are unchanged (if a snapshot needs a new type, stop and request confirmation).

## Open questions
- none

## Deviations
