---
spec: 2026-11-02-logging-chronicle
step: 04
status: to-do
---

# Step 04: Chronicle emission — one report entry per root, level escalation, sampling, DI registration

## Summary
This step makes Chronicle observable. When a root completes, an internal reporter builds the
`ChronicleReport` (step 03), chooses the level, applies tail-based sampling (B8), and emits one
`[LoggerMessage]` entry, EventId 2154 `ChronicleReported` (LC-03). The level is Error on
failure, Warning when cancelled or slower than `SlowThreshold`, and Information otherwise.
`AddSolChronicle` (both overloads) registers the recorder, reporter, options validation, and
`TimeProvider`, and both `AddSolLogging` overloads call it. This edits the Logging installer
(premortem gate).

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleLogMessages.cs` — NEW — `internal static partial class` with `[LoggerMessage(EventId = 2154, EventName = "ChronicleReported", Message = "Chronicle [{ChronicleName}] [{ChronicleStatus}] in [{DurationMs} ms], [{StepCount}] steps: [{@Chronicle}]")] static partial void ChronicleReported(ILogger logger, LogLevel level, Exception? exception, string chronicleName, string chronicleStatus, long durationMs, int stepCount, ChronicleReport chronicle)`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleEmissionPolicy.cs` — NEW — internal, pure: `(LogLevel Level, bool ShouldEmit) Decide(ChronicleReport report, int? httpStatusCode)`; level rules and sampling from the brief; random source injected as `Func<double>` (default `Random.Shared.NextDouble`)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReporter.cs` — NEW — `internal sealed class ChronicleReporter(ILoggerFactory loggerFactory, IOptions<ChronicleOptions> options) : IChronicleRootListener`; builds the report (step 03), asks `ChronicleEmissionPolicy`, logs; logger category `SolTechnology.Core.Logging.Chronicle`; skips roots with `IsReportedByCaller` (set by step 09); catches and logs (Warning, plain template) its own failures
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — EDIT — `internal bool IsReportedByCaller`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `SlowThreshold` (`TimeSpan`, 1 s), `SampleRate` (`double`, 1.0) (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptionsValidator.cs` — EDIT — `SlowThreshold > TimeSpan.Zero`, `0 <= SampleRate <= 1`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleServiceRegistrations.cs` — NEW — internal; idempotent `TryAdd*`: `IChronicle` → `ChronicleRecorder` (singleton), `IChronicleRootListener` → `ChronicleReporter` (`TryAddEnumerable`), `IValidateOptions<ChronicleOptions>` → `ChronicleOptionsValidator`, `AddOptions<ChronicleOptions>().ValidateOnStart()`, `TimeProvider.System`
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — `extension(IServiceCollection services)`: `AddSolChronicle(Action<ChronicleOptions>? configure = null)`, `AddSolChronicle(IConfiguration configuration)` binding `ChronicleOptions.SectionName` (CH-07); stays ≤ 150 lines
- `src/SolTechnology.Core.Logging/LoggingServiceRegistrations.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "Guide conformance — Logging") — the shared path of both `AddSolLogging` overloads calls the Chronicle registrations; the `IConfiguration` overload binds `Logging:Core:Chronicle`
- `tests/SolTechnology.Core.Logging.Tests/SolTechnology.Core.Logging.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version via the `package-management` skill)
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleReporterTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/StateCapturingLoggerProvider.cs` — NEW — test-only `ILoggerProvider` that keeps the original `TState` (`IReadOnlyList<KeyValuePair<string, object?>>`); `FakeLogger` stringifies state values, so it cannot prove B6
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleEmissionPolicyTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/ModuleInstallerTests.cs` — EDIT
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "DI resolution test — Logging") — `IChronicle` resolves with `ValidateOnBuild` and `ValidateScopes`

## Changes
- Status string in the template: `Ok`, `Failed`, `Cancelled` (`ChronicleStepStatus.ToString()`).
- Emission policy:
  - Always emit when the status is not `Ok`, when the level is ≥ Warning, or when
    `IsNPlusOneSuspected` is set.
  - Otherwise emit when `nextSample() < SampleRate`.
- B6 crux (settled by the plan review: `{@Name}` is supported since .NET 8, dotnet/runtime#69490):
  - If the build still reports `SYSLIB1014` for `{@Chronicle}`, switch to `[{Chronicle}]` and
    record a deviation.
  - Test `ChronicleReported_State_CarriesChronicleReportObject` uses `StateCapturingLoggerProvider`
    and asserts that the state KVP for `Chronicle`/`@Chronicle` holds the `ChronicleReport`
    instance, not a string.
- Other tests use `FakeLogCollector` and assert on `FakeLogRecord.Message` (rendered tree), level,
  `Id`, and `Exception`.
- The failed root's `Exception` is passed as the log exception, never embedded in the template
  (Guide §11).
- Tests:
  - `[TestCase]` level matrix in `ChronicleEmissionPolicyTests`:
    - ok fast → Information;
    - ok slow → Warning;
    - cancelled → Warning;
    - failed → Error;
    - HTTP 404 → Warning;
    - HTTP 500 → Error.
  - Sampling:
    - `SampleRate0_FastOk_NotEmitted`;
    - `SampleRate0_Failed_Emitted`;
    - `SampleRate0_Slow_Emitted`;
    - `SampleRate0_NPlusOne_Emitted`.
  - `RootCompleted_EmitsExactlyOneEntryWithEventId2154`.
  - `ReportedByCallerRoot_NotEmittedByReporter`.
  - `Template_EveryPlaceholderBracketed` (the template string matches the `[{` … `}]` rule).
  - `AddSolChronicle_CalledTwice_RegistersOnce`.
  - `AddSolChronicle_InvalidOptions_FailsOnStart` (host start throws `OptionsValidationException`).
  - `AddSolLoggingConfiguration_BindsChronicleSection`.
  - `AddSolLoggingAction_RegistersChronicle`.
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-4: the emission policy always emits when `HasFailedSteps` is true (step 03). Tests
    `SampleRate0_OkRootWithFailedChild_Emitted` and
    `SampleRate0_OkRootWithFailedOverflowStep_Emitted`.
  - CRM-6: only service registrations are `TryAdd`-guarded. The options `Bind` (the
    `IConfiguration` overload) and `Configure` (the `Action` overload) are applied on every call,
    whatever the call order relative to the parameterless calls made by other installers. Tests
    `AddSolChronicle_ParameterlessThenConfiguration_BindsSection` (`Enabled = false` from
    configuration wins), `AddSolChronicle_ConfigurationThenParameterless_KeepsSection`, and
    `AddSolChronicle_ActionAfterInstallerCall_Applies`.
  - CRM-14: the reporter derives the level from status, duration, and HTTP status **before**
    building the report, and builds nothing when `!logger.IsEnabled(level)`. Rendering runs only
    when a provider formats the entry (lazy `ToString`, step 03). Tests
    `CategoryAboveLevel_OkRoot_ReportNotBuilt` (an internal builder seam counts invocations) and
    `Root_TenSteps_Emitted_AllocatesUnderCeiling`. The ceiling is pinned at the first green
    measurement + 25% and recorded under `Deviations`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rnE "EventId = 2154" src --include=*.cs | wc -l` prints `1`, and no other `[LoggerMessage]` in `src` uses 2154.
- [ ] `wc -l < src/SolTechnology.Core.Logging/ModuleInstaller.cs` prints ≤ 150.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.
- [ ] CRM-4: `SampleRate0_OkRootWithFailedChild_Emitted` and `SampleRate0_OkRootWithFailedOverflowStep_Emitted` are green.
- [ ] CRM-6: the three `AddSolChronicle_*` call-order tests are green.
- [ ] CRM-14: `CategoryAboveLevel_OkRoot_ReportNotBuilt` and `Root_TenSteps_Emitted_AllocatesUnderCeiling` are green; the pinned ceiling is recorded under `Deviations`.

## Open questions
- none (OQ-1 confirmed 2026-11-02: `CH-04` `SlowThreshold`/`SampleRate`, `CH-07`; OQ-2 accepted 2026-11-02: `LC-03`, `LC-04`)

## Deviations
