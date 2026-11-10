---
spec: 2026-11-02-flagship-showcase-hygiene
step: 02
status: done
---

# Step 02: Workflow supply-chain hardening

## Summary
Every action is pinned to a moving tag (`@v4`), and the unlist workflow has no explicit
least-privilege block (`repo#A8`). Nothing keeps NuGet packages or actions up to date
(`repo#A6`). The stats workflow has Polish comments (`repo#A15`, `P1-5`). This step pins actions
to commit SHAs, adds Dependabot for `nuget` and `github-actions`, and translates the comments.

## Affected components
- `.github/workflows/publishPackages.yml` — EDIT — `:20,22,87,102,107` (`actions/checkout`, `actions/setup-dotnet` twice, `actions/upload-artifact`, `actions/download-artifact`) pinned to SHAs with a `# vX.Y.Z` comment
- `.github/workflows/unlistDeprecatedPackages.yml` — EDIT — `:23` SHA pin (`actions/setup-dotnet`, its only `uses:`); `permissions: contents: read` (still missing at `c0eb9482`)
- `.github/workflows/update-nuget-stats.yml` — EDIT — `:15` SHA pin; `:23,44,47,57,60` comments in English (`:60` "Pokaż podsumowanie" was missing from the original list)
- `docs/CICD.md` — EDIT — the pipeline page documents every workflow and job; add the SHA-pin rule and Dependabot (resync 2026-11-10: not listed before, and step 08 does not cover this page)
- `.github/dependabot.yml` — NEW — `nuget` (directory `/`, weekly, group `Testcontainers*`, `Microsoft.Extensions.*`, `OpenTelemetry*`, `Aspire*`; `ignore` `version-update:semver-major`; label `dependencies`) and `github-actions` (weekly, one group for every action) — premortem SRM-2, SRM-3

## Changes
- Prerequisite: core steps 02 and 03 are merged ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); step 03 introduces the `publish` job and the `actions/upload-artifact`/`actions/download-artifact` uses pinned here. Done (verified 2026-11-10). Core 03 shipped without the planned `nuget-release` environment (its Deviations): `publish` runs for `refs/tags/v*` only, and `NUGET_API_KEY` stays a repository secret shared with the unlist workflow.
- Dependabot PRs must still pass the `CLAUDE.md §6` rules. The `dependency-audit` skill applies when they raise `NU190x`.
- Premortem SRM-2 (step 00, 2026-11-10, scenario 2: a merged Dependabot PR raises a published dependency floor or takes a major, and consumers pinning lower get NU1605): Dependabot ignores major nuget updates; `Aspire*` is one group because `DreamTravel.Aspire.csproj:2` pins the `Aspire.AppHost.Sdk` version by hand and must move in the same PR; no group mixes packages from the "Library and testing-kit dependencies" item group (`Directory.Packages.props:9-58`) with test-only or DreamTravel packages, so a floor change is always visible as such. The planned `Microsoft.Extensions.*` group therefore needs `exclude-patterns` for its test-only and DreamTravel members (`Microsoft.Extensions.*.Testing`, `Microsoft.Extensions.Logging.Console`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.ServiceDiscovery` at `c0eb9482`); `Testcontainers*` is all library-floor and `OpenTelemetry*` all DreamTravel. The merge rule lives in `CONTRIBUTING.md` (step 01).
- Premortem SRM-3 (scenario 3: `download-artifact` runs only on tags, so a split action bump breaks only the release): one `github-actions` group (`patterns: ["*"]`); `upload-artifact` and `download-artifact` stay on the same major.
- Premortem SRM-4 (scenario 4: false passes and side effects while verifying): resolve each SHA with `git ls-remote https://github.com/<owner>/<action> 'refs/tags/vX.Y.Z^{}'` (the dereferenced commit, not an annotated tag object); test the unlist workflow with an empty `confirm` and check that its job is skipped (never type `UNLIST`); a `workflow_dispatch` of the stats workflow on a branch commits `nuget-stats.json` to that branch, so delete the branch afterwards.
- Optional owner action after merge (admin): Settings → Actions → General → require actions to be pinned to a full-length commit SHA, so a later unpinned `uses:` fails instead of relying on the grep.

## Acceptance criteria
- [x] `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` prints nothing.
- [x] `grep -rnE "[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]|Szukaj|Posortuj|Stwórz|Zapisz|Pokaż" .github/workflows` prints nothing and exits 1 (SRM-4: `-P` is not supported by BSD grep on macOS and would "pass" with an error).
- [ ] A test run of each workflow (`workflow_dispatch` on a branch) succeeds; publish job skipped; the unlist job skipped with an empty `confirm` (SRM-4). *Owner action: the implementing agent may not push or trigger workflows; actionlint passed instead (see Verification).*
- [x] SRM-2, SRM-3: `.github/dependabot.yml` has the major-version ignore for `nuget`, no group that mixes library-floor packages with test-only or DreamTravel packages, and a single `github-actions` group.
- [x] `docs/CICD.md` describes the SHA pins and Dependabot.

## Open questions
- none

## Deviations

### Polish-comment grep needs a UTF-8 locale

**Original plan:** the acceptance grep for Polish text exits 1 on the translated workflows.

**Actual implementation:** it does, but only in a UTF-8 locale (`LC_ALL=en_US.UTF-8`). In the
default C locale of a non-interactive macOS shell, BSD grep reads the bracket expression byte by
byte, and the kept `📊` emoji in the stats summary line shares a byte with `Ó`, so the grep matches
that English line. Run with a UTF-8 locale; the same grep on the previous revision finds 5 lines.

### Unlist ghost-id list in docs/CICD.md

**Original plan:** `docs/CICD.md` gains the SHA-pin rule and Dependabot.

**Actual implementation:** also corrected the unlist section, which named six ghost ids while the
workflow unlists eight (`Faker` and `Flow` were missing), and added the new read-only token
there. The stale "Versioning policy" section (`1.0.0`, Logging `1.2.0`) belongs to the release
feature and is left alone.

### SRM outcomes (2026-11-10)

- **SRM-2 — done.** `nuget` ignores `version-update:semver-major` for every dependency. Groups:
  `testcontainers` (5 packages, all library floor), `microsoft-extensions` (9, all library floor,
  with `exclude-patterns` for `Microsoft.Extensions.*.Testing`, `.Logging.Console`, `.Options`,
  `.ServiceDiscovery`), `opentelemetry` (5, all DreamTravel), `aspire` (2, all DreamTravel). The
  membership was computed against `Directory.Packages.props` with shell-style matching; no group
  spans two item groups. The `Aspire.AppHost.Sdk` hand-move rule is in `docs/CICD.md`.
- **SRM-3 — done.** One `github-actions` group with `patterns: ["*"]`; `upload-artifact` (v4.6.2)
  and `download-artifact` (v4.3.0) are both on major 4.
- **SRM-4 — done for what the agent may run.** The greps use `-E`. Each SHA came from
  `git ls-remote` of `refs/tags/vX.Y.Z` and `refs/tags/vX.Y.Z^{}`: all four tags are lightweight
  (no `^{}` line), so the listed SHA is the commit, and each equals the commit the moving `v4` tag
  pointed to on 2026-11-10 (checkout v4.4.0 `11d5960a`, setup-dotnet v4.3.1 `67a3573c`,
  upload-artifact v4.6.2 `ea165f8d`, download-artifact v4.3.0 `d3f86a10`), so behaviour does not
  change. The `workflow_dispatch` test runs (unlist with an empty `confirm`, never `UNLIST`; the
  stats run on a branch, then delete that branch) are owner actions.

### Verification (2026-11-10)

- `actionlint` 1.7.12 (release binary, checksum verified) over all three workflows: no findings.
  `shellcheck` is not installed, so actionlint's embedded shell checks did not run.
- `.github/dependabot.yml` validates against the SchemaStore `dependabot-2.0.json` schema.
- Pin grep prints nothing; Polish grep exits 1 in a UTF-8 locale.
