---
spec: 2026-11-02-logging-chronicle
step: 02
status: blocked
---

# Step 02: Chronicle — bounded recording and options validation

## Summary
A loop with 10,000 iterations must not turn a request into an unbounded tree. This step caps
recording at `MaxSteps`, `MaxDepth`, `MaxAttributesPerStep`, and `MaxAttributeValueLength`, and
counts whatever it drops so the report can show truncation markers. Steps dropped by `MaxSteps`
still feed a per-parent aggregate (count, total and max duration, failed count), so N+1 detection
(step 03) stays accurate even when nodes are truncated. The options are validated by an internal
`IValidateOptions<ChronicleOptions>`; step 04 wires it to `ValidateOnStart`.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `MaxSteps` (256), `MaxDepth` (12), `MaxAttributesPerStep` (8), `MaxAttributeValueLength` (200) (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptionsValidator.cs` — NEW — `internal sealed class ChronicleOptionsValidator : IValidateOptions<ChronicleOptions>` with the ranges from the brief's defaults table
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleNode.cs` — EDIT — per-group overflow aggregates (`Dictionary<string, ChronicleOverflow>` under the node lock), `DroppedDeeperCount`, attribute count and truncation
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOverflow.cs` — NEW — internal struct: count, total ticks, max ticks, failed count
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — EDIT — recorded-node counter (`Interlocked`), `DroppedStepCount`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStep.cs` — EDIT — a handle variant that holds (parent, group key, start timestamp) without a node, used for steps beyond `MaxSteps`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — apply the limits
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleLimitsTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleOptionsValidatorTests.cs` — NEW

## Changes
- `MaxSteps` counts recorded nodes per root, including the root.
  - Beyond the limit, `Step` returns an overflow handle that adds its duration and status to the
    parent's aggregate for its group key on `Dispose`.
  - The handle does not change the ambient node, so nested steps under it are counted against
    the same parent.
- `MaxDepth`: a step deeper than the limit is not recorded. The deepest recorded ancestor's
  `DroppedDeeperCount` is incremented.
- `MaxAttributesPerStep`: later keys are ignored and counted (`DroppedAttributeCount`). Setting an
  existing key overwrites it.
- `MaxAttributeValueLength`: a longer value is cut to the limit and suffixed with `[truncated]`.
- The group key equals the step name, except for SQL steps (step 07 sets it).
- Tests:
  - `MaxSteps_Exceeded_DropsNodesAndAggregatesOverflow` (`MaxSteps = 10`, 50 identical steps → 9 child nodes + overflow count 41).
  - `MaxDepth_Exceeded_CountsDeeperSteps`.
  - `MaxAttributesPerStep_Exceeded_IgnoresAndCounts`.
  - `AttributeValue_TooLong_TruncatedWithMarker`.
  - `[TestCase]` validator rows for every range bound (valid and invalid).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests --filter "FullyQualifiedName~Chronicles"` is green.
- [ ] `Step_ChronicleDisabled_AllocatesNothing` (step 01) is still green.
- [ ] `find src/SolTechnology.Core.Logging/Chronicles -name "*.cs" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.

## Open questions
- OQ-1 (brief): `CLAUDE.md §2` confirmation of `CH-04` (the four limit properties).

## Deviations
