---
spec: 2026-11-02-flagship-showcase-hygiene
step: 08
status: to-do
---

# Step 08: Retrospective

## Summary
This step closes the feature with the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md)
retrospective transaction. It verifies the delivered behaviour, brings `docs/architecture/` to the
current state, completes the durable feature brief, and deletes this working folder. It runs only
when steps 01–07 are `done`, or were removed by the plan-reviewer.

## Affected components
- `docs/architecture/package-release.md` — EDIT — actions pinned to commit SHAs with `# vX.Y.Z` comments; Dependabot for `nuget` and `github-actions` (Dependabot PRs still follow `CLAUDE.md §6`); `publish` needs `dreamtravel-tests`
- `docs/architecture/testing.md` — EDIT — DreamTravel unit and component tests run in the GitHub Actions job `dreamtravel-tests`; Google integration tests stay skipped; the Azure DevOps pipelines are kept without CI coverage (AR-4, Q9)
- `docs/features/2026-11-02-flagship-showcase-hygiene.md` — EDIT — `Completion summary`, `Deviations`, `Follow-ups`, `status: completed`, `completed: YYYY-MM-DD`
- `docs/features/2026-11-02-flagship-showcase-hygiene/` — DELETE

## Changes
1. Review every step against the delivered change. Run final validation: `dotnet build SolTechnology.Core.slnx -c Release`, `cd sample-tale-code-apps/DreamTravel && dotnet build`, the DreamTravel unit and component tests, `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows`, and a green `dreamtravel-tests` run on `master`.
2. Update the affected `docs/architecture/*.md` pages above to current behaviour and rationale. Copy the H1 rationale that shipped from the brief only if an architecture page describes DreamTravel events; otherwise the brief keeps it as history.
3. Complete the feature brief with outcomes, material deviations from each step, and follow-ups. The `Completion summary` must record the step 06 clean-clone check (date and platform), because release 01 pre-checks this feature's completion. Candidate follow-ups:
   - Git LFS or release assets for `wroclaw_drive.graphml` and `research_documentation.docx`.
   - CI coverage or retirement of the DreamTravel Azure DevOps pipelines (AR-4, Q9).
   - The outcome of the `CitySearched` persisted-type mitigation (H1, step 05).
4. Set feature status to `completed` (or `abandoned`) with `completed: YYYY-MM-DD` (`CLAUDE.md §2`: confirm with the user first).
5. Verify that no durable link points into this working folder: `grep -rn "2026-11-02-flagship-showcase-hygiene/" --include=*.md . | grep -v "^./docs/features/2026-11-02-flagship-showcase-hygiene/"` prints nothing.
6. Delete the working folder, then search Markdown for the deleted path and repair dangling links.

## Acceptance criteria
- [ ] Every feature-level acceptance criterion in the brief is checked or recorded as a deviation.
- [ ] The documentation-cleanup skill passes for `docs/architecture/` and the feature brief.
- [ ] The working folder no longer exists, and the step 5 grep prints nothing.

## Open questions
- none

## Deviations
