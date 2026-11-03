---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 10
status: to-do
---

# Step 10: Retrospective

## Summary
This step closes the foundation feature with the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md)
retrospective transaction. Since the per-library regroup (user, 2026-11-02) this feature is the
umbrella of the 2.0 hardening: it runs only when every other step here is `done` **and** every
library feature listed in the brief is `completed`. It verifies the whole 2.0 tree, brings the
cross-cutting `docs/architecture/` pages to the current state of `master`, completes the umbrella
brief, and deletes this working folder. Library-specific architecture pages are owned by each
library feature's retrospective.

Former core step 71, trimmed on 2026-11-02 to the foundation and cross-cutting part.

## Affected components
- `docs/architecture/package-release.md` — EDIT — version 2.0.0 on `master` for all packages (not yet published; release step 05 of [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md) publishes it); tag-only publishing via `nuget-release`; CI fails on any test-project failure; `buildTransitive` lockstep check `SOLCORE001` (RM-1); zero-dependency `SolTechnology.Core` and per-project `Microsoft.Extensions.*` references (step 07, P-13); Guards/Scheduler kept as deprecated, non-packable projects; `GenerateDocumentationFile` central (step 08). Trusted Publishing, authorship, repository URL, and the nuget.org organization are left to the release feature's retrospective
- `docs/architecture/testing.md` — EDIT — DI resolution tests per module (each library feature's step 01); Blob has no test project (accepted risk AR-1)
- `docs/architecture/naming-and-public-api.md` — EDIT — 2.0 `Async`-suffix outcome; internal implementation types; additive `TaleJsonOptions` and `MaxDeliveryCount` (`C-26`, `C-27`)
- `docs/architecture/logging.md` — EDIT — cross-module log contract after 2.0: `[{Placeholder}]` templates in every module and L-01…L-05 (the page is created by the logging feature's retrospective)
- `docs/features/2026-11-02-core-2-0-flagship-hardening.md` — EDIT — `Completion summary`, `Deviations`, `Follow-ups`, `status: completed`, `completed: YYYY-MM-DD`
- `docs/features/2026-11-02-core-2-0-flagship-hardening/` — DELETE

## Changes
1. Review every step of this feature against delivered code, and confirm that every library feature brief listed in the umbrella brief is `status: completed`. Run final validation:
   - `dotnet build SolTechnology.Core.slnx -c Release`, `pwsh ./.github/runTests.ps1`, the DreamTravel tests, and the `NU190[1-4]|NU1605` grep;
   - the whole-repository checks that the library features ran per module: `find src -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'`, `grep -rnE "catch\s*\{\s*\}" src`, the `[{Placeholder}]` grep over `src` (former step 70), and the former step 61 docs-name grep over `docs/*.md` outside `docs/features/`.
2. Update each affected `docs/architecture/*.md` page above to current behaviour and rationale. The A1–A7, A9, and A10 rationale is copied into the library pages by the library retrospectives; this step copies only what no library owns (A9 and the lockstep rule RM-1). The brief's design-alternatives table stays as the historical record.
3. Complete the umbrella brief with outcomes, material deviations from each step here, and follow-ups. The `Completion summary` must also record, because the release feature reads them after this working folder is deleted:
   - the owner-confirmed dates from step 06 (revocation of both zip keys, the four history Google keys, and the `talecodeservicebus` SAS key) — release step 01 pre-checks them;
   - the owner-confirmed date that environment `nuget-release` exists with required reviewers and the `v*` tag rule (step 03) — release steps 01 and 04 re-check it after the transfer;
   - any deviation of this feature that changes a `C-nn`, `P-nn`, or `L-nn` row or the "Release-notes inputs" section, plus a pointer list to the library briefs whose `Deviations` amend those rows — release step 05 writes the 2.0 release notes and migration guide from them.

   Candidate follow-ups (library-specific follow-ups live in the library briefs):
   - Guards/Scheduler: reference-only, unmaintained; known defects and eventual removal; Guards.Tests out of the slnx and CI, still on xUnit (AR-2, Q1, RQ4).
   - Remove the `System.Security.Cryptography.Pkcs` override from `SolTechnology.Core.SQL` and `SQL.Testing` when `Microsoft.Data.SqlClient` depends on a patched Pkcs (step 01, RQ1).
   - The outdated-package sweep.
4. Set feature status to `completed` (or `abandoned`) with `completed: YYYY-MM-DD` (`CLAUDE.md §2`: confirm with the user first).
5. Verify that no durable link points into this working folder: `grep -rn "2026-11-02-core-2-0-flagship-hardening/" --include=*.md . | grep -v "^./docs/features/2026-11-02-core-2-0-flagship-hardening/"` prints nothing.
6. Delete the working folder, then search Markdown for the deleted path and repair dangling links.

## Acceptance criteria
- [ ] Every feature-level acceptance criterion in the umbrella brief is checked or recorded as a deviation.
- [ ] Every library feature brief listed in the umbrella brief is `status: completed`.
- [ ] The documentation-cleanup skill passes for `docs/architecture/` and the umbrella brief.
- [ ] The working folder no longer exists, and the step 5 grep prints nothing.

## Open questions
- none

## Deviations
