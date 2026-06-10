---
adr: 009-hangfire-persistent-events-and-jobs
step: 06 of 10
status: reviewed
---

# Step 06: Deprecate `Scheduler` + delete orphan `Jobs`

## Summary
Mark `SolTechnology.Core.Scheduler`'s public surface `[Obsolete]` pointing at the new
`SolTechnology.Core.Hangfire` recurring-jobs API (step 05), and delete the dead
`src/SolTechnology.Core.Jobs/` folder (bin/obj artifacts only — no `.csproj`, not in `.slnx`). This is
**plumbing / housekeeping**: no behavioural change to Scheduler, additive deprecation only, kept
separate from the new feature logic so the reviewer sees the deprecation in isolation. Independent of
steps 04–05 in code, but it should land **after** step 05 so the `[Obsolete]` message can name a real
replacement API.

## Affected components
- `src/SolTechnology.Core.Scheduler/ModuleInstaller.cs` — add `[Obsolete]` to
  `AddScheduledJob<T>(...)` with a message: "Deprecated. Use
  `SolTechnology.Core.Hangfire.AddRecurringJob<TJob>(cron)` (ADR-009)." Do **not** set
  `error: true` — keep it a warning so downstream consumers are nudged, not broken.
- `src/SolTechnology.Core.Scheduler/ScheduledJob.cs` — add `[Obsolete]` to the public abstract
  `ScheduledJob` class with the same pointer to `IJob` / `AddRecurringJob<TJob>`.
- `src/SolTechnology.Core.Scheduler/SolTechnology.Core.Scheduler.csproj` — bump
  `<Version>0.5.0</Version>` → `<Version>0.6.0</Version>` (additive `[Obsolete]` = MINOR per ADR-009
  §Consequences). Review-verified: current version is `0.5.0`, and `TreatWarningsAsErrors` is already
  `false` here, so the self-referential `[Obsolete]` members do not break the Scheduler build.
- `src/SolTechnology.Core.Jobs/` — **delete the entire folder.** Review-verified orphan: contains only
  `bin/` + `obj/`, has no `.csproj`, and `SolTechnology.Core.slnx` has no `Jobs` entry. Re-confirm with
  a solution-wide search before deleting and record the grep evidence in the PR.

## Details
- **Additive only.** Do not remove or change any Scheduler behaviour, signature, or configuration
  binding — only attach `[Obsolete]`. Existing Scheduler consumers keep compiling (with a warning).
- The `[Obsolete]` message is public-facing API documentation — write it as a complete sentence with
  the exact replacement symbol name finalised in step 05.
- **`TreatWarningsAsErrors` is `true` for every other `src/` project** (root `Directory.Build.props`),
  so a `CS0618` from any in-solution consumer of `AddScheduledJob<T>` / `ScheduledJob` would **break
  the build**. Review-verified there is currently **no** such consumer: DreamTravel schedules via
  Hangfire directly, and no core/test project references Scheduler. Re-run the search at implementation
  time; if a consumer has appeared, migrate it in this step or defer the obsoletion.
- Deleting `src/SolTechnology.Core.Jobs/` is safe only if it is truly orphaned (re-confirmed above).
- This step does **not** remove the Scheduler package or its docs. `Cron.md` gets its deprecation
  banner, and the repo-root `README.md` module table (which has a live `Scheduler` row) gets its
  deprecation marker, **in step 07**.

## Acceptance criteria
- `AddScheduledJob<T>` and `ScheduledJob` carry `[Obsolete]` with a message naming the
  `SolTechnology.Core.Hangfire` replacement.
- `Scheduler.csproj` version is `0.6.0`.
- `src/SolTechnology.Core.Jobs/` no longer exists on disk.
- `dotnet build SolTechnology.Core.slnx` is green (no new `CS0618`-as-error from an in-solution
  consumer).
- A solution-wide search for `SolTechnology.Core.Jobs` returns no references.

## Open questions
- Confirm (at implementation time) no hidden consumer of `AddScheduledJob<T>` exists in `tests/` or
  sample apps that would turn the new `CS0618` warning into a build-breaking error under
  `TreatWarningsAsErrors`. (Review snapshot: none.)
- Should the Scheduler package gain a NuGet-level deprecation too, or is the source-level attribute
  enough for now? Default: source attribute only; revisit at 1.0.

