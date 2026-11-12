---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 03
---

# Step 03: Org repository hygiene — security settings, presentation, and ghost-ID deprecations (no ruleset)

**Mixed.** The **owner** applies the GitHub and nuget.org settings. An **agent** edits `docs/CICD.md`,
and the owner pushes that change.

## Summary
After the transfer, the organization repository gets the security and presentation settings the
plan relies on (D1, `repo` org checklist). The publishing credential moves to Trusted Publishing in
step 04.

**Re-scope (owner, 2026-11-10).** Earlier this step was "Protect the org repository", and decision A
(user, 2026-11-02) had already removed its ruleset. The owner confirmed on 2026-11-10: no branch
protection and no rulesets on `master`; CI runs the tests and direct pushes stay allowed. Most of the
step still holds, so it is re-scoped, not marked obsolete. What stays is organization-level hygiene:
- secret scanning and push protection, with the AR-6 alert dismissals;
- Private Vulnerability Reporting (Q8; first enabled in step 01 phase A, re-checked here);
- a read-only default `GITHUB_TOKEN`;
- a check that no ruleset or branch rule reaches `master`, from the repository or the organization;
- repository presentation;
- the nuget.org deprecation of the 7 published ghost IDs.

No ruleset, bypass actor, required check or GitHub App is added.

## Affected components
- GitHub repository settings — MANUAL (owner):
  - secret scanning and push protection on;
  - Private Vulnerability Reporting still on after the transfer;
  - Actions → General → Workflow permissions = read.
- GitHub repository and organization rules — MANUAL (owner) — check only. Create no ruleset or branch protection rule, and make sure no **organization** ruleset targets this repository's `master`. An organization ruleset would block the stats bot's direct push (RRM-16).
- `.github/workflows/update-nuget-stats.yml` — unchanged. The `update-stats` job declares `permissions: contents: write` (`:11-12`), which overrides the read-only default for that job.
- GitHub repository settings — MANUAL (owner) — presentation:
  - topics (`dotnet`, `cqrs`, `workflow`, `nuget`, `tale-code`);
  - check the description (set in step 01 phase A);
  - homepage = the product site;
  - social preview `docs/logo.png`.
- GitHub secret scanning alerts — MANUAL (owner) — AR-6 (RRM-17). Dismiss alerts on the local-only DreamTravel credentials as "used in tests":
  - the SQL `sa` password in `appsettings*.json`, including `appsettings.prod.json`, which targets `localhost,1404`;
  - Neo4j at `bolt://localhost`;
  - the Aspire `sql-password` parameter;
  - the database `Dockerfile` `ARG`;
  - the `devOps/` psake script and the `build&test.yml` `docker run`.

  Treat any other alert as a real leak, in particular a Google API key, a Service Bus SAS key or a storage account key. Revoke it first, then record it in Deviations.
- nuget.org — MANUAL (owner) — deprecate every version of each of the 7 published ghost IDs with the reason "Legacy" and the alternate package from `dontreadme.md`:

  | ID (latest on 2026-11-10) | Alternate package | Message points to |
  |---|---|---|
  | `SolTechnology.Core.Scheduler` (0.5.0) | `SolTechnology.Core.Hangfire` | `docs/Hangfire.md` |
  | `SolTechnology.Core.Guards` (0.5.0) | none (BCL guards and FluentValidation) | `dontreadme.md` |
  | `SolTechnology.Core.ApiClient` (0.5.0) | `SolTechnology.Core.HTTP` | `docs/Clients.md` |
  | `SolTechnology.Core.Story` (0.8.0) | `SolTechnology.Core.Tale` | `docs/Tale.md` |
  | `SolTechnology.Core.BlobStorage` (0.5.0) | `SolTechnology.Core.Blob` | `docs/Blob.md` |
  | `SolTechnology.Core.Faker` (1.0.0) | `SolTechnology.Core.HTTP.Testing` | `docs/HTTP.Testing.md` |
  | `SolTechnology.Core.Flow` (1.0.0) | `SolTechnology.Core.Tale` | `docs/Tale.md` |

  Deprecate none of the 20 live IDs. `SolTechnology.Core.BlobStorage.Testing` has never been published (404), so there is nothing to deprecate. `SolTechnology.Core.Authentication` waits for step 05, because its alternate `SolTechnology.Core.Auth` does not exist before the `v2.0.0` publish. Unlisting is separate and optional (step 05, RRM-11).
- `docs/CICD.md` — EDIT (agent) — a short "Repository settings" section:
  - secret scanning and push protection are on, and AR-6 alerts are dismissed as used in tests;
  - Private Vulnerability Reporting is on;
  - the default token is read-only;
  - `master` has no ruleset and no branch protection, CI runs on push and PR, direct pushes are allowed, and `build` and `dreamtravel-tests` are not required checks.

## Changes
- Organization: `sol-technology` (Q3).
- Nothing creates a ruleset, a bypass actor, a GitHub App or a new repository secret (decision A, 2026-11-02; owner, 2026-11-10).

## Acceptance criteria
- [ ] `gh api repos/sol-technology/SolTechnology.Core --jq '.security_and_analysis'` shows secret scanning and push protection `enabled`.
- [ ] Private Vulnerability Reporting is enabled after the transfer (owner-confirmed, dated).
- [ ] `gh api repos/sol-technology/SolTechnology.Core/actions/permissions/workflow --jq '.default_workflow_permissions'` prints `read`.
- [ ] `gh api repos/sol-technology/SolTechnology.Core/rulesets` returns `[]`, and `gh api repos/sol-technology/SolTechnology.Core/rules/branches/master` returns `[]`, which includes organization rulesets.
- [ ] After the settings are applied, a `workflow_dispatch` run of `update-nuget-stats.yml` on `master` pushes its commit to `master`, or reports no change.
- [ ] Every open secret scanning alert is dismissed as "used in tests" with a path from the AR-6 list, or is recorded in Deviations as a real leak with its revocation date.
- [ ] Each of the 7 ghost IDs shows the deprecation with the alternate from the table, and no live ID shows one (owner-confirmed, dated).

## Open questions
- none (Q3, Q8 resolved by the user on 2026-11-02; RQ7 superseded by decision A on 2026-11-02; re-scope confirmed by the owner on 2026-11-10)

## Deviations
