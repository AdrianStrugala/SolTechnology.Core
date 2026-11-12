---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 04
---

# Step 04: Switch publishing to NuGet Trusted Publishing (no environment)

**Mixed.** The **owner** creates the nuget.org policy and the `NUGET_USER` repository secret. An
**agent** edits `publishPackages.yml` and `docs/CICD.md`, and the owner pushes that change.

## Summary
Q10 (user, 2026-11-02): publishing uses OIDC Trusted Publishing instead of a long-lived
`NUGET_API_KEY`.

**Resync (owner, 2026-11-03 and 2026-11-10).** There is no `nuget-release` GitHub environment and no
required reviewer. Publishing is tag-only, and the `build` job's tag guard
(`publishPackages.yml:64-73`) fails a tag that is not `v` + `<Version>`. The Trusted Publishing
policy is therefore created **without** an environment.

**Current NuGet docs, read 2026-11-10** (<https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing>,
updated 2026-09-01):
- The policy fields are Repository Owner, Repository, Workflow File (file name only) and
  **Environment (optional)**: "Leave this empty if you do not use GitHub Actions environments". The
  fields are case-insensitive.
- A policy is owned by a user or an organization and applies to all packages that owner owns.
- "Policy Scopes" decide whether the policy may publish new packages, new versions of existing
  packages, or both, and a glob limits which package IDs it covers.
- A policy can start "temporarily active for 7 days" until a publish locks it to the GitHub
  repository ID and owner ID. If nothing is published in that window, it becomes inactive and can be
  restarted.
- The temporary API key is valid for 1 hour, and each OIDC token buys one key.
- The `NuGet/login` `user:` input is the nuget.org profile name, not the e-mail address.

RQ2 (user, 2026-11-02): there is no `v2.0.0-rc.1` dry run, because the version comes from
`src/Directory.Build.props` (`2.0.0` since core 04). The first real use of Trusted Publishing is the
`v2.0.0` tag in step 05, so the interim `NUGET_API_KEY` is revoked only after that publish succeeds
(AR-5).

## Affected components
- nuget.org — MANUAL (owner) — create the Trusted Publishing policy **after the transfer** (step 01), naming the **new** repository (RRM-1):
  - policy owner: the organization `SolTechnology`;
  - Repository Owner `sol-technology` (the GitHub login is `Sol-Technology`; case-insensitive);
  - Repository `SolTechnology.Core`;
  - Workflow File `publishPackages.yml`;
  - Environment **empty**;
  - scopes: **new packages and new versions** (the `v2.0.0` publish creates `SolTechnology.Core.Auth`, `C-28`);
  - package glob `SolTechnology.Core*` or none (RRM-5). Never use `SolTechnology.Core.*`: it does not match the base ID `SolTechnology.Core`, which is the first file the push sends, so nothing would be published.

  A policy that names `AdrianStrugala` never matches the token of a run in the organization repository, because the `repository_owner` claim and the owner ID both change with the transfer.
- GitHub repository secret `NUGET_USER` — MANUAL (owner) — the nuget.org profile name (`AdrianStrugala`) of the policy creator, who must stay an active member of `SolTechnology`. It is a repository secret: no environment exists.
- `.github/workflows/publishPackages.yml` — EDIT (agent):
  - `publish` job: `permissions: id-token: write` at job level only. The workflow default stays `contents: read` (`:11-12`).
  - `publish` job: a new `NuGet/login` step right before the push, pinned to `NuGet/login@8d196754b4036150537f80ac539e15c2f1028841 # v1.2.0` (resolved on 2026-11-10 with `git ls-remote https://github.com/NuGet/login 'refs/tags/v1.2.0'`; re-resolve before editing), with `id: login` and `with: user: ${{ secrets.NUGET_USER }}`.
  - `publish` job: the push line becomes `dotnet nuget push ./artifacts/*.nupkg --api-key "${{ steps.login.outputs.NUGET_API_KEY }}" -s https://api.nuget.org/v3/index.json --skip-duplicate`. `--skip-duplicate` and `-s` stay (RRM-6): a re-run after a partial publish must skip what is already on nuget.org.
  - `publish` job: `secrets.NUGET_API_KEY` is removed. `unlistDeprecatedPackages.yml` keeps its own reference (RRM-11).
  - `build` job: a new step, "Guard - every package carries the release version", runs after packing on every trigger (RRM-8). It reads `<Version>` as the tag guard does and fails unless:
    - the number of `./artifacts/*.nupkg` files equals the number of `src/` projects in `dotnet sln SolTechnology.Core.slnx list` (20 on 2026-11-10);
    - every file name ends in `.<Version>.nupkg`;
    - each `.nupkg` has a matching `.snupkg`.

    The tag guard checks only `SolTechnology.Core`'s version; this step catches a project that overrides `<Version>` (Logging did in 1.x) or a package that failed to pack.
  - The slnx guard comment (`:40-41`) still mentions "step 07" and drops the Guards and Scheduler exclusion. That is an open core follow-up; leave it unless the edit touches those lines anyway.
- `docs/CICD.md` — EDIT (agent):
  - Triggers and the `publish` job:
    - the credential is Trusted Publishing: the policy owner `SolTechnology`, repository `sol-technology/SolTechnology.Core`, workflow `publishPackages.yml`, no environment, scopes new packages and new versions, glob `SolTechnology.Core*`;
    - the `NUGET_USER` repository secret;
    - `id-token: write` is granted only to `publish`;
    - renaming `publishPackages.yml` or the repository breaks the policy until it is edited.
  - The tag guard and the new version guard are the automated checks that replace a reviewer (AR-5).
  - The unlist workflow section: it needs `NUGET_API_KEY`. After step 05 revokes the interim key, a run needs a new short-lived nuget.org key with only the "Unlist package" scope and a glob covering only the ghost IDs. Save that key as the secret, run the workflow, then delete the secret and revoke the key (RRM-11).
  - **"Versioning policy" (stale since core 04):** replace it with the 2.0 state:
    - `<Version>2.0.0</Version>` lives once in `src/Directory.Build.props`, and no package overrides it;
    - the Logging 1.2.0 exception ended with `C-01`;
    - lockstep and `SOLCORE001`;
    - Guards and Scheduler stay at `0.5.0` and are not packable;
    - `ApiClient`, `Story`, `BlobStorage`, `Faker`, `Flow` and `Authentication` have no source and stay at their last version;
    - 1.x hotfixes branch from the `v1.0.0` tag;
    - to release, bump `<Version>`, push, and tag `v<Version>`.

    Step 05 repoints the two runbook links (`:8`, and the one in this section) to `release-runbook-2.0.md`.

## Changes
- Order:
  1. Step 01 (transfer) is done.
  2. The owner creates the policy and the `NUGET_USER` secret.
  3. The agent edits; the owner pushes; CI on `master` is green. The `publish` job is skipped, so the login step is not exercised before the tag.
  4. Step 05 tags within the policy's activation window.
- The interim `NUGET_API_KEY` repository secret stays until the step 05 publish succeeds **through the login step**. Step 05 then deletes the secret and revokes the key on nuget.org.
- No dry-run tag and no `rc` pre-release (RQ2).

## Acceptance criteria
- [ ] `actionlint .github/workflows/publishPackages.yml` (or the GitHub workflow editor) reports no error.
- [ ] `grep -n "secrets.NUGET_API_KEY" .github/workflows/publishPackages.yml` prints nothing; `grep -n "NUGET_API_KEY" .github/workflows/unlistDeprecatedPackages.yml` still matches.
- [ ] `grep -nE "NuGet/login@[0-9a-f]{40} # v" .github/workflows/publishPackages.yml` matches, and the step passes `user: ${{ secrets.NUGET_USER }}`.
- [ ] `grep -n -- "--skip-duplicate" .github/workflows/publishPackages.yml` matches the push line.
- [ ] `id-token: write` appears only under the `publish` job, and `grep -n "environment:" .github/workflows/publishPackages.yml` prints nothing.
- [ ] The version guard fails on a throwaway branch where one csproj sets `<Version>2.0.1</Version>`, run with `workflow_dispatch`, and passes on `master`.
- [ ] The nuget.org policy exists:
  - owner `SolTechnology`, repository `sol-technology/SolTechnology.Core`, workflow `publishPackages.yml`, environment empty;
  - new packages and new versions allowed, glob `SolTechnology.Core*` or none;
  - its status is recorded (active, or temporarily active with the date the window ends).

  Owner-confirmed, dated in Deviations.
- [ ] `docs/CICD.md` "Versioning policy" states `<Version>2.0.0</Version>` and no Logging `1.2.0` override, and `grep -n "nuget-release" docs/CICD.md` prints nothing.

## Open questions
- none (Q10 resolved by the user on 2026-11-02; no environment and no reviewer, owner 2026-11-03 and 2026-11-10; RQ2 resolved: no rc dry run)

## Deviations
