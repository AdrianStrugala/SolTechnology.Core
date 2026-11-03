---
spec: 2026-11-02-core-2-0-api-auth
step: 00
status: to-do
---

# Step 00: Run premortem (gate) — step 01 rename

## Summary
A premortem is mandatory (`CLAUDE.md §4`): step 01 renames and edits
`src/SolTechnology.Core.Authentication/ModuleInstaller.cs` (`AddSolAuthentication` →
`AddSolAuth`) and changes a published NuGet ID. The core premortem `go-with-mitigations
(2026-11-02)` covers steps 02–10 (user skip, 2026-11-02) but predates `C-28` (user, 2026-11-03),
so it does not cover step 01. Run the [premortem skill](../../../../.github/skills/premortem/SKILL.md)
in a session that did not author this plan, after `review` is `done`. Record the full output here
and the verdict in `summary.md` `premortem:`. This step changes no `src/` code. Scope: step 01
only.

## Affected components
- `docs/features/2026-11-02-core-2-0-api-auth/steps/00-run-premortem.md` — EDIT — append a `## Premortem` section (before `## Deviations`) with the full skill output
- `docs/features/2026-11-02-core-2-0-api-auth/summary.md` — EDIT — `premortem:` verdict
- Step files named by required mitigations — EDIT (by the executing `implement-plan` session)

## Changes
- **Modules touched:**
  - `SolTechnology.Core.Authentication` → `SolTechnology.Core.Auth` (project, assembly, namespace, NuGet ID, installer, configuration type).
  - `tests/SolTechnology.Core.Authentication.Tests` → `tests/SolTechnology.Core.Auth.Tests`.
  - `SolTechnology.Core.slnx`; DreamTravel `DreamTravel.Api` (`DreamTravel.Api.csproj`, `Program.cs`).
  - Packaging inputs: `docs/icons/SolTechnology.Core.Auth.png`, `docs/Auth.md` (package README), `dontreadme.md`.
  - Docs and AI tooling: `README.md`, `docs/ClaudeCodingGuide.md`, `.github/skills/premortem/SKILL.md`, `.github/skills/premortem/references/authentication.md`.
- **Contract delta:** see the feature brief
  [`../../2026-11-02-core-2-0-api-auth.md`](../../2026-11-02-core-2-0-api-auth.md) and the core brief
  [`../../2026-11-02-core-2-0-flagship-hardening.md`](../../2026-11-02-core-2-0-flagship-hardening.md):
  - `C-28`: NuGet ID, assembly, and namespace `SolTechnology.Core.Authentication` → `SolTechnology.Core.Auth`; `AddSolAuthentication` → `AddSolAuth`; `AuthenticationConfiguration` → `AuthConfiguration` (`IOptions<AuthConfiguration>`).
  - Old ID on nuget.org: stays at 1.x and is deprecated with the alternative `SolTechnology.Core.Auth` (release 05, manual owner action).
  - Lockstep: `SOLCORE001` must keep firing for a graph that mixes `SolTechnology.Core.Authentication` 1.x with 2.x packages.
  - Semver: MAJOR (2.0.0).
- **`ModuleInstaller.cs` edits:** step 01 (`src/SolTechnology.Core.Auth/ModuleInstaller.cs`: method, parameter, and options type renamed; registrations unchanged).
- **`Directory.Build.props` edits:** none (`src/Directory.Build.props` derives the icon and `buildTransitive` target names from `<PackageId>`).
- **Persisted contracts:** none in storage; the published NuGet ID and the `IOptions<>` type are consumer-facing identities.
- **Applicable checklists** (`.github/skills/premortem/references/`): `authentication.md`, `build-and-nuget.md`, `di.md`.
- **Step under review:** [01](01-rename-authentication-to-auth.md).
- **Covered by the core premortem (not re-run here):**
  [02](02-di-resolution-test.md),
  [03](03-no-upstream-uri-in-problem-details.md),
  [04](04-constant-time-api-key-comparison.md),
  [05](05-api-result-filter-status-preservation.md),
  [06](06-auth-api-public-surface.md),
  [07](07-xml-docs.md),
  [08](08-docs-api-auth.md),
  [09](09-conform-api-auth.md),
  [10](10-log-template-brackets.md).
- **Close:** [11](11-retrospective.md).

## Acceptance criteria
- [ ] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [ ] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Deviations
