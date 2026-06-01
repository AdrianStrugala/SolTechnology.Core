---
adr: 008-testing-framework-companions
step: 09 of 11
status: reviewed
---

<!-- Reviewed: NEW step (no to-do/ counterpart). Required because "ship seven NuGet packages"
     is the ADR-008 outcome and the publish workflow currently packs none of the companions
     except (partially) SQL, and still packs Faker. -->

# Step 09: Wire the publish workflow for all seven companion packages

## Summary
Update `.github/workflows/publishPackages.yml` so the CI publish job packs **all seven** `.Testing`
companions and stops packing the superseded `Faker` package. Without this step the ADR-008 outcome
("ship seven NuGet packages") does not actually reach NuGet — the packages would build but never
publish. Separate PR because it is pipeline configuration, not package authoring.

## Affected components
- `.github/workflows/publishPackages.yml`:
  - **Add** `dotnet pack` steps for the four brand-new packages: `SolTechnology.Core.Testing`, `SolTechnology.Core.SQL.Testing`, `SolTechnology.Core.HTTP.Testing`, `SolTechnology.Core.Redis.Testing`, `SolTechnology.Core.BlobStorage.Testing`, `SolTechnology.Core.ServiceBus.Testing`.
  - **Add** a `dotnet pack` step for `SolTechnology.Core.API.Testing` — it ships today (v0.6.0 → 0.7.0) but is **not currently packed** by the workflow; this is the gap that must be closed.
  - **Remove / retarget** the existing `Pack SolTechnology.Core.Faker` step in line with the step-04 decision (thin `[Obsolete]` shim → retarget to pack the shim; outright deletion → remove the step entirely).
  - Leave the final `Publish all nuget packages` (`dotnet nuget push *.nupkg …`) step intact — the new `.nupkg` files land in the same output directory and are pushed automatically.

## Details
- Mirror the existing per-package `dotnet pack -c Release -o . ./src/<Package>/<Package>.csproj` step shape; place the new steps alongside the other `Pack …` steps, before `Publish all nuget packages`.
- The seven companions to pack: `Core.Testing` (0.1.0), `API.Testing` (0.7.0), `SQL.Testing` (0.1.0), `HTTP.Testing` (0.1.0), `Redis.Testing` (0.1.0), `BlobStorage.Testing` (0.1.0), `ServiceBus.Testing` (0.1.0).
- The runtime `SolTechnology.Core.SQL` package is already packed; this step only adds the **`.Testing`** companion packs.
- **No new test projects** are referenced by the workflow — the existing `runTests.ps1` test step is unchanged, so PR builds stay green and Docker-dependent integration tests are never run in CI (consistent with the no-test-project decision across steps 02–08).

## Acceptance criteria
- `publishPackages.yml` contains a `Pack` step for each of the seven companion packages (including `API.Testing`).
- The `Pack SolTechnology.Core.Faker` step is removed or retargeted to the chosen shim, matching step 04.
- The workflow YAML is valid and the `dotnet nuget push *.nupkg` step still runs after all `Pack` steps.

## Open questions
- none — the shim-vs-delete choice is made in step 04 and only mirrored here.

