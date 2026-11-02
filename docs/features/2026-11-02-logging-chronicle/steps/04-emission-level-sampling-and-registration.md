---
spec: 2026-11-02-logging-chronicle
step: 04
status: blocked
---

# Step 04: Chronicle emission — one report entry per root, level escalation, sampling, DI registration

## Summary
This step makes Chronicle observable. When a root completes, an internal reporter builds the
`ChronicleReport` (step 03), chooses the level, applies tail-based sampling (B8), and emits one
`[LoggerMessage]` entry, EventId 2150 `ChronicleReported` (LC-03). The level is Error on
failure, Warning when cancelled or slower than `SlowThreshold`, and Information otherwise.
`AddSolChronicle` (both overloads) registers the recorder, reporter, options validation, and
`TimeProvider`, and both `AddSolLogging` overloads call it. This edits the Logging installer
(premortem gate).

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleLogMessages.cs` — NEW — `internal static partial class` with `[LoggerMessage(EventId = 2150, EventName = "ChronicleReported", Message = "Chronicle [{ChronicleName}] [{ChronicleStatus}] in [{DurationMs} ms], [{StepCount}] steps: [{@Chronicle}]")] static partial void ChronicleReported(ILogger logger, LogLevel level, Exception? exception, string chronicleName, string chronicleStatus, long durationMs, int stepCount, ChronicleReport chronicle)`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleEmissionPolicy.cs` — NEW — internal, pure: `(LogLevel Level, bool ShouldEmit) Decide(ChronicleReport report, int? httpStatusCode)`; level rules and sampling from the brief; random source injected as `Func<double>` (default `Random.Shared.NextDouble`)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReporter.cs` — NEW — `internal sealed class ChronicleReporter(ILoggerFactory loggerFactory, IOptions<ChronicleOptions> options, ...) : IChronicleRootListener`; logger category `SolTechnology.Core.Logging.Chronicle`; skips roots with `IsReportedByCaller` (set by step 09)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — EDIT — `internal bool IsReportedByCaller`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `SlowThreshold` (`TimeSpan`, 1 s), `SampleRate` (`double`, 1.0) (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptionsValidator.cs` — EDIT — `SlowThreshold > TimeSpan.Zero`, `0 <= SampleRate <= 1`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleServiceRegistrations.cs` — NEW — internal; idempotent `TryAdd*`: `IChronicle` → `ChronicleRecorder` (singleton), `IChronicleRootListener` → `ChronicleReporter` (`TryAddEnumerable`), `IValidateOptions<ChronicleOptions>` → `ChronicleOptionsValidator`, `AddOptions<ChronicleOptions>().ValidateOnStart()`, `TimeProvider.System`
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` — EDIT — `extension(IServiceCollection services)`: `AddSolChronicle(Action<ChronicleOptions>? configure = null)`, `AddSolChronicle(IConfiguration configuration)` binding `ChronicleOptions.SectionName` (CH-07); stays ≤ 150 lines
- `src/SolTechnology.Core.Logging/LoggingServiceRegistrations.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "Guide conformance — Logging") — the shared path of both `AddSolLogging` overloads calls the Chronicle registrations; the `IConfiguration` overload binds `Logging:Core:Chronicle`
- `tests/SolTechnology.Core.Logging.Tests/SolTechnology.Core.Logging.Tests.csproj` — EDIT — `PackageReference Microsoft.Extensions.Diagnostics.Testing` (version via the `package-management` skill)
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleReporterTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleEmissionPolicyTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/ModuleInstallerTests.cs` — EDIT
- `tests/SolTechnology.Core.Logging.Tests/ModuleResolutionTests.cs` — EDIT (created by `2026-11-02-core-2-0-logging` step "DI resolution test — Logging") — `IChronicle` resolves with `ValidateOnBuild` and `ValidateScopes`

## Changes
- Status string in the template: `Ok`, `Failed`, `Cancelled` (`ChronicleStepStatus.ToString()`).
- Emission policy:
  - Always emit when the status is not `Ok`, when the level is ≥ Warning, or when
    `IsNPlusOneSuspected` is set.
  - Otherwise emit when `nextSample() < SampleRate`.
- B6 crux:
  - If the source generator rejects `{@Chronicle}`, switch to `[{Chronicle}]` and record a
    deviation.
  - Test `ChronicleReported_State_CarriesChronicleReportObject` asserts that the state KVP for
    `Chronicle`/`@Chronicle` holds the `ChronicleReport` instance, not a string.
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
  - `RootCompleted_EmitsExactlyOneEntryWithEventId2150`.
  - `ReportedByCallerRoot_NotEmittedByReporter`.
  - `Template_EveryPlaceholderBracketed` (the template string matches the `[{` … `}]` rule).
  - `AddSolChronicle_CalledTwice_RegistersOnce`.
  - `AddSolChronicle_InvalidOptions_FailsOnStart` (host start throws `OptionsValidationException`).
  - `AddSolLoggingConfiguration_BindsChronicleSection`.
  - `AddSolLoggingAction_RegistersChronicle`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests` is green.
- [ ] `grep -rnE "EventId = 2150" src --include=*.cs | wc -l` prints `1`, and no other `[LoggerMessage]` in `src` uses 2150.
- [ ] `wc -l < src/SolTechnology.Core.Logging/ModuleInstaller.cs` prints ≤ 150.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` (step 00) are applied.

## Open questions
- OQ-1 (brief): `CLAUDE.md §2` confirmation of `CH-04` (`SlowThreshold`, `SampleRate`) and `CH-07`.
- OQ-2 (brief): acceptance of `LC-03` and `LC-04`.

## Deviations
