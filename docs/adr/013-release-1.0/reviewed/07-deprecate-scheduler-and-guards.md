---
adr: 013-release-1.0
step: 07 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): locked "stop packing now" (answer 10) — both projects are already outside
     .slnx so the pack glob never includes them; removed the "one final deprecated publish" ambiguity;
     stated explicitly that the [Obsolete] surface is the Guards entry type + GuardsExtensions, with
     the per-type *Guards classes under Specific/ covered transitively (a decision, not an omission).
     2026-06-30 (Tale decision): clarified that the renamed SolTechnology.Core.Story package is NOT
     [Obsolete]-able (no source under the old id) — its deprecate + unlist lives in the step-10
     runbook, like ApiClient.
     2026-06-30 (unlist decision — answer 1): the nuget.org-facing retirement of these ids is
     *unlist* (CI-automated, step 01's gated unlist-deprecated job), NOT server-side deprecate (which
     has no CLI/API — web-UI only). This step's `[Obsolete]` is the compile-time signal and is
     unaffected; the `<Description>` `[DEPRECATED]` banner never reaches nuget.org because these
     projects are not re-packed (IsPackable=false). -->

# Step 07: Deprecate `Scheduler` + `Guards` in source (`[Obsolete]` + package banner)

## Summary
Mark the two source-bearing deprecated libraries as obsolete at the API and package-metadata level so
consumers see the warning at compile time and on the nuget.org listing. These packages are **not**
bumped to `1.0` and stay out of the shared version default. The nuget.org-facing retirement (unlisting
every published version) is **CI-automated** in step 01's gated `unlist-deprecated` job, not a manual
action here; server-side *deprecation* (badge + successor message) has no CLI/API and is an optional
web-UI-only follow-up (step 10). Small, self-contained PR.

## Affected components
- `src/SolTechnology.Core.Scheduler/ModuleInstaller.cs` — EDIT — `[Obsolete]` on `AddScheduledJob<T>`.
- `src/SolTechnology.Core.Scheduler/ScheduledJob.cs` — EDIT — `[Obsolete]` on the public `ScheduledJob` base.
- `src/SolTechnology.Core.Scheduler/SolTechnology.Core.Scheduler.csproj` — EDIT — `<Description>` banner + `<PackageReleaseNotes>`; `<IsPackable>false>`.
- `src/SolTechnology.Core.Guards/Guards.cs`, `Guards/GuardsExtensions.cs` — EDIT — `[Obsolete]` on `Guards` + the `GuardsExtensions` methods.
- `src/SolTechnology.Core.Guards/SolTechnology.Core.Guards.csproj` — EDIT — `<Description>` banner + `<PackageReleaseNotes>`; `<IsPackable>false>`.

## Changes
- `[Obsolete("SolTechnology.Core.Scheduler is deprecated. Use SolTechnology.Core.Hangfire — AddSolRecurringJob<TJob>(cron). See dontreadme.md.", error: false)]`.
- `[Obsolete("SolTechnology.Core.Guards is deprecated. Use FluentValidation AbstractValidator<T> (auto-discovered by the CQRS pipeline). See dontreadme.md.", error: false)]`.
- **`[Obsolete]` surface for Guards (explicit decision, not an omission).** Mark the `Guards` entry
  type (`Guards.cs`) and the `GuardsExtensions` methods. The seven per-type guard classes under
  `Guards/Specific/` (`DecimalGuards`, `DoubleGuards`, `FloatGuards`, `IntGuards`, `LongGuards`,
  `ObjectGuards`, `StringGuards`) are reached **transitively** via `Guards.*` / the extension methods,
  so marking the entry points is the chosen, sufficient surface — annotating every `Specific/*` member
  is deliberately **not** done (it adds noise without changing what a consumer sees at their call site).
- `<Description>` prefixed `[DEPRECATED] …` so the banner shows on nuget.org and in the IDE package view.
- **Stop packing now (answer 10).** Set `<IsPackable>false>` on both `.csproj`. Both projects are also
  already absent from `SolTechnology.Core.slnx`, so the slnx-driven pack glob (step 01) never includes
  them regardless — the `<IsPackable>false>` is belt-and-suspenders and documents intent. There is
  **no** "one final deprecated publish"; existing consumers are protected by `[Obsolete]` + the
  nuget.org deprecate/unlist runbook (step 10). They keep their existing `0.5.0` on nuget.org.
- Neither project is in `SolTechnology.Core.slnx`, so `[Obsolete]` warnings cannot break the solution
  build (`TreatWarningsAsErrors=true` applies only to built projects).
- **`SolTechnology.Core.Story` is NOT deprecated here (decision 13).** After step 05b renames it to
  `SolTechnology.Core.Tale`, there is **no buildable source under the old `Story` id** to annotate
  with `[Obsolete]` — exactly the `ApiClient` situation. The old `Story` package is frozen at `0.8.0`
  and is **unlisted on nuget.org** (every version, via step 01's CI job; successor
  `SolTechnology.Core.Tale` is documented in the step-10 migration map) with a Story→Tale migration
  note + `dontreadme.md` row. This step stays scoped to the two source-bearing deprecated libs
  (`Scheduler`, `Guards`).

## Acceptance criteria
- [ ] `AddScheduledJob<T>`, `ScheduledJob`, `Guards`, and the `GuardsExtensions` methods carry
      `[Obsolete]` with a migration message.
- [ ] Both `.csproj` `<Description>` start with `[DEPRECATED]` and name the successor; both set
      `<IsPackable>false>`.
- [ ] Neither project produces a `.nupkg` in the step-01 pack glob (absent from `.slnx` + `IsPackable=false`).
- [ ] `dotnet build SolTechnology.Core.slnx` green (these projects are outside the solution).

## Open questions
- none — "stop packing now" is resolved at step 00 (answer 10).



