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
  - The [`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md) brief is `status: completed`, and its Completion summary records the core step 09 revocation dates.
  - The [`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md) brief is `status: completed`.
  - CI on `master` is green.
- GitHub: Settings → Danger Zone → Transfer → `sol-technology`.
  - Never re-create `AdrianStrugala/SolTechnology.Core` afterwards, because that breaks the
    redirects.
- nuget.org: create organization `SolTechnology` and add it as co-owner of:
  - the 20 live IDs, listed by `dotnet sln SolTechnology.Core.slnx list | grep -E '^src/'`;
  - the 8 deprecated IDs from `docs/deprecated-packages.md` and
    `unlistDeprecatedPackages.yml:7-18`.

  Keep the personal account as an owner. This decides whether `update-nuget-stats.yml:20`
  `owner:` changes in step 02.
- Verify that environment `nuget-release` and its interim `NUGET_API_KEY` secret (core step 03; date in the core brief's Completion summary) survived
  the transfer. Re-create them if not. Step 04 replaces the secret with Trusted Publishing (Q10).

## Acceptance criteria
- [ ] `git ls-remote https://github.com/sol-technology/SolTechnology.Core` succeeds, and `https://github.com/AdrianStrugala/SolTechnology.Core` redirects.
- [ ] nuget.org shows `SolTechnology` as an owner on every one of the 28 IDs (owner-confirmed list in Deviations).

## Open questions
- none (Q3 resolved by the user on 2026-11-02: `sol-technology` / `SolTechnology`; Q10 resolved: Trusted Publishing, configured in step 04, so no scoped API key is created here)

## Deviations
