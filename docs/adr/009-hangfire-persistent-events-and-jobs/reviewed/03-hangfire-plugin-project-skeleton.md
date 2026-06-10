---
adr: 009-hangfire-persistent-events-and-jobs
step: 03 of 10
status: reviewed
---

# Step 03: Hangfire plugin project skeleton + dependency report

## Summary
Stand up the new package `SolTechnology.Core.Hangfire` as a compiling, packable skeleton with its
dependency surface and packaging metadata, and record the new external dependency per CLAUDE.md §1.
No event/job logic yet — that lands in steps 04–05. This is pure plumbing: a `.csproj`, a `.slnx`
registration, and a dependency-impact note. Kept separate from the logic steps so the reviewer sees
the dependency adoption (and its CVE mitigation) in isolation.

## Affected components
- `src/SolTechnology.Core.Hangfire/SolTechnology.Core.Hangfire.csproj` — **new**. Model on
  `src/SolTechnology.Core.Scheduler/SolTechnology.Core.Scheduler.csproj`:
  - `<Version>0.1.0</Version>`, `<Product>` / `<PackageId>` / `<AssemblyName>` =
    `SolTechnology.Core.Hangfire`.
  - `<Description>` + `<PackageTags>` (e.g. `sol technology core hangfire persistent-events
    background-jobs recurring-jobs cqrs dotnet`).
  - `<PackageIcon>docs\mini-logo.png</PackageIcon>` + the `<None Include="..\..\docs\mini-logo.png" …>`
    pack item.
  - `<PackageReadmeFile>docs\readme.md</PackageReadmeFile>` + `<None Include="..\..\docs\Hangfire.md"
    … PackagePath="docs\readme.md" />` (the `Hangfire.md` file itself is authored in step 07; the
    pack reference can exist before the content is final).
  - `<PackageReference Include="Hangfire.Core" Version="1.8.22" />` — the proven DreamTravel version.
  - `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />` — **mandatory pin** lifting the
    transitive `Newtonsoft.Json` 11.0.1 floor that carries CVE-2024-21907 (HIGH / NU1903), exactly as
    `DreamTravel.Infrastructure.csproj` does. (Review-verified: the GitHub advisory's first-fixed
    version for CVE-2024-21907 is **13.0.1**, so 13.0.4 clears NU1901–NU1904.)
  - `<ProjectReference Include="..\SolTechnology.Core.CQRS\SolTechnology.Core.CQRS.csproj" />` — for
    `IEvent` / `IEventPublisher` / `IEventDispatcher`.
- `SolTechnology.Core.slnx` — add the project under the `/src/` folder block (alongside the other
  `SolTechnology.Core.*` entries; the `<Folder Name="/src/">` block already exists).
- `nuget-stats.json` — this file is generated download stats, **not** a manual dependency manifest;
  do **not** hand-edit package rows (review-verified: it contains no `Hangfire`/`Newtonsoft` rows and
  is keyed on download counts). Record the dependency-impact note required by CLAUDE.md §1 in the PR
  description / step deviations: "`SolTechnology.Core.Hangfire` introduces `Hangfire.Core` 1.8.22 (new
  to `src/`), mitigated `Newtonsoft.Json` → 13.0.4." Confirm during review whether the repo wants a
  tracked manifest entry; if so, raise it as a follow-up rather than editing the stats file.

## Build settings inherited from `src/Directory.Build.props` (review note)
- **`TreatWarningsAsErrors=true`** is inherited (root `Directory.Build.props` sets `net10.0`,
  `Nullable=enable`, `ImplicitUsings=enable`; `src/Directory.Build.props` adds
  `TreatWarningsAsErrors=true`). Do **not** copy Scheduler's local `TreatWarningsAsErrors=false`
  override unless a Hangfire/analyzer warning forces it — it should not for this project. If any
  `Hangfire.Core` API the plugin uses is `[Obsolete]`, the build will **error**; resolve at source,
  do not blanket-disable.
- `NU1900` and `NU1510` are already demoted to non-errors by `src/Directory.Build.props`; the new
  project (a pure-NuGet consumer like CQRS) may echo `NU1510` — that is expected and non-fatal.
- Do not re-declare `TargetFramework` / `Nullable` — they inherit.

## Details
- The skeleton needs at least one type so the assembly is non-empty and the package is valid. The
  `AddPersistentEvents` **signature stub is NOT created here** (it belongs with its options in step
  04). Add the smallest compiling surface — either a marker type or a `ModuleInstaller` with a no-op
  `internal` helper — and **state which placeholder you used** so step 04 can replace/extend it.
- Run `dotnet restore` / `dotnet build` on the new project and **capture any `NU1901`–`NU1904` /
  `NU1603` / `NU1605` warnings**. If the `Newtonsoft.Json` pin does not silence the CVE warning,
  invoke the [`dependency-audit`](../../../../.github/skills/dependency-audit/SKILL.md) skill before
  proceeding — do not mask the warning.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.Hangfire/SolTechnology.Core.Hangfire.csproj` is green.
- `dotnet build SolTechnology.Core.slnx` is green with the project registered.
- No `NU1901`–`NU1904` warnings (CVE floor lifted by the `Newtonsoft.Json` 13.0.4 pin).
- `dotnet pack` produces a valid `.nupkg` (readme path resolves once step 07 authors `Hangfire.md`;
  a placeholder `Hangfire.md` may be created here if pack must pass — note it for step 07 to replace).
- Dependency-impact note recorded per CLAUDE.md §1.

## Open questions
- Does the repo track a manual dependency manifest beyond `nuget-stats.json`? If yes, add the row
  there; if no, the §1 note in the PR suffices.
- Placeholder type for the skeleton — record which one you used so step 04 can replace/extend it.

