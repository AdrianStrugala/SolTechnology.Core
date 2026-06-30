---
adr: 013-release-1.0
step: 02 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): locked built-in SDK SourceLink with NO PackageReference (answer 8);
     replaced the shared-root-README approach with per-package READMEs (answer 9, an override) — each
     shipped package now ships its own README, sourced from docs/*.md; expanded scope + acceptance to
     reflect the larger authoring workload; moved the SPDX-license confirmation from an open question
     to an implementation task. README files + PackageReadmeFile are kept in ONE step because the prop
     is a guaranteed pack failure without the files it points at (they are cohesive). -->

# Step 02: Centralise NuGet metadata + SourceLink + per-package READMEs (no version change)

## Summary
Add the shared NuGet packaging metadata and built-in SourceLink wiring that every
`src/SolTechnology.Core.*` package should carry, **and** author one `README.md` per shipped package
wired through `PackageReadmeFile`. This makes **no** version change — versions stay at their current
per-project values until the release-gating flip (step 08) — so it cannot trigger a premature `1.0.0`
publish. The metadata/SourceLink props and the per-package READMEs ship together because
`PackageReadmeFile` is a hard `dotnet pack` failure (NU5039) when the README it names is absent — the
prop and the files are inseparable. This step is **larger** than a typical metadata change because of
the ~20 per-package READMEs; the implementer may land the READMEs as a series of commits within the PR.

## Affected components
- `src/Directory.Build.props` — EDIT — add shared package metadata, built-in SourceLink props, and a
  conditional per-project README pack include.
- `src/SolTechnology.Core.*/README.md` — NEW — one per shipped package (enumerated below).
- `src/SolTechnology.Core.*/*.csproj` — EDIT (light) — remove now-redundant per-project metadata
  duplicated by the shared props.
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — record only the
  decision "SourceLink is built-in to the SDK; no explicit package" (no version row, since nothing is
  added).

## Changes
- Add to the existing `<PropertyGroup>` in `src/Directory.Build.props` (already holds `Authors`,
  `RepositoryUrl`, `SymbolPackageFormat=snupkg`):
  - `<Company>`, `<PackageProjectUrl>` (the GitHub repo), `<PackageLicenseExpression>`,
    `<PackageReadmeFile>README.md</PackageReadmeFile>`.
  - SourceLink (**built-in SDK — answer 8, no `PackageReference`**): `<PublishRepositoryUrl>true</…>`,
    `<EmbedUntrackedSources>true</…>`,
    `<ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)'=='true'">true</…>`,
    `<IncludeSymbols>true</…>` (pairs with existing `snupkg`).
- **Per-package README wiring (answer 9 — overrides the earlier shared-root README).** Each package
  ships its **own** `README.md`. Add a single convention to the shared props so each project that has a
  `README.md` packs it automatically (no per-csproj repetition):
  `<None Include="README.md" Pack="true" PackagePath="\" Condition="Exists('$(MSBuildProjectDirectory)/README.md')" />`.
- **Author one README per shipped package**, sourced/adapted from the matching `docs/*.md` (the
  per-module docs are the natural content). Enumerate:
  - Runtime (13): `Core`, `API` (`docs/Api.md`), `HTTP` (the HTTP doc — coordinate with step 09's
    repurposed `docs/Clients.md`), `AUID` (`docs/AUID.md`), `Authentication` (`docs/Auth.md`),
    `BlobStorage` (`docs/Blob.md`), `Cache` (`docs/Cache.md`), `CQRS` (`docs/CQRS.md`), `Hangfire`
    (`docs/Hangfire.md`), `Story` (`docs/Story.md`), `Logging` (`docs/Log.md`), `MessageBus`
    (`docs/Bus.md`), `SQL` (`docs/SQL.md`).
  - Testing (7): `Testing` (`docs/Testing.md`), `BlobStorage.Testing` (`docs/BlobStorage.Testing.md`),
    `HTTP.Testing` (`docs/HTTP.Testing.md`), `Redis.Testing` (`docs/Redis.Testing.md`),
    `ServiceBus.Testing` (`docs/ServiceBus.Testing.md`), plus `API.Testing` and `SQL.Testing` which
    have **no** dedicated doc today — author these fresh (short, sourced from `docs/Testing.md`).
  - `Core` also has no dedicated doc — author a short overview README.
  - These READMEs may still reference renamed symbols; the renames land in steps 03–07, so author them
    with the **final** `AddSol*`/`UseSol*` names (or sweep them in step 11's doc pass if authored first).
- **License.** Confirm the SPDX id (e.g. `MIT`) against the repo `LICENSE` file before setting
  `<PackageLicenseExpression>` — an implementation task, not an open decision.
- Leave `<Version>` untouched in every project (handled in step 08).

## Acceptance criteria
- [ ] `src/Directory.Build.props` defines `Company`, `PackageProjectUrl`, `PackageLicenseExpression`,
      `PackageReadmeFile`, the four SourceLink/CI props, and the conditional README pack include.
- [ ] Every shipped package (13 runtime + 7 `.Testing`) has a `src/<Package>/README.md`, and
      `dotnet pack -c Release` on each embeds that README (no NU5039 "readme not found").
- [ ] `dotnet pack -c Release` on any `src/` package embeds repository metadata (verify the `.nuspec`
      inside the `.nupkg` contains `repository` + `projectUrl` + `license` + `readme`).
- [ ] No `<Version>` value changes in this PR.
- [ ] No explicit `Microsoft.SourceLink.GitHub` `PackageReference` is added (built-in SDK only);
      `canonical-versions.md` records that decision.
- [ ] `dotnet build SolTechnology.Core.slnx` green; no NU5xxx pack warnings.

## Open questions
- none — SourceLink (built-in) and per-package READMEs are resolved at step 00; SPDX id is a confirm
  task against `LICENSE`.

