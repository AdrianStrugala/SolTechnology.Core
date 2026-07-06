# ADR-015: OpenTelemetry as the first-class telemetry stack, wired from `SolTechnology.Core.Logging`

> **Status:** Proposed
> **Decision Date:** 2026-07-06
> **Decision Maker:** Adrian Strugala

## Context

The libraries emit telemetry but nothing ships the wiring:

- One `ActivitySource` exists (`CoreLoggingActivitySources.OperationsName` =
  `"SolTechnology.Core.Logging.Operations"`, consumed by `LoggingPipelineBehavior` in
  `SolTechnology.Core.CQRS`) and one `Meter` (`HttpClientMetrics.MeterName` =
  `"SolTechnology.Core.HTTP"`).
- No package references OpenTelemetry. Every consumer hand-writes
  `AddOpenTelemetry().WithTracing(t => t.AddSource(...))` — and in practice forgets the
  metrics half: the `SolTechnology.Core.HTTP` meter is subscribed nowhere, including the
  DreamTravel sample, so retry/circuit-breaker metrics are silently lost.
- `SolTechnology.Core.Tale`, `.MessageBus`, `.SQL`, `.Cache` have no instrumentation;
  consumer-side handler work is invisible in distributed traces.

This contradicts the repo goal: a plug-and-play library set for backend APIs.

## Decision

1. **OpenTelemetry is the only supported telemetry stack.** No Serilog, no direct
   Application Insights SDK.
2. **The wiring preset lives in `SolTechnology.Core.Logging`**: one method
   `AddSolTelemetry(this IHostApplicationBuilder, ...)` configures tracing, metrics, OTel
   logging, resource attributes, and OTLP export. Full dependency preset:
   `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
   `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`,
   `OpenTelemetry.Instrumentation.Runtime`.
3. **Wildcard subscription is the contract.** `AddSolTelemetry` subscribes sources and
   meters `"SolTechnology.Core.*"` (plus `"Azure.Messaging.*"` for Service Bus SDK spans).
   Any module naming its `ActivitySource`/`Meter` `SolTechnology.Core.<Module>` is
   auto-subscribed; the name is a stable contract (MAJOR bump to change), same as
   `HttpClientMetrics` today.
4. **OTLP export is gated, not mandatory**: explicit `TelemetryOptions.OtlpEndpoint`
   wins, else the standard `OTEL_EXPORTER_OTLP_ENDPOINT` env var, else no exporter
   (providers still run, so `CorrelationId` == W3C trace id everywhere).
5. **The remaining modules get instrumented** under the naming contract:
   `SolTechnology.Core.MessageBus` (handler span + publish/handle counters),
   `SolTechnology.Core.Tale` (tale + chapter spans), `SolTechnology.Core.SQL`
   (unit-of-work span + connection-retry counter), `SolTechnology.Core.Cache`
   (hit/miss counters).

## Alternatives Considered

1. **New `SolTechnology.Core.Telemetry` package.** Pros: Logging stays dependency-lean;
   OTel weight is opt-in. Cons: splits one concern in two — `CorrelationId` already
   reads `Activity.Current`, and correlation without export wiring is exactly the
   half-built state this ADR removes; a 21st package to discover and version. Rejected
   (user decision 2026-07-06: extend the Logging library).
2. **Status quo — documented hand-wiring only.** Zero dependency cost. Rejected: proven
   to fail; even the in-repo sample lost the HTTP metrics, and every consumer copies
   ~20 lines of boilerplate.
3. **Per-module self-wiring (each `ModuleInstaller` registers its own OTel pieces).**
   Rejected: source/meter subscription happens on the host's single OTel builder, and
   this would push OTel package references into every module instead of one.
4. **Minimal wiring (only `OpenTelemetry.Extensions.Hosting`, consumer adds exporter +
   ASP.NET Core instrumentation).** Lighter, but leaves ~6 lines of per-service
   boilerplate and an incomplete default. Rejected (user decision 2026-07-06: full
   preset).

## Consequences

**Positive:**
- One call (`builder.AddSolTelemetry()`) yields end-to-end traces
  (HTTP → CQRS → SQL → Service Bus → remote handler), metrics, and trace-correlated logs
  exported over OTLP to any backend.
- The already-emitted `SolTechnology.Core.HTTP` metrics become visible again.
- The naming convention doubles as the subscription contract — future modules are
  auto-covered with no Logging change.

**Negative:**
- Five OpenTelemetry packages become transitive dependencies of every
  `SolTechnology.Core.Logging` consumer (API, HTTP, Hangfire, MessageBus, CQRS).
- Services enabling tracing see correlation ids shift from random GUIDs to W3C
  trace-derived ids (designed behavior; release-notes callout required).
- `UseOtlpExporter` throws when called twice — consumers with their own exporter setup
  must set `TelemetryOptions.EnableOtlpExporter = false`.

**Semver impact:** MINOR (`SolTechnology.Core.Logging`, `.MessageBus`, `.Tale`, `.SQL`,
`.Cache`).

## Related

- Implemented via [2026-07-06-opentelemetry-support](../features/2026-07-06-opentelemetry-support.md)
- [ADR-005](005-http-production-defaults.md) — origin of `HttpClientMetrics` and its
  stable-name contract.
- [ADR-002](002-Story-Framework-Implementation.md) — listed "OpenTelemetry
  `ActivitySource` + per-chapter metrics" as future work; delivered here.
- [`docs/Log.md`](../Log.md) — module doc that documents the consumer-facing API.
