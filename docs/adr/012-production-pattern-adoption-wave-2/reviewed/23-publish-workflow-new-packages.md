---
adr: 012-production-pattern-adoption-wave-2
step: 23 of 24
status: reviewed
---

# Step 23: Wire the new packages into the publish workflow

## Summary
Add `dotnet pack` steps for the **three** new packages — `SolTechnology.Core.DistributedLock`
(steps 04–05), `SolTechnology.Core.HealthChecks` (step 06), and the idempotency glue package
`SolTechnology.Core.Api.Idempotency.Redis` (step 13) — to
`.github/workflows/publishPackages.yml`, so the ADR-012 outcome actually reaches NuGet. Separate PR
because it is **pipeline configuration**, not package authoring (per the no-bundle-plumbing-with-logic
rule, and mirroring ADR-008's dedicated publish-workflow step). **Depends on steps 04, 06, and 13**
(all three packages must exist).

## Affected components
- `.github/workflows/publishPackages.yml`:
  - **Add** a `Pack SolTechnology.Core.DistributedLock` step:
    `dotnet pack -c Release -o . ./src/SolTechnology.Core.DistributedLock/SolTechnology.Core.DistributedLock.csproj`.
  - **Add** a `Pack SolTechnology.Core.HealthChecks` step:
    `dotnet pack -c Release -o . ./src/SolTechnology.Core.HealthChecks/SolTechnology.Core.HealthChecks.csproj`.
  - **Add** a `Pack SolTechnology.Core.Api.Idempotency.Redis` step:
    `dotnet pack -c Release -o . ./src/SolTechnology.Core.Api.Idempotency.Redis/SolTechnology.Core.Api.Idempotency.Redis.csproj`.
  - Place all three alongside the other `Pack …` steps, **before** the final
    `Publish all nuget packages` (`dotnet nuget push *.nupkg …`) step.

## Details
- Mirror the existing per-package step shape exactly (see the `Pack SolTechnology.Core.Scheduler` /
  `Pack SolTechnology.Core.Cache` / `Pack SolTechnology.Core.API` steps already in the file).
- The final `Publish all nuget packages` step pushes every `*.nupkg` in the output directory, so the
  three new packs are published automatically once their `Pack` steps run — no change to the push
  step.
- No new test wiring: `.\.github\runTests.ps1` is unchanged; the new packages' tests run through the
  normal `dotnet build`/`Test` steps already in the workflow.
- This step does **not** touch the `.slnx` (that wiring shipped with steps 04, 06 and 13) — it only
  adds the pack steps.

## Acceptance criteria
- `publishPackages.yml` contains a `Pack` step for `SolTechnology.Core.DistributedLock`,
  `SolTechnology.Core.HealthChecks`, and `SolTechnology.Core.Api.Idempotency.Redis`, all before
  `Publish all nuget packages`.
- The workflow YAML is valid; the `dotnet nuget push *.nupkg` step still runs after all `Pack` steps.
- All three new `.nupkg` files are produced and pushed by an end-to-end run.

## Open questions
- none — the pack-step shape is fixed by the existing workflow.

