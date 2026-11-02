---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 02
status: to-do
---

# Step 02: Fail CI on any test-project failure

## Summary
`.github/runTests.ps1` loops `dotnet test --no-build` over every folder in `tests/`. The `pwsh`
step reports only the last `$LASTEXITCODE`, so failures in 12 of the 13 test projects are hidden
(`C1`). The loop also runs `tests/SolTechnology.Core.Guards.Tests`, which is not in
`SolTechnology.Core.slnx`, so its `--no-build` run fails and that failure is hidden too. Guards is
reference-only and no longer maintained (RQ4, user, 2026-11-02), so Guards.Tests stays out of the
solution and out of CI. The runner iterates only the `tests/` projects listed in the slnx,
collects every exit code, and fails at the end.

## Affected components
- `.github/runTests.ps1` — EDIT — iterate slnx test projects; aggregate exit codes

## Changes
- Project list: `dotnet sln SolTechnology.Core.slnx list` filtered to paths starting with `tests/`. This keeps the existing scope; DreamTravel tests run in their own job (showcase step 07 in [`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md)).
- Run `dotnet test --no-build <project>` for each, record each non-zero `$LASTEXITCODE` with the project name, and keep going.
- After the loop: print a summary of failed projects and `exit 1` if any failed, otherwise `exit 0`.
- `SolTechnology.Core.slnx` is unchanged: Guards.Tests is not added (AR-2).
- Keep `excluded="SolTechnology.Core.Scheduler SolTechnology.Core.Guards"` in `publishPackages.yml`.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx && pwsh ./.github/runTests.ps1` exits 0 on a green tree.
- [ ] On a throwaway local branch, a forced `Assert.Fail()` in `tests/SolTechnology.Core.AUID.Tests` (not the last project) makes `runTests.ps1` exit non-zero, and the later projects still run (their results appear in the output). Revert it before committing.
- [ ] The runner output does not mention `SolTechnology.Core.Guards.Tests`.
- [ ] `git diff --stat -- SolTechnology.Core.slnx` prints nothing.

## Open questions
- none (RQ4 resolved by the user on 2026-11-02: Guards.Tests stays out of the slnx and CI)

## Deviations
