---
spec: 2026-11-02-logging-chronicle
step: 06
status: to-do
---

# Step 06: Chronicle flight recorder — buffered Debug/Trace entries attached only on failure

## Summary
With `FlightRecorder.Enabled`, an internal `ILoggerProvider` (alias `ChronicleFlightRecorder`,
design B4) buffers Debug and Trace entries written inside an active root. The buffer is bounded.
It is attached to the report only when the root fails; otherwise it is dropped. Outside a root,
or when the recorder is disabled, the provider's `IsEnabled` returns `false`, so no template is
formatted. Default filter rules (Debug for all categories; Warning for `Microsoft` and `System`)
are added only when the user configured no rule for the alias. Serilog hosts need
`writeToProviders: true` or `builder.Logging.AddSerilog()`, which step 15 documents.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/FlightRecorder/ChronicleFlightRecorderProvider.cs` — NEW — `[ProviderAlias("ChronicleFlightRecorder")] internal sealed class … : ILoggerProvider`
- `src/SolTechnology.Core.Logging/Chronicles/FlightRecorder/ChronicleFlightRecorderLogger.cs` — NEW — internal `ILogger`: `IsEnabled(level)` = enabled ∧ level ≤ `MinimumLevel` ∧ open ambient root ∧ category ≠ `SolTechnology.Core.Logging.Chronicle`; `Log` appends to the root buffer
- `src/SolTechnology.Core.Logging/Chronicles/FlightRecorder/ChronicleFlightRecorderFilterSetup.cs` — NEW — `internal sealed class … : IConfigureOptions<LoggerFilterOptions>`; adds the default rules only when no existing rule targets the alias or the provider type
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleFlightRecorderOptions.cs` — NEW — public (CH-05): `Enabled` (`false`), `MinimumLevel` (`Debug`), `MaxEntries` (100), `MaxMessageLength` (500)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleFlightRecord.cs` — NEW — public read-only (CH-06)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `FlightRecorder` property (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptionsValidator.cs` — EDIT — `MinimumLevel` ∈ {`Trace`, `Debug`}; `MaxEntries` 1–1000; `MaxMessageLength` 32–4096
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — EDIT — bounded buffer (lock-guarded; `DroppedFlightRecordCount`)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReport.cs` — EDIT — `FlightRecords` (CH-06)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReportBuilder.cs`, `ChronicleTreeRenderer.cs` — EDIT — attach and render the `flight recorder:` section only for `Failed` roots, plus the `+{n} records dropped` marker
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleServiceRegistrations.cs` — EDIT — `TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ChronicleFlightRecorderProvider>())`, the filter setup
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleFlightRecorderTests.cs` — NEW

## Changes
- Each record holds the offset from root start, the level, the category, the EventId, and the
  formatted message cut to `MaxMessageLength` with `[truncated]`. Its `ExceptionType` holds the
  exception type name only, never the stack trace.
- The provider is registered even when disabled, because `IsEnabled` gates every call. Enabling
  it is a configuration change only (no code or DI change); because the recorder reads
  `IOptions<ChronicleOptions>`, the change takes effect on restart.
- DI-cycle guard: the provider and its loggers depend only on `IOptions<ChronicleOptions>` and the
  internal ambient accessor, never on `IChronicle`, `ILoggerFactory`, or `ILogger<T>`
  (`ILoggerFactory` → providers → `IChronicle` → reporter → `ILoggerFactory` would be a cycle).
  `ModuleResolutionTests` (`ValidateOnBuild`) covers it.
- Tests (a `LoggerFactory` built from DI with `Logging:LogLevel:Default = Information`):
  - `FailedRoot_DebugEntries_AttachedToReport`.
  - `SucceededRoot_DebugEntries_Dropped`.
  - `NoRoot_IsEnabledDebug_ReturnsFalse`.
  - `RecorderDisabled_IsEnabledDebug_ReturnsFalse`.
  - `MaxEntries_Exceeded_KeepsFirstAndCountsDropped`.
  - `MicrosoftCategory_DebugNotBuffered_ByDefault`.
  - `UserRuleForAlias_WinsOverDefaults`.
  - `ChronicleCategory_NeverBuffered`.
  - `InformationEntries_NotBuffered`.
  - `AddSolChronicle_WithFlightRecorder_ResolvesLoggerFactoryWithoutCycle`.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-8: `ChronicleFlightRecorderLogger.Log` checks buffer capacity **before** invoking
    `formatter`; beyond `MaxEntries` it only increments `DroppedFlightRecordCount`. The buffer is
    allocated on the first record.
  - CRM-8: when `FlightRecorder.Enabled = false` at startup and no user rule targets the alias or
    provider type, `ChronicleFlightRecorderFilterSetup` adds a `LogLevel.None` rule for the alias
    instead of the Debug defaults, so MEL never consults the provider.
  - Tests `MaxEntries_Reached_FormatterNotInvoked` and
    `FlightRecorderDisabled_ProviderNotConsultedForDebug` (the provider's `IsEnabled` call count
    stays 0 for 1,000 `LogDebug` calls inside a root).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `Step_ChronicleDisabled_AllocatesNothing` (step 01) is still green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied (the registrations reached from the installers change).
- [ ] CRM-8: `MaxEntries_Reached_FormatterNotInvoked` and `FlightRecorderDisabled_ProviderNotConsultedForDebug` are green.

## Open questions
- none (OQ-1 confirmed 2026-11-02: `CH-04` `FlightRecorder`, `CH-05`, `CH-06` `FlightRecords`/`ChronicleFlightRecord`; OQ-2 accepted 2026-11-02: `LC-05`)

## Deviations
