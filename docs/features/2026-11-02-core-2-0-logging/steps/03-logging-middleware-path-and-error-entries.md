---
spec: 2026-11-02-core-2-0-logging
step: 03
status: to-do
---

# Step 03: Logging — segment-aware path matching; one Error entry per failure

## Summary
Two `LoggingMiddleware` defects:

- Path matching uses `string.StartsWith`, so `/health` also swallows `/healthcare/...` and
  `/live` swallows `/livestream` (`logging-cache#14`, `LoggingMiddleware.cs:235`,
  `Enrichment/LogDetailEnricher.cs:69`).
- One failure logs two Error entries ("failed after" and then "Finished … -> [500]"), so
  error-rate alerts double-count (`logging-cache#18`, `:112-148`, P-15).

Former core step 30; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Logging/Middleware/LoggingMiddleware.cs` — EDIT — `PathString.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)`; skip the Finished entry when the exception was already logged
- `src/SolTechnology.Core.Logging/Enrichment/LogDetailEnricher.cs` — EDIT — `:69` same matcher (the file is already 155 lines; step 07 brings it under 150)
- `tests/SolTechnology.Core.Logging.Tests/Middleware/LoggingMiddlewareTests.cs` — EDIT

## Changes
- Tests:
  - `[TestCase("/healthcare/x", true)] [TestCase("/health", false)] [TestCase("/health/ready", false)] Invoke_PathMatching_LogsBusinessRoutes`.
  - `Invoke_HandlerThrows_LogsExactlyOneError`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green, and both new tests fail on `4097d47f`.

## Open questions
- none

## Deviations
