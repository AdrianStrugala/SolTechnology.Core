---
spec: 2026-11-02-logging-chronicle
step: 08
status: to-do
---

# Step 08: Chronicle detached roots — `IChronicle.BeginRoot`

## Summary
`Begin` joins an open ambient root (B9). That is wrong for work that outlives its caller: the
default `InMemoryEventPublisher` runs handlers through `Task.Run`, which flows the request's
ambient node, so the event would become a late or unfinished child of the request. This step adds
`BeginRoot(name)`, which always starts a new root and restores the previous ambient node on
`Dispose` (B11). CQRS fire-and-forget events (step 10), MessageBus messages (step 13), and
Hangfire jobs (step 14) use it to guarantee their own report. The step is Logging-only plumbing.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/IChronicle.cs` — EDIT — NEW member `ChronicleStep BeginRoot(string name)` with XML docs (CH-11)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRecorder.cs` — EDIT — implement `BeginRoot`; start the root Activity under the same rules as step 05 (its parent is `Activity.Current`, so the trace stays linked)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleStep.cs` — EDIT — the handle carries the node to restore on `Dispose` (the previous ambient node for detached roots, the parent otherwise)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleRoot.cs` — EDIT — `internal string? DetachedFrom`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReportBuilder.cs` — EDIT — the root step carries the attribute `DetachedFrom` when set
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleDetachedRootTests.cs` — NEW

## Changes
- `BeginRoot(name)`:
  - `Enabled = false` → returns `default(ChronicleStep)` (no-op); an Activity is still started per step 05 when the source has listeners.
  - Otherwise starts a new `ChronicleRoot` and sets the ambient node to its root node.
  - When an open ambient root exists, `DetachedFrom` = that root's name. The outer root records nothing about the detached root.
- `Dispose` of a detached root restores the ambient node captured at `BeginRoot`, even when that node's root is still open (still only when the ambient node is the detached root, step 01 rule).
- Steps started after the detached root is disposed land in the restored node.
- Tests:
  - `BeginRoot_NoAmbientRoot_StartsRoot`.
  - `BeginRoot_InsideOpenRoot_StartsSeparateRoot` (two `IChronicleRootListener` callbacks; the outer root has no child for the detached root).
  - `BeginRoot_InsideOpenRoot_SetsDetachedFromAttribute`.
  - `BeginRoot_Dispose_RestoresPreviousAmbientNode`.
  - `BeginRoot_InTaskRunOutlivingCaller_ReportsOwnRootAndNoLateStepInCaller`.
  - `BeginRoot_ChronicleDisabled_ReturnsNonRecordingHandle`.
  - `BeginRoot_WithListener_ActivityParentIsAmbientActivity`.
- `Step_ChronicleDisabled_AllocatesNothing` (step 01) is still green; add a `BeginRoot` variant: `BeginRoot_ChronicleDisabled_AllocatesNothing`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Logging.Tests --filter "FullyQualifiedName~Chronicles"` is green.
- [ ] `find src/SolTechnology.Core.Logging/Chronicles -name "*.cs" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none (OQ-1 confirmed by the user 2026-11-02: `CH-11` `IChronicle.BeginRoot`; B11 accepted by the plan review)

## Deviations
