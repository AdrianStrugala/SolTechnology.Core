---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 01
---

# Step 01: Push master, pass CI on GitHub, transfer the repository, and set up the nuget.org organization (owner runbook)

**Owner-only.** Every action is in the GitHub or nuget.org admin UI, or is a push to `master` (`CLAUDE.md §2`).
An agent only records the owner's answers and dates in this file's Deviations.

## Summary
This step has two phases, in this order.

- **Phase A, the CI gate (resync 2026-11-10).** On 2026-11-10 `master` was 125 commits ahead of
  `origin/master`, so most of 2.0 had never run in GitHub CI: not `runTests.ps1` on the Linux runner with
  Ryuk on (P-28), not the first `dreamtravel-tests` run, and not the first Dependabot run. The owner
  pushes `master`, and the build, the tests and `dreamtravel-tests` must be green on GitHub before the
  transfer. The showcase owner actions that need a pushed `master` happen in this phase too.
- **Phase B, the transfer.** GitHub's built-in transfer keeps redirects. The nuget.org organization
  becomes co-owner of every existing package ID. Q3 (user, 2026-11-02): GitHub organization
  `sol-technology` (the API spells the login `Sol-Technology`; GitHub and nuget.org compare these names
  case-insensitively), and nuget.org organization `SolTechnology`.

The gate is phase A of this step, not a new step, for two reasons. The step already had the pre-check "CI on
`master` is green", which cannot pass without a push. The closed core and showcase briefs also cite this feature's
steps by number ("release 01 re-checks them before the transfer", "release 03", "release 05"), and renumbering would
break those references. Step 05 checks CI again on the exact commit that gets the tag.

## Affected components
- GitHub `AdrianStrugala/SolTechnology.Core`: MANUAL. Settings, the push, workflow runs, and the transfer to `sol-technology/SolTechnology.Core`.
- nuget.org: MANUAL. The organization `SolTechnology` and its co-ownership of 27 existing IDs.
- This step file: EDIT. Record each action with its date.

## Changes

### Phase A — push and CI gate (before the transfer)

1. Pre-checks (all true on 2026-11-10):
   - These briefs are `status: completed`:
     - [`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md), whose
       Completion summary records the core 06 revocation (2026-11-10);
     - the 13 library briefs it lists under "Per-library regroup";
     - [`2026-11-02-logging-chronicle`](../../2026-11-02-logging-chronicle.md);
     - [`2026-11-05-cache-redis-fail-fast`](../../2026-11-05-cache-redis-fail-fast.md);
     - [`2026-11-10-chronicle-sql-step-targets`](../../2026-11-10-chronicle-sql-step-targets.md);
     - [`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md).
   - `2026-11-04-cache-redis-reconnect` is `abandoned`.
2. Before the push (showcase "Owner manual actions" 1–2, SRM-1):
   - enable Private Vulnerability Reporting (Settings → Code security);
   - set the repository description;
   - turn on content reports (Settings → Moderation options → Reported content);
   - confirm that Insights → Community shows every item checked.
3. Push `master` (RRM-12):
   - `git fetch origin` first. The weekly stats bot commits to `origin/master` on Sundays at 00:00 UTC
     (`update-nuget-stats.yml:5`).
   - If `origin/master` moved, integrate it with `git pull --no-rebase` (a merge commit). Never rebase:
     71 of the unpushed commits are cited by SHA in `docs/features/*.md` (count on 2026-11-10), and a
     rebase would orphan them.
   - If push protection blocks the push, read the flagged path before doing anything else. The 125
     unpushed commits were scanned on 2026-11-10 for Google, Service Bus SAS, storage account and
     GitHub token patterns, and only placeholders such as `SharedAccessKey=v` matched. A real hit is a
     new leak to revoke, not a bypass.
4. CI gate on the pushed `master` (GitHub's Linux runner):
   - the `Build, test and publish` run is green: `build` (including `.github/runTests.ps1` with Ryuk on,
     because nothing sets `TESTCONTAINERS_RYUK_DISABLED`) and `dreamtravel-tests`;
   - `publish` is skipped (not a tag).
   - A failure that shows only on Linux is fixed in tests or fixtures and pushed again. Never weaken the
     job (no `continue-on-error`, no `if:`).
5. After the green run (showcase "Owner manual actions" 3–6), run each workflow once with `workflow_dispatch`:
   - the package workflow (publish skipped);
   - `Unlist deprecated packages` with an **empty** `confirm`: the job is skipped. Never type `UNLIST`
     here.
   - `Update NuGet Statistics` on a throwaway branch, then delete the branch;
   - the forced `dreamtravel-tests` failure on a throwaway branch, then delete the branch.
6. First Dependabot run (showcase "Owner manual actions" 4):
   - check Insights → Dependency graph → Dependabot;
   - turn on Dependabot security updates;
   - create the `dependencies` label if Dependabot asks for it.
   - **Freeze (RRM-13):** merge no Dependabot PR from this gate until the `v2.0.0` publish succeeds
     (step 05). A floor PR would move the 2.0.0 dependency floors. An actions-group PR changes
     `download-artifact`, which runs only on tags. A security update that cannot wait is merged
     deliberately, and then this gate runs again.

### Phase B — transfer and nuget.org organization

7. GitHub: Settings → Danger Zone → Transfer → `sol-technology`.
   - Never create or fork a repository named `SolTechnology.Core` under `AdrianStrugala` afterwards.
     That includes forking the organization repository back to the personal account. GitHub would stop
     redirecting, and every 1.x nuspec (`projectUrl`, `repository`), old clone and old link points at
     that name (RRM-14).
   - Point local clones at the new URL with
     `git remote set-url origin https://github.com/sol-technology/SolTechnology.Core.git`.
   - GitHub keeps repository secrets through a transfer. Check that the repository secret
     `NUGET_API_KEY` is still listed (Settings → Secrets and variables → Actions). There is no
     `nuget-release` environment: the owner declined it on 2026-11-03 (core 03 deviation).
   - Organization Actions policy (Organization settings → Actions → General):
     - allow all actions, or allow `NuGet/login@*` next to the `actions/*` ones, so step 04's login
       step can run (RRM-16);
     - keep the organization's default workflow permissions at read; the workflows declare what
       they need.
8. nuget.org: create the organization `SolTechnology` (its profile returned 404 on 2026-11-10, so the
   name was free). Add it as co-owner of the 27 IDs that exist on nuget.org, and accept each ownership
   request. Keep the personal account `AdrianStrugala` as an owner, because the stats bot queries
   `owner:AdrianStrugala` (`update-nuget-stats.yml:20`).
   - 19 live IDs: `dotnet sln SolTechnology.Core.slnx list | grep -E '^src/'` lists the 20 packable
     projects. Leave out `SolTechnology.Core.Auth`, which is new in 2.0 (`C-28`), returned 404 on
     2026-11-10, and is created by the `v2.0.0` publish under the policy owner `SolTechnology`. The 19
     IDs are:
     `SolTechnology.Core`, `.Api`, `.API.Testing`, `.AUID`, `.Blob`, `.Blob.Testing`, `.Cache`,
     `.CQRS`, `.Hangfire`, `.HTTP`, `.HTTP.Testing`, `.Logging`, `.MessageBus`, `.Redis.Testing`,
     `.ServiceBus.Testing`, `.SQL`, `.SQL.Testing`, `.Tale`, `.Testing`.
   - `SolTechnology.Core.Authentication` (1.x; renamed to `.Auth`, step 05 deprecates it).
   - 7 ghost IDs, hardcoded in `unlistDeprecatedPackages.yml:36-39` and listed in `dontreadme.md`:
     `.ApiClient`, `.Story`, `.Scheduler`, `.Guards`, `.BlobStorage`, `.Faker`, `.Flow`.
     `SolTechnology.Core.BlobStorage.Testing`, the eighth ID in the unlist workflow, has never been
     published (flat-container and gallery 404 on 2026-11-10). Nothing to co-own or deprecate; the
     workflow skips it.
   - Do not reserve an ID prefix now. If `SolTechnology.*` is ever reserved, reserve it for the
     organization `SolTechnology`. A prefix that only the personal account owns would block new IDs
     that the organization's Trusted Publishing policy pushes.
   - The `update-nuget-stats.yml:20` `owner:` stays `AdrianStrugala` (step 02).

## Acceptance criteria
- [ ] Phase A: the run for the pushed `master` SHA is green on GitHub (`build`, `dreamtravel-tests`; `publish` skipped); its URL and date are in Deviations.
- [ ] Phase A: `git rev-list --count origin/master..master` prints `0`, and no commit was rebased (`git merge-base --is-ancestor c9868425 origin/master` succeeds).
- [ ] Phase A: Private Vulnerability Reporting, the description and content reports are set, and the three `workflow_dispatch` runs and the first Dependabot run were checked (owner-confirmed, dated).
- [ ] `git ls-remote https://github.com/sol-technology/SolTechnology.Core` succeeds, and `https://github.com/AdrianStrugala/SolTechnology.Core` redirects.
- [ ] `curl -s "https://azuresearch-usnc.nuget.org/query?q=owner:SolTechnology&take=100&prerelease=true" | jq '[.data[].id] | length'` prints `27`. Unlisted ghost versions do not appear in search, so check any missing ID on its gallery page; `SolTechnology.Core.Auth` is checked in step 05.
- [ ] The organization Actions policy allows `NuGet/login` (owner-confirmed, dated).

## Open questions
- none (Q3 resolved by the user on 2026-11-02; Q10 resolved: Trusted Publishing, configured in step 04 without an environment, owner 2026-11-03 and 2026-11-10)

## Deviations
