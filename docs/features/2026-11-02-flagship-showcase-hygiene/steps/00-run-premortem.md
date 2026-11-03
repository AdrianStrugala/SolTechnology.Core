---
spec: 2026-11-02-flagship-showcase-hygiene
step: 00
status: to-do
---

# Step 00: Run premortem (gate)

## Summary
A premortem is mandatory (`CLAUDE.md §4`): step 05 edits a `ModuleInstaller.cs` and moves a type
that DreamTravel persists as a Hangfire job argument. Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author
this plan, after `review` is `done` and after the core steps this feature depends on are `done`
(see the brief), so the evidence is current. Record the full output here and the verdict in
`summary.md` `premortem:`. This step changes no `src/` code.

## Affected components
- `docs/features/2026-11-02-flagship-showcase-hygiene/steps/00-run-premortem.md` — EDIT — append a `## Premortem` section (before `## Deviations`) with the full skill output
- `docs/features/2026-11-02-flagship-showcase-hygiene/summary.md` — EDIT — `premortem:` verdict
- Step files named by required mitigations — EDIT (by the executing `implement-plan` session)

## Changes

- **Modules touched:**
  - Root community files: `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CHANGELOG.md`, `.github/ISSUE_TEMPLATE/*`, `.github/PULL_REQUEST_TEMPLATE.md`, `.github/CODEOWNERS`, `dontreadme.md` Guards row (step 01; the file keeps its name, user, 2026-11-03).
  - CI: `.github/workflows/publishPackages.yml` (steps 02, 07), `unlistDeprecatedPackages.yml` and `update-nuget-stats.yml` (step 02), `.github/dependabot.yml` (step 02).
  - DreamTravel: `DreamTravel.Api` controllers and `Program.cs`, `DreamTravel.Queries`, `DreamTravel.Domain`, `DreamTravel.Worker`, unit and component tests, `README.md`, `initWorkspace.bat`, `docker-compose.yml` (steps 03, 05, 06).
  - Guidance: `docs/ClaudeCodingGuide.md`, `CLAUDE.md` (step 04).
  - `SolTechnology.Core.slnx` `/Docs/` entries (step 01).
- **Contract delta:** see the feature brief
  [`../../2026-11-02-flagship-showcase-hygiene.md`](../../2026-11-02-flagship-showcase-hygiene.md):
  - No public/protected symbol in `src/SolTechnology.Core.*` changes; no `C-nn` applies.
  - No package version or `PackageReference` changes in `src/`.
  - The `publish` job gains `needs: [build, dreamtravel-tests]` (step 07): a failing DreamTravel test blocks a tag publish.
  - Semver: none for the packages.
- **`ModuleInstaller.cs` edits:**
  - `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/ModuleInstaller.cs`: step 05 (BOM removal; the `LimitCostOfPathsService` → `LimitCostOfPathsHandler` rename is picked up by `RegisterQueriesFromAssembly`).
- **`Directory.Build.props` edits:** none.
- **Persisted contracts:**
  - Step 05: `CitySearched` moves from `DreamTravel.Domain.Events` to `DreamTravel.Queries.Events`. DreamTravel registers `AddSolPersistentEvents()` (`DreamTravel.Api/Program.cs:125`, `DreamTravel.Worker/Program.cs:55`), so the event is stored as a Hangfire job argument with its type name (design choice H1 in the brief).
- **Applicable checklists** (`.github/skills/premortem/references/`): `di.md`, `cqrs.md`, `build-and-nuget.md`, and the Hangfire checklist that core step 00 adds. If that file is still missing, the premortem writes it in the same change.
- **Steps under review:**
  - [01](01-community-health-files-and-root-readme.md),
    [02](02-workflow-supply-chain-hardening.md),
    [03](03-dreamtravel-mediator-controllers.md),
    [04](04-coding-guide-self-consistency.md),
    [05](05-dreamtravel-logic-and-domain-cleanup.md),
    [06](06-dreamtravel-run-instructions.md),
    [07](07-dreamtravel-tests-in-github-ci.md).
  - Close: [08](08-retrospective.md).

## Acceptance criteria
- [ ] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [ ] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Deviations
