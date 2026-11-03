---
spec: 2026-11-02-core-2-0-logging
step: 03
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green, and both new tests fail on `4097d47f`.

## Open questions
- none

## Deviations

### Path matcher extracted in this step

**Original plan:** Replace `string.StartsWith` with `PathString.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)` in `LoggingMiddleware.cs` and `LogDetailEnricher.cs`; step 07 extracts `LoggingPathMatcher.cs`.

**Actual implementation:** Both call sites use a new internal `Middleware/LoggingPathMatcher.cs` from this step, so the segment rule lives in one place. It trims a trailing `/` from a prefix (`StartsWithSegments("/health/")` would otherwise stop matching `/health/ready`) and ignores prefixes without a leading `/`, which never matched a request path before and would make `new PathString(...)` throw. A new `Enrichment/LogDetailEnricherTests.cs` pins the `LogDetail` endpoint rule (`/api/v1/TripsArchive` no longer matches `/api/v1/Trips`). Both planned tests fail on the pre-step code (the middleware and enricher files were unchanged since `4097d47f`): the `/healthcare/x` case and `Invoke_HandlerThrows_LogsExactlyOneError`.

### Client aborts also skip the Finished entry

**Original plan:** Skip the Finished entry when the exception was already logged.

**Actual implementation:** Applied to both catch blocks: a client abort logs its Warning entry and no Finished entry, so it no longer adds an Information "Finished … -> [200]" line for a response that never completed.
