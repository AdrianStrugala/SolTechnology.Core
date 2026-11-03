---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 01
status: to-do
---

# Step 01: Transfer the repository and set up the nuget.org organization (owner runbook)

## Summary
This step is manual: owner actions in GitHub and nuget.org, recorded here as a runbook (D3). It
uses GitHub's built-in transfer, which keeps redirects, and adds a nuget.org organization as
co-owner of every package ID. Q3 (user, 2026-11-02): GitHub organization `sol-technology`,
nuget.org organization `SolTechnology`. It runs after the core and showcase features are completed, so the organization receives
a clean `master`. No repository file changes except this step's Deviations.

## Affected components
- GitHub: `AdrianStrugala/SolTechnology.Core` → `sol-technology/SolTechnology.Core` — MANUAL
- nuget.org: organization `SolTechnology` — MANUAL
- This step file — EDIT — record each action with its date

## Changes
- Pre-checks:
  - The [`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md) brief is `status: completed`, and its Completion summary records the core step 06 (former 09) revocation dates.
  - Every library feature brief is `status: completed`: [`2026-11-02-core-2-0-core-result`](../../2026-11-02-core-2-0-core-result.md), [`2026-11-02-core-2-0-logging`](../../2026-11-02-core-2-0-logging.md), [`2026-11-02-core-2-0-cqrs`](../../2026-11-02-core-2-0-cqrs.md), [`2026-11-02-core-2-0-auid`](../../2026-11-02-core-2-0-auid.md), [`2026-11-02-core-2-0-tale`](../../2026-11-02-core-2-0-tale.md), [`2026-11-02-core-2-0-http`](../../2026-11-02-core-2-0-http.md), [`2026-11-02-core-2-0-api-auth`](../../2026-11-02-core-2-0-api-auth.md), [`2026-11-02-core-2-0-cache`](../../2026-11-02-core-2-0-cache.md), [`2026-11-02-core-2-0-sql`](../../2026-11-02-core-2-0-sql.md), [`2026-11-02-core-2-0-blob`](../../2026-11-02-core-2-0-blob.md), [`2026-11-02-core-2-0-messagebus`](../../2026-11-02-core-2-0-messagebus.md), [`2026-11-02-core-2-0-hangfire`](../../2026-11-02-core-2-0-hangfire.md), [`2026-11-02-core-2-0-testing-kits`](../../2026-11-02-core-2-0-testing-kits.md).
  - The [`2026-11-02-logging-chronicle`](../../2026-11-02-logging-chronicle.md) brief is `status: completed`.
  - The [`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md) brief is `status: completed`.
  - CI on `master` is green.
- GitHub: Settings → Danger Zone → Transfer → `sol-technology`.
  - Never re-create `AdrianStrugala/SolTechnology.Core` afterwards, because that breaks the
    redirects.
- nuget.org: create organization `SolTechnology` and add it as co-owner of the 28 existing IDs:
  - the 19 live IDs that already exist on nuget.org, listed by `dotnet sln SolTechnology.Core.slnx list | grep -E '^src/'`
    minus `SolTechnology.Core.Auth`; that ID is new in 2.0 (`C-28`, user, 2026-11-03), does not exist on nuget.org
    before step 05 (404 on 2026-11-03), and is created under the policy owner `SolTechnology` by the `v2.0.0` publish;
  - `SolTechnology.Core.Authentication` (1.x, renamed to `SolTechnology.Core.Auth` by api-auth 01; step 05 deprecates it);
  - the 8 deprecated IDs from `dontreadme.md` (kept under its name, user, 2026-11-03) and
    `unlistDeprecatedPackages.yml:7-18`. Since api-auth 01, `dontreadme.md` also lists `SolTechnology.Core.Authentication`.

  Keep the personal account as an owner. This decides whether `update-nuget-stats.yml:20`
  `owner:` changes in step 02.
- Verify that environment `nuget-release` and its interim `NUGET_API_KEY` secret (core step 03; date in the core brief's Completion summary) survived
  the transfer. Re-create them if not. Step 04 replaces the secret with Trusted Publishing (Q10).

## Acceptance criteria
- [ ] `git ls-remote https://github.com/sol-technology/SolTechnology.Core` succeeds, and `https://github.com/AdrianStrugala/SolTechnology.Core` redirects.
- [ ] nuget.org shows `SolTechnology` as an owner on every one of the 28 existing IDs (owner-confirmed list in Deviations); `SolTechnology.Core.Auth` is checked in step 05.

## Open questions
- none (Q3 resolved by the user on 2026-11-02: `sol-technology` / `SolTechnology`; Q10 resolved: Trusted Publishing, configured in step 04, so no scoped API key is created here)

## Deviations
