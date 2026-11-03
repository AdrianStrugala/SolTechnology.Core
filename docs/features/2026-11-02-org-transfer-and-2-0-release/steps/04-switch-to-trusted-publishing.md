---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 04
status: to-do
---

# Step 04: Switch publishing to NuGet Trusted Publishing

## Summary
Q10 (user, 2026-11-02): publishing uses OIDC Trusted Publishing instead of a long-lived
`NUGET_API_KEY`. This step configures the nuget.org policy and changes the `publish` job (split
from the former single protection step during plan review, RQ11). RQ2 (user, 2026-11-02): there
is no `v2.0.0-rc.1` dry run, because the package version comes from `src/Directory.Build.props`
(already `2.0.0` since core step 04, [`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)), so any tag run would publish the final 2.0.0 packages. The first
real use of Trusted Publishing is the `v2.0.0` tag in step 05, which is why the interim
`NUGET_API_KEY` is only revoked after that publish succeeds (accepted risk AR-5).

## Affected components
- nuget.org — MANUAL — Trusted Publishing policy (owner `SolTechnology`) for `sol-technology/SolTechnology.Core`, workflow `publishPackages.yml`, environment `nuget-release`
- GitHub environment `nuget-release` — MANUAL — secret `NUGET_USER` = the nuget.org profile name (not the e-mail) of the account that owns the policy; required reviewers and tag rule `v*` from core step 03 re-checked after the transfer
- `.github/workflows/publishPackages.yml` — EDIT — `publish` job:
  - `permissions: id-token: write` (job level only; the workflow default stays `contents: read`);
  - NEW `NuGet/login` step, SHA-pinned per showcase step 02 ([`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md)) with a `# v1.x.y` comment, `id: login`, `with: user: ${{ secrets.NUGET_USER }}`;
  - `dotnet nuget push ... --api-key "${{ steps.login.outputs.NUGET_API_KEY }}"`;
  - `secrets.NUGET_API_KEY` removed.
- `docs/CICD.md` — EDIT — publishing credential (Trusted Publishing), the `NUGET_USER` secret, and the reviewer's tag-equals-`<Version>` check (AR-5)

## Changes
- Order:
  1. Create the nuget.org policy and the `NUGET_USER` secret.
  2. Merge the workflow change.
  3. Keep the interim `NUGET_API_KEY` secret in `nuget-release` until the step 05 publish succeeds; step 05 then deletes it and revokes the key on nuget.org.
- No dry-run tag, no `rc` pre-release (RQ2).
- The policy must allow pushing a new package ID: the `v2.0.0` publish creates `SolTechnology.Core.Auth` for the first time (`C-28`, user, 2026-11-03). If nuget.org offers policy scopes or a package glob, set the glob to `SolTechnology.Core.*` and allow both new packages and new versions.

## Acceptance criteria
- [ ] `actionlint .github/workflows/publishPackages.yml` (or the GitHub workflow editor) reports no error.
- [ ] `grep -n "secrets.NUGET_API_KEY" .github/workflows/publishPackages.yml` prints nothing.
- [ ] `grep -nE "NuGet/login@[0-9a-f]{40}" .github/workflows/publishPackages.yml` matches, and the step passes `user:`.
- [ ] `id-token: write` appears only under the `publish` job.
- [ ] The nuget.org policy exists for owner `SolTechnology`, repository `sol-technology/SolTechnology.Core`, workflow `publishPackages.yml`, environment `nuget-release` (owner-confirmed, dated in Deviations).
- [ ] The policy permits pushing the new ID `SolTechnology.Core.Auth` (owner-confirmed, dated in Deviations).

## Open questions
- none (Q10 resolved by the user on 2026-11-02; RQ2 resolved: no rc dry run)

## Deviations
