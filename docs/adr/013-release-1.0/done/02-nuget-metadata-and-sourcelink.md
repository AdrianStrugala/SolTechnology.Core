---
adr: 013-release-1.0
step: 02 of 11
status: done
---

<!-- Reviewed (2026-06-30): locked built-in SDK SourceLink with NO PackageReference (answer 8);
     replaced the shared-root-README approach with per-package READMEs (answer 9, an override) — each
     shipped package now ships its own README, sourced from docs/*.md; expanded scope + acceptance to
     reflect the larger authoring workload; moved the SPDX-license confirmation from an open question
     to an implementation task. README files + PackageReadmeFile are kept in ONE step because the prop
     is a guaranteed pack failure without the files it points at (they are cohesive).
     Amendment (2026-07-01): the maintainer noted every package ALREADY ships a per-package README
     sourced from docs/*.md (wired per-csproj). So answer 9's "author ~20 new src/<Package>/README.md"
     is dropped as content duplication - this step now only CENTRALISES the existing docs/*.md README
     wiring into src/Directory.Build.props (via a per-project PackageReadmeSource property) and fills
     the three missing docs (Core, API.Testing, SQL.Testing) in docs/, not src/. -->

# Step 02: Centralise NuGet metadata + SourceLink + centralise README wiring (docs-sourced, no version change)

## Summary
Add the shared NuGet packaging metadata and built-in SourceLink wiring that every
`src/SolTechnology.Core.*` package should carry, **and centralise the existing per-package README
wiring** so the repeated `PackageReadmeFile` + `<None Include="..\..\docs\*.md">` block (today copied
into ~20 csproj files) lives once in `src/Directory.Build.props`. Each package **already ships its own
README on nuget.org, sourced from the matching `docs/*.md`** — that stays the single source of truth;
this step does **not** author a second copy under `src/`. This makes **no** version change — versions
stay at their current per-project values until the release-gating flip (step 08) — so it cannot trigger
a premature `1.0.0` publish. Only three packages lack a `docs/*.md` today (`Core`, `API.Testing`,
`SQL.Testing`); short docs for those are authored **in `docs/`**, not as `src/` READMEs.

## Affected components
- `src/Directory.Build.props` — EDIT — add shared package metadata, built-in SourceLink props, and
  centralise the README pack wiring (shared `PackageReadmeFile` + `docs/*.md`-sourced `None Include`
  driven by a per-project `PackageReadmeSource` property).
- `src/SolTechnology.Core.*/*.csproj` — EDIT (light) — replace the repeated `PackageReadmeFile` +
  `<None Include="..\..\docs\*.md">` block with a single
  `<PackageReadmeSource>X.md</PackageReadmeSource>` property; remove now-redundant per-project metadata
  duplicated by the shared props.
- `docs/Core.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md` — NEW — the three packages with no
  dedicated doc today (short, sourced from `docs/Testing.md` / a Core overview). **No `src/` READMEs.**
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — record only the
  decision "SourceLink is built-in to the SDK; no explicit package" (no version row, since nothing is
  added).

## Changes
- Add to the existing `<PropertyGroup>` in `src/Directory.Build.props` (already holds `Authors`,
  `RepositoryUrl`, `SymbolPackageFormat=snupkg`):
  - `<Company>`, `<PackageProjectUrl>` (the GitHub repo), `<PackageLicenseExpression>`.
  - The shared, already-used README target: `<PackageReadmeFile>docs\readme.md</PackageReadmeFile>`
    (every csproj sets this identical value today — hoist it once).
  - SourceLink (**built-in SDK — answer 8, no `PackageReference`**): `<PublishRepositoryUrl>true</…>`,
    `<EmbedUntrackedSources>true</…>`,
    `<ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)'=='true'">true</…>`,
    `<IncludeSymbols>true</…>` (pairs with existing `snupkg`).
- **Centralise the existing README wiring — keep `docs/*.md` as the single source (amendment
  2026-07-01, supersedes answer 9's per-`src/` README).** Every package **already** ships its own
  README on nuget.org via the repeated per-csproj block
  `<None Include="..\..\docs\X.md" Pack="true" Visible="false" PackagePath="docs\readme.md" />`
  (+ the shared `mini-logo.png` include). Do **not** create a second copy under
  `src/<Package>/README.md` (that forks the content and breaks single-source-of-truth). Instead hoist
  the wiring into the shared props, driven by a single per-project property:
  ```xml
  <!-- src/Directory.Build.props -->
  <ItemGroup Condition="'$(PackageReadmeSource)' != ''">
    <None Include="$(MSBuildThisFileDirectory)..\docs\$(PackageReadmeSource)" Pack="true" Visible="false" PackagePath="docs\readme.md" />
    <None Include="$(MSBuildThisFileDirectory)..\docs\mini-logo.png"        Pack="true" Visible="false" PackagePath="docs\mini-logo.png" />
  </ItemGroup>
  ```
  Each csproj then declares only `<PackageReadmeSource>CQRS.md</PackageReadmeSource>` (its `docs/*.md`
  file name) and drops the repeated `PackageReadmeFile` + two `None Include` lines.
- **Fill the three doc gaps in `docs/`, not `src/`.** `Core`, `API.Testing`, and `SQL.Testing` have no
  dedicated `docs/*.md` today. Author short docs `docs/Core.md`, `docs/API.Testing.md`,
  `docs/SQL.Testing.md` (sourced from `docs/Testing.md` / a Core overview) and point their
  `PackageReadmeSource` at them. Every other package already has its `docs/*.md`
  (`API`→`Api.md`, `HTTP`→the HTTP doc / step 09's repurposed `Clients.md`, `AUID`→`AUID.md`,
  `Authentication`→`Auth.md`, `BlobStorage`→`Blob.md`, `Cache`→`Cache.md`, `CQRS`→`CQRS.md`,
  `Hangfire`→`Hangfire.md`, `Story`→`Story.md`, `Logging`→`Log.md`, `MessageBus`→`Bus.md`,
  `SQL`→`SQL.md`, and the `.Testing` companions → their matching `*.Testing.md`).
- **Renamed symbols in the docs are swept in step 11.** The `docs/*.md` files may still reference
  pre-rename symbols; the renames land in steps 03–07 and the doc-string sweep is step 11's job — this
  step does not re-author doc prose.
- **License.** Confirm the SPDX id (e.g. `MIT`) against the repo `LICENSE` file before setting
  `<PackageLicenseExpression>` — an implementation task, not an open decision.
- Leave `<Version>` untouched in every project (handled in step 08).

## Acceptance criteria
- [ ] `src/Directory.Build.props` defines `Company`, `PackageProjectUrl`, `PackageLicenseExpression`,
      the shared `PackageReadmeFile` (`docs\readme.md`), the four SourceLink/CI props, and the
      `PackageReadmeSource`-driven README/mini-logo pack `ItemGroup`.
- [ ] No `src/<Package>/README.md` files are created — READMEs remain sourced from `docs/*.md` (single
      source of truth).
- [ ] Every shipped package (13 runtime + 7 `.Testing`) sets `<PackageReadmeSource>` and its csproj no
      longer repeats the `PackageReadmeFile` + `docs/*.md` `None Include` block; `dotnet pack -c Release`
      on each still embeds `docs/readme.md` (no NU5039 "readme not found").
- [ ] `docs/Core.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md` exist and back the three packages
      that had no doc; every other package points at its pre-existing `docs/*.md`.
- [ ] `dotnet pack -c Release` on any `src/` package embeds repository metadata (verify the `.nuspec`
      inside the `.nupkg` contains `repository` + `projectUrl` + `license` + `readme`).
- [ ] No `<Version>` value changes in this PR.
- [ ] No explicit `Microsoft.SourceLink.GitHub` `PackageReference` is added (built-in SDK only);
      `canonical-versions.md` records that decision.
- [ ] `dotnet build SolTechnology.Core.slnx` green; no NU5xxx pack warnings.

## Open questions
- none — SourceLink (built-in) is resolved at step 00; per-package READMEs are **not** newly authored
  (amendment 2026-07-01: reuse the existing `docs/*.md` READMEs, only centralise their wiring); SPDX id
  is a confirm task against `LICENSE`.

## Retrospective — Implementation Deviations
- **`docs/Core.md` was NOT created.** The `Core` umbrella package keeps the richer repo-root
  `README.md` (via its own retained `<ItemGroup>` with `..\..\README.md` → `docs\readme.md`) rather
  than a slimmer `docs/Core.md`. Only the two genuine gaps — `docs/API.Testing.md` and
  `docs/SQL.Testing.md` — were authored. Confirmed with the maintainer.
- **`LICENSE` file added at the repo root** (MIT, © 2026 Adrian Strugala). No license file existed;
  `<PackageLicenseExpression>MIT</PackageLicenseExpression>` in the shared props now matches it.
- **`RepositoryUrl` removed from every non-`Core` csproj** as redundant (hoisted to
  `src/Directory.Build.props`). This is the "remove now-redundant per-project metadata" clause applied
  to `RepositoryUrl`, `PackageIcon`, and `PackageReadmeFile` alike. `Core`'s three redundant props
  were likewise dropped while its README `ItemGroup` was kept.
- **`PackageIcon` was also hoisted** into the shared props (the plan named `PackageReadmeFile`
  explicitly; the icon shared the same identical value across all csproj files, so it was centralised
  in the same conditional `ItemGroup`).
- **Validation:** packed `SQL.Testing` (gap-filled), `API` (runtime), and `Core` (umbrella) — each
  `.nuspec` carries `license`/`icon`/`readme`/`projectUrl`/`repository` (with SourceLink commit), and
  `docs/readme.md` resolves to the correct source (`SQL.Testing.md`, `Api.md`, root `README.md`
  respectively). `dotnet build SolTechnology.Core.slnx -c Release` green, 0 errors, no NU19xx/NU5xxx.


