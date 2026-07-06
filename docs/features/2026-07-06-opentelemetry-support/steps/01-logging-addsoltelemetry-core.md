---
spec: 2026-07-06-opentelemetry-support
step: 01
status: to-do
---

# Step 01: Logging — `AddSolTelemetry` core

## Summary

Adds the OpenTelemetry preset to `SolTechnology.Core.Logging`: one
`AddSolTelemetry(this IHostApplicationBuilder, ...)` call wires tracing, metrics, OTel
logging, resource attributes, and gated OTLP export. This is the whole public-API delta
of the Logging package and the foundation every later step plugs into, so it ships as
one reviewable PR.

## Affected components

- `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` — EDIT — OTel packages, version bump
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — new `extension(IHostApplicationBuilder)` block
- `src/SolTechnology.Core.Logging/Telemetry/TelemetryOptions.cs` — NEW — options class
- `src/SolTechnology.Core.Logging/Telemetry/TelemetryDefaults.cs` — NEW — stable wildcard constants
- `src/SolTechnology.Core.Logging/Telemetry/TelemetryWiring.cs` — NEW — internal wiring helper
- `tests/SolTechnology.Core.Logging.Tests` — EDIT — new test fixture(s)

## Changes

- csproj: add `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
  `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`,
  `OpenTelemetry.Instrumentation.Runtime` — exact versions via the
  [package-management](../../../../.github/skills/package-management/SKILL.md) skill
  (never from memory); add rows to `references/canonical-versions.md`; align
  `DreamTravel.ServiceDefaults.csproj` versions to the resolved set in the same pass.
- csproj: `<Version>` `1.2.0` → `1.3.0`; extend `<Description>`/`<PackageTags>` with
  OpenTelemetry/OTLP.
- CLAUDE.md §2 dependency gate: report impact against
  [nuget-stats.json](../../../../nuget-stats.json) in the PR —
  `SolTechnology.Core.Logging` (2,878 downloads) is a dependency of API, HTTP, Hangfire,
  MessageBus, CQRS; all inherit the OTel graph transitively.
- NEW `TelemetryOptions` (`SectionName = "Telemetry"`):
  `string? ServiceName` (fallback `builder.Environment.ApplicationName`),
  `string? ServiceVersion` (fallback entry-assembly informational version),
  `bool EnableTracing = true`, `bool EnableMetrics = true`, `bool EnableLogging = true`,
  `bool EnableOtlpExporter = true`, `string? OtlpEndpoint` (validated absolute URI when
  set), `IReadOnlyCollection<string> AdditionalSources`,
  `IReadOnlyCollection<string> AdditionalMeters`. XML `<summary>` on type and members.
- NEW `TelemetryDefaults` (public static, stable contract — MAJOR bump to change):
  `SolSourcesWildcard = "SolTechnology.Core.*"`,
  `AzureMessagingSourcesWildcard = "Azure.Messaging.*"`.
- NEW internal `TelemetryWiring` — keeps `ModuleInstaller.cs` inside the Guide §9 size
  budget:
  1. `builder.Logging.AddOpenTelemetry(o => { o.IncludeScopes = true; o.IncludeFormattedMessage = true; })`
     when `EnableLogging`.
  2. `builder.Services.AddOpenTelemetry().ConfigureResource(...)`
     `.WithTracing(t => t.AddSource(TelemetryDefaults... + AdditionalSources).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation(); configureTracing?.Invoke(t))`
     `.WithMetrics(m => m.AddMeter(TelemetryDefaults.SolSourcesWildcard, AdditionalMeters...).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation(); configureMetrics?.Invoke(m))`
     honoring `EnableTracing`/`EnableMetrics`.
  3. OTLP gating: `OtlpEndpoint` set → `UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(...))`;
     else `builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]` non-empty →
     `UseOtlpExporter()`; else no exporter. All guarded by `EnableOtlpExporter`.
  4. `services.AddSolCorrelationIdService()` (idempotent).
- EDIT `ModuleInstaller.cs`: second `extension(IHostApplicationBuilder builder)` block
  (Guide §9.7) exposing
  `AddSolTelemetry(Action<TelemetryOptions>? configure = null, Action<TracerProviderBuilder>? configureTracing = null, Action<MeterProviderBuilder>? configureMetrics = null)`;
  binds section `"Telemetry"`, then applies `configure`; options chain ends
  `.ValidateDataAnnotations().ValidateOnStart()` (Guide §14); body delegates to
  `TelemetryWiring`.
- Tests (NUnit, per [test-writing](../../../../.github/skills/test-writing/SKILL.md)):
  - `[TestCase]`-parameterized options validation (invalid `OtlpEndpoint` fails at start;
    defaults bind from in-memory `"Telemetry"` section).
  - Per-signal switches: `EnableTracing=false` registers no `TracerProvider`;
    same pattern for metrics/logging.
  - `ActivityListener`/provider-based assertion that an `ActivitySource` named
    `SolTechnology.Core.Anything` is sampled (wildcard proof).
  - CorrelationId == `Activity.Current.TraceId` when telemetry is enabled.
  - Check whether `Microsoft.Extensions.Diagnostics.Testing` (`MetricCollector<T>`) is
    already available to test projects; add via package-management skill if not.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green (TreatWarningsAsErrors), no new
      `NU1901`–`NU1904` / `NU1605`.
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` green.
- [ ] Wildcard test proves any `SolTechnology.Core.*` source is subscribed.
- [ ] App start with no `"Telemetry"` config section and no env var succeeds (no
      exporter, no throw).
- [ ] nuget-stats impact report included in the PR description.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
