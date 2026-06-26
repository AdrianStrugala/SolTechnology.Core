---
adr: 012-production-pattern-adoption-wave-2
step: 23 of 24
status: done
---

# Step 23: Wire the new package into the publish workflow

## Summary
Add a `dotnet pack` step for the **one** new package this wave —
`SolTechnology.Core.Api.Idempotency.Redis` (step 13) — to `.github/workflows/publishPackages.yml`, so
the ADR-012 outcome reaches NuGet. Separate PR because it is **pipeline configuration**, not package
authoring (per the no-bundle-plumbing-with-logic rule, and mirroring ADR-008's dedicated
publish-workflow step). **Depends on step 13** (the package must exist).

> **Scope shrank during implementation (2026-06-25).** The original plan packed **three** new
> packages. Two were dropped:
> - `SolTechnology.Core.DistributedLock` → the lock ships **in `Core.Cache`** (Option B, step 04);
>   `Core.Cache` is already packed by an existing `Pack` step — nothing to add.
> - `SolTechnology.Core.HealthChecks` → health checks ship **per-module** (no foundation package);
>   each module (`Core.Api`/`Core.SQL`/`Core.Cache`/`Core.MessageBus`/`Core.HTTP`) is already packed.
> The only genuinely new package is the idempotency glue.

## Affected components
- `.github/workflows/publishPackages.yml`:
  - **Add** a `Pack SolTechnology.Core.Api.Idempotency.Redis` step:
    `dotnet pack -c Release -o . ./src/SolTechnology.Core.Api.Idempotency.Redis/SolTechnology.Core.Api.Idempotency.Redis.csproj`.
  - Place it alongside the other `Pack …` steps, **before** the final
    `Publish all nuget packages` (`dotnet nuget push *.nupkg …`) step.

## Details
- Mirror the existing per-package step shape exactly (see the `Pack SolTechnology.Core.Scheduler` /
  `Pack SolTechnology.Core.Cache` / `Pack SolTechnology.Core.API` steps already in the file).
- The final `Publish all nuget packages` step pushes every `*.nupkg` in the output directory, so the
  new pack is published automatically once its `Pack` step runs — no change to the push step.
- The health-check changes are additive to already-published packages (`Core.Api`, `Core.SQL`,
  `Core.Cache`, `Core.MessageBus`, `Core.HTTP`) and the lock change is additive to `Core.Cache`; all
  ship via their **version bumps** in the existing pack steps — no new pack step for them.
- No new test wiring: `.\.github\runTests.ps1` is unchanged; the new package's tests run through the
  normal `dotnet build`/`Test` steps already in the workflow.
- This step does **not** touch the `.slnx` (that wiring shipped with step 13) — it only adds the pack
  step.

## Acceptance criteria
- `publishPackages.yml` contains a `Pack` step for `SolTechnology.Core.Api.Idempotency.Redis`, before
  `Publish all nuget packages`.
- The workflow YAML is valid; the `dotnet nuget push *.nupkg` step still runs after all `Pack` steps.
- The new `.nupkg` is produced and pushed by an end-to-end run; the health-check + lock changes ship
  via the existing modules' version bumps.

## Open questions
- none — the pack-step shape is fixed by the existing workflow.

