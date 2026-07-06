# Plug-and-play OpenTelemetry support

> **Status:** Proposed
> **Created:** 2026-07-06

## Goal

Implement [ADR-015](../adr/015-opentelemetry-first-class-telemetry.md): ship
`AddSolTelemetry(this IHostApplicationBuilder, ...)` in `SolTechnology.Core.Logging`
(full OTel preset — tracing, metrics, OTel logging, OTLP export), and instrument the
modules that currently emit nothing (`MessageBus`, `Tale`, `SQL`, `Cache`) under the
`SolTechnology.Core.<Module>` stable-name contract. End state: one installer call gives
end-to-end distributed traces, the currently-lost `SolTechnology.Core.HTTP` metrics, and
trace-correlated logs in any OTLP backend.

## Scope

- In: `AddSolTelemetry` + `TelemetryOptions` (section `"Telemetry"`) + `TelemetryDefaults`
  wildcard constants in `SolTechnology.Core.Logging`.
- In: OTel packages in `SolTechnology.Core.Logging.csproj` (versions via the
  [package-management](../../.github/skills/package-management/SKILL.md) skill).
- In: MessageBus handler span + publish/handle counters; Tale tale/chapter spans; SQL
  unit-of-work span + connection-retry counter; Cache hit/miss counters.
- In: docs — `docs/Log.md` (AddSolTelemetry + names-contract table), Observability
  subsections in `docs/Bus.md`, `docs/Tale.md`, `docs/SQL.md`, `docs/Cache.md`.
- In: DreamTravel migration — replace hand-rolled OTel wiring in
  `DreamTravel.ServiceDefaults` with `AddSolTelemetry()`; E2E verification in the Aspire
  dashboard.
- Out: `SolTechnology.Core.Hangfire` instrumentation (no proven need yet; Hangfire has
  its own dashboard).
- Out: Redis command spans (`OpenTelemetry.Instrumentation.StackExchangeRedis` stays a
  consumer opt-in, documented in `docs/Cache.md`).
- Out: distributed-lock / idempotency-store counters (record under `docs/future-ideas/`
  if wanted later).
- Out: Azure Monitor / App Insights exporter presets — OTLP only; backends attach via
  their OTLP endpoints.

## Affected modules

- `src/SolTechnology.Core.Logging` (+ `tests/SolTechnology.Core.Logging.Tests`)
- `src/SolTechnology.Core.MessageBus` (+ tests)
- `src/SolTechnology.Core.Tale` (+ tests)
- `src/SolTechnology.Core.SQL` (+ tests)
- `src/SolTechnology.Core.Cache` (+ tests)
- `sample-tale-code-apps/DreamTravel` (`DreamTravel.ServiceDefaults`, `DreamTravel.Api`)
- `docs/Log.md`, `docs/Bus.md`, `docs/Tale.md`, `docs/SQL.md`, `docs/Cache.md`

## Semver impact

MINOR (five packages).

## Related

- Driving decision: [ADR-015](../adr/015-opentelemetry-first-class-telemetry.md)
- Steps: [2026-07-06-opentelemetry-support/summary.md](2026-07-06-opentelemetry-support/summary.md)
- [ADR-005](../adr/005-http-production-defaults.md) — `HttpClientMetrics` stable-name precedent.
