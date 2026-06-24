---
adr: 012-production-pattern-adoption-wave-2
step: 20 of 24
status: to-do
---

# Step 20: A5 — Per-request timing diagnostics (`Core.Logging`)

## Summary
Add a generalised `TimingService` to `Core.Logging` that records total request time and **named
sub-context timings** (`using TimingService.UseTimingContext("db")`), backed by `AsyncLocal` and
sourced from the injectable `TimeProvider` (testable), and emits the aggregated map into the request
log scope on finish. A lightweight "where did the time go in this request" breakdown without a full
APM. Drops the harvested app-specific coupling.

## Affected components
- `src/SolTechnology.Core.Logging/Operations/TimingService.cs` (or `Diagnostics/`) — the timing
  service (`AsyncLocal` storage, named sub-contexts, aggregation).
- `src/SolTechnology.Core.Logging/` — emit the aggregated timing map into the request log scope on
  finish (hook into the existing operation/scope finish path — see `OperationLogMessages.cs` /
  `Operations/`).
- `src/SolTechnology.Core.Logging/ValueStopwatch.cs` — reuse the existing stopwatch primitive rather
  than introducing a new one.
- `docs/Log.md` — document the timing API and the emitted scope field.
- `tests/SolTechnology.Core.Logging.Tests/` — sub-context aggregation + `TimeProvider`-driven timing
  + scope-emission tests.

## Details
- **`TimeProvider`-sourced (ADR-010 G1):** all timing reads go through `TimeProvider` so tests can
  use `FakeTimeProvider` — do **not** use `Stopwatch.GetTimestamp()`/`DateTime.UtcNow` directly
  unless via the existing `ValueStopwatch` already in the module.
- **`AsyncLocal` storage:** the timing context flows with the async request; nested
  `UseTimingContext("name")` blocks aggregate into a per-name total. The disposable returned by
  `UseTimingContext` stops/accumulates on dispose.
- **Emission:** on request finish, push the aggregated `{ name → elapsed }` map into the request log
  scope (one structured field) so it lands in Seq/App Insights alongside the correlation id. Reuse
  the existing scope/operation finish path rather than adding a new middleware.
- **Generalise:** the harvested version was a static service coupled to the app; keep the public
  surface minimal and DI-friendly, with the static convenience entry only if it matches the module's
  existing style.

## Acceptance criteria
- `UseTimingContext("name")` records and aggregates nested timings per name.
- All timing is `TimeProvider`-sourced and deterministic under `FakeTimeProvider`.
- The aggregated timing map is emitted into the request log scope on finish.
- `docs/Log.md` documents the API and the emitted field.
- Tests cover aggregation, fake-clock timing, and scope emission.

## Open questions
- Static `TimingService` (matches the harvest) vs an injected `ITimingService`. Recommend following
  the module's existing pattern (the correlation/scope helpers) — likely an injected service with an
  `AsyncLocal` backing; flag for the reviewer.

