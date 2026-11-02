---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 03
status: in-progress
---

# Step 03: Publish to nuget.org only from `v*` tags behind a protected environment

## Summary
`publishPackages.yml:73` publishes when `workflow_dispatch` runs on any branch, and nuget.org
versions cannot be reused (`C3`, `repo#A4`). The workflow also has no `permissions:` block
(`repo#A8`). This step restricts publishing to `refs/tags/v*`, moves the push into a separate job
bound to a GitHub environment with required reviewers, and sets least-privilege token
permissions. From this point on, only an approved `v*` tag run can reach nuget.org. The package
version still comes from `src/Directory.Build.props`, not from the tag; the environment reviewer
is the control for that (accepted risk AR-5).

## Affected components
- `.github/workflows/publishPackages.yml` — EDIT — trigger, permissions, publish job
- GitHub repository settings — MANUAL (owner, **before** this step's PR merges) — create environment `nuget-release` with required reviewers and move `NUGET_API_KEY` into it
- `docs/CICD.md` — EDIT — describe the tag-only publish and the environment

## Changes
- Top-level `permissions: contents: read`.
- Keep `workflow_dispatch` for build, test, and pack only. Delete the `github.event_name == 'workflow_dispatch' ||` clause from the publish condition.
- Upload `./artifacts/*.nupkg` and `*.snupkg` with `actions/upload-artifact` in the `build` job.
- NEW job `publish`: `needs: build`, `if: startsWith(github.ref, 'refs/tags/v')`, `environment: nuget-release`. It downloads the artifacts and runs `dotnet nuget push ... --skip-duplicate`.
- Owner action, completed **before** the workflow change merges (otherwise the first tag run auto-creates an unprotected `nuget-release` environment): GitHub → Settings → Environments → `nuget-release` → required reviewer = repository owner, deployment tag rule `v*`; move the `NUGET_API_KEY` secret into the environment and delete the repository-level secret. Record the date under Deviations.
- `docs/CICD.md` states that the reviewer must check that the tag matches `<Version>` in `src/Directory.Build.props` before approving (AR-5).
- Premortem RM-18 (step 00, 2026-11-02): the uploaded artifact carries both `*.nupkg` and `*.snupkg`, and the `publish` job pushes from one folder that holds both, so the 2.0.0 symbol packages reach nuget.org.

## Acceptance criteria
- [x] `actionlint .github/workflows/publishPackages.yml` (or the GitHub workflow editor) reports no error.
- [ ] A `workflow_dispatch` run on a non-tag ref finishes with the `publish` job skipped.
- [ ] The owner-confirmed date that `nuget-release` exists with ≥1 required reviewer and the `v*` tag rule precedes the merge date of this step's PR.
- [x] The workflow YAML shows the `publish` job referencing `environment: nuget-release`.
- [ ] `gh secret list` (repository level) no longer lists `NUGET_API_KEY`.
- [x] `docs/CICD.md` states "publishing only from `v*` tags through `nuget-release`" and the tag-equals-`<Version>` reviewer check.
- [ ] RM-18: the workflow YAML uploads `./artifacts/*.snupkg` next to `*.nupkg`, and a `workflow_dispatch` run's artifact lists one `.snupkg` per `.nupkg`.

## Open questions
- none

## Deviations

### Awaiting owner actions

**Original plan:** the owner creates `nuget-release` and moves `NUGET_API_KEY` before the workflow change merges.

**Actual implementation:** the workflow and docs change is committed on branch `foundation/03-tag-only-publish` (not on `master`) and verified locally: actionlint 1.7.12 clean; a local Release pack of every slnx `src/` project produced 20 `.nupkg` and 20 matching `.snupkg`. The step stays `in-progress` until the owner creates the environment, moves the secret, merges the branch, and a `workflow_dispatch` run shows the `publish` job skipped.
