---
spec: 2026-07-06-opentelemetry-support
step: 03
status: to-do
---

# Step 03: Tale telemetry

## Summary

Adds tracing to the Tale framework: one span per tale execution and one per chapter, so a
tale run shows as a waterfall in any trace backend. Zero new dependencies
(`System.Diagnostics.ActivitySource` is BCL) and no installer change — a static source
mirroring `CoreLoggingActivitySources` keeps the diff surgical.

## Affected components

- `src/SolTechnology.Core.Tale/Telemetry/TaleActivitySources.cs` — NEW — static source + name constant
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — chapter spans
- `src/SolTechnology.Core.Tale/TaleHandler.cs` or `Orchestration/TaleManager.cs` — EDIT — tale-level span (whichever owns the execution entry point; decide at implementation, record in `## Deviations` if `TaleManager`)
- `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` — EDIT — minor version bump
- `tests/SolTechnology.Core.Tale.Tests` — EDIT — span tests

## Changes

- NEW `TaleActivitySources` (stable contract — MAJOR bump to change): mirrors
  `CoreLoggingActivitySources` — `public const string Name = "SolTechnology.Core.Tale"`,
  `public static readonly ActivitySource Source = new(Name)`, XML doc with the
  zero-cost-when-unsubscribed note.
- Tale-level span `tale {handlerTypeName}` (`ActivityKind.Internal`) around a tale
  execution: tags `tale.id`, `tale.handler`; terminal tag `tale.status` from
  `TaleStatus`; `SetStatus(Error)` + `AddException` on failure.
- Chapter span `chapter {ChapterId}` inside `TaleEngine` chapter execution: tags
  `tale.id`, `chapter.id`; terminal tag `chapter.status`; `SetStatus(Error)` on failure.
- Pause/resume rule: a span never outlives the process — each resume of a persisted tale
  starts a fresh `tale {handler}` span; the shared `tale.id` tag is the join key across
  spans.
- csproj: minor version bump.
- Tests (`ActivityListener`): a tale with N chapters emits 1 tale span + N chapter spans
  with matching `tale.id`; failed chapter sets `ActivityStatusCode.Error` on both chapter
  and tale spans; pause → resume yields two tale spans sharing `tale.id`.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green.
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` green.
- [ ] No `ModuleInstaller.cs` change (static source only); deviation recorded if one
      proves necessary.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
