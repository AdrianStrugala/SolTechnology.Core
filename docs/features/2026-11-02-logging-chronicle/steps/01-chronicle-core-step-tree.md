---
spec: 2026-11-02-logging-chronicle
step: 01
status: blocked
---

# Step 01: Chronicle core — ambient step tree and step handle

## Summary
This step adds the recording core of Chronicle to `SolTechnology.Core.Logging` as new files only,
with no DI registration and no emission yet. `IChronicle` starts roots and steps. A
`readonly struct ChronicleStep` handle records the name, start offset, duration, status, and
attributes. The tree lives in an internal `AsyncLocal`, so parallel `Task.WhenAll` branches build
isolated subtrees under a shared parent. Completed roots are handed to internal listeners, which
step 04 implements. Design: B1, B2, B9 in the brief.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/IChronicle.cs` — NEW — public interface (CH-01)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStep.cs` — NEW — public `readonly struct` handle (CH-02)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStepStatus.cs` — NEW — public enum (CH-03)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — NEW — public options class with `SectionName` and `Enabled` only (CH-04, first part)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleNode.cs` — NEW — internal sealed: name, group key, start/end timestamps, status, failure type/message, attributes (lazy array), children (lock-guarded list), parent, depth, idempotent close (`Interlocked`)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — NEW — internal sealed: root node, `StartedAt` (`TimeProvider.GetUtcNow()`), start timestamp, closed flag, `LateStepCount`, root exception reference
- `src/SolTechnology.Core.Logging/Chronicles/AmbientChronicle.cs` — NEW — internal static: `private static readonly AsyncLocal<ChronicleNode?> Current` (precedent `Correlations/CorrelationIdService.cs:5`)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — NEW — `internal sealed class ChronicleRecorder(IOptions<ChronicleOptions> options, TimeProvider timeProvider, IEnumerable<IChronicleRootListener> listeners) : IChronicle`
- `src/SolTechnology.Core.Logging/Chronicles/IChronicleRootListener.cs` — NEW — internal: `void OnRootCompleted(ChronicleRoot root)`
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleRecorderTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleConcurrencyTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleAllocationTests.cs` — NEW

## Changes
- `Begin(name)`:
  - No ambient node, or the ambient root is closed: starts a new root (B9).
  - Otherwise: starts a child of the ambient node.
  - Either way it sets the ambient node to the new node. It is a synchronous method, so the
    `AsyncLocal` write flows to the caller.
- `Step(name)`:
  - With an open root: a child of the ambient node.
  - With no root or `Enabled = false`: returns `default(ChronicleStep)`, a no-op.
  - With a closed root: the step is not recorded and `LateStepCount` is incremented.
- `ChronicleStep.Dispose()`:
  - Closes the node idempotently and restores the ambient node to the node's parent.
  - Status defaults to `Ok` unless `Fail`/`Cancel` was called.
  - Disposing a root closes it, marks still-open descendants unfinished (end = root end), and
    calls every `IChronicleRootListener`.
- `Fail(Exception)` stores the exception type name and message. For a root it also keeps the
  `Exception` reference for the log call. `Fail(string errorType, string? message)` stores the
  type and message only.
- `Set(key, value)` stores the value through `Convert.ToString(value, CultureInfo.InvariantCulture)`.
  `SetMasked` stores `PiiMask.Apply(value, mode, keepChars)`. The raw value is never kept.
- All timestamps come from `TimeProvider.GetTimestamp()`/`GetElapsedTime()`, never from
  `Stopwatch`.
- `ChronicleNode` and `ChronicleRoot` expose internal read access for tests; Logging already has
  `InternalsVisibleTo SolTechnology.Core.Logging.Tests`.
- Every public type and member has an XML `<summary>` (Logging ships `GenerateDocumentationFile`).
- Tests:
  - `Begin_NoAmbientRoot_StartsRoot`.
  - `Begin_InsideOpenRoot_StartsChild`.
  - `Begin_AfterRootClosed_StartsNewRoot`.
  - `Step_NoRoot_ReturnsNonRecordingHandle`.
  - `Step_AfterRootClosed_CountsLateStep`.
  - `Dispose_Twice_ClosesOnce`.
  - `Dispose_RootWithOpenChild_MarksChildUnfinished`.
  - `Fail_Exception_SetsFailedWithTypeAndMessage`.
  - `Cancel_SetsCancelled`.
  - `SetMasked_StoresMaskedValueOnly`.
  - `Durations_UseTimeProvider` (`FakeTimeProvider.Advance`).
  - `WhenAll_ParallelBranches_BuildsIsolatedSubtrees` (64 branches, each with 3 nested steps; every branch subtree is intact and siblings never nest into each other).
  - `ParallelAppends_ToOneParent_LoseNoChildren` (1,000 concurrent `Step` calls under one parent → exactly 1,000 children).
  - `Step_ChronicleDisabled_AllocatesNothing` (`GC.GetAllocatedBytesForCurrentThread()` delta is `0` over 1,000 `Step`+`Dispose` calls with `Enabled = false`).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests --filter "FullyQualifiedName~Chronicles"` is green.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] `find src/SolTechnology.Core.Logging/Chronicles -name "*.cs" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rn "Stopwatch" src/SolTechnology.Core.Logging/Chronicles` prints nothing.
- [ ] `grep -rn "public static" src/SolTechnology.Core.Logging/Chronicles` prints nothing.

## Open questions
- OQ-1 (brief): `CLAUDE.md §2` confirmation of `CH-01`, `CH-02`, `CH-03`, and `CH-04` (`SectionName`, `Enabled`).

## Deviations
