---
spec: 2026-07-06-opentelemetry-support
step: 07
status: to-do
---

# Step 07: DreamTravel migration + E2E verification

## Summary

Replaces DreamTravel's hand-rolled OTel wiring with `AddSolTelemetry()` and runs the
whole feature's end-to-end verification in the Aspire dashboard. This step doubles as the
feature-level verify gate: it must demonstrate the previously-lost HTTP metrics and the
cross-service trace continuity that motivated ADR-015.

## Affected components

- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.ServiceDefaults/Extensions.cs` — EDIT — replace `ConfigureOpenTelemetry` + `AddOpenTelemetryExporters` with `builder.AddSolTelemetry()`
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.ServiceDefaults/DreamTravel.ServiceDefaults.csproj` — EDIT — drop OTel refs that become transitive, add `SolTechnology.Core.Logging` ProjectReference if absent
- `DreamTravel.Api/Program.cs` — EDIT — delete the manual `AddOpenTelemetry().WithTracing(t => t.AddSource(CoreLoggingActivitySources.OperationsName))` block (redundant via wildcard)

## Changes

- `Extensions.cs`: keep service discovery, resilience defaults, and health-check mapping;
  telemetry concerns move to `AddSolTelemetry()`. `OTEL_EXPORTER_OTLP_ENDPOINT` gating
  behavior is identical by design (step 01).
- csproj: remove direct `OpenTelemetry.*` PackageReferences now satisfied transitively;
  keep any still directly used (verify with `dotnet build`).
- `Program.cs`: remove the redundant manual source subscription.

## Acceptance criteria

- [ ] `cd sample-tale-code-apps/DreamTravel && dotnet build` green.
- [ ] App runs under Aspire; dashboard shows:
  - [ ] CQRS operation spans (source `SolTechnology.Core.Logging.Operations`).
  - [ ] `soltechnology.core.http.retries` / `circuit_state_changes` metrics visible
        (regression-fix proof — lost today).
  - [ ] Service Bus publish → `messagebus.handle` spans linked across services in one
        trace.
  - [ ] `soltechnology.core.cache.hits`/`misses` counters visible.
  - [ ] Log entries carry a CorrelationId equal to the active TraceId.
- [ ] No behavior change in health checks / service discovery.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
