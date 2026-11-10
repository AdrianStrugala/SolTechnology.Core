# CI/CD

How `SolTechnology.Core` is built, tested, packed, and published to nuget.org. The primary automation
entry point is [`.github/workflows/publishPackages.yml`](../.github/workflows/publishPackages.yml).
Unlisting the deprecated ghost ids lives in a separate, manual-only workflow,
[`.github/workflows/unlistDeprecatedPackages.yml`](../.github/workflows/unlistDeprecatedPackages.yml),
so a normal release publish can never reach it. The manual release steps that CI cannot perform are
captured in the [release runbook](release-runbook-1.0.md).

---

## Pipeline — `Build, test and publish`

### Triggers

| Trigger | Build + test | Pack | DreamTravel tests | Publish |
|---|:---:|:---:|:---:|:---:|
| `pull_request` → `master` | ✅ | ✅ | ✅ | — |
| `push` → `master` | ✅ | ✅ | ✅ | — |
| `push` tag `v*` | ✅ | ✅ | ✅ | ✅ |
| `workflow_dispatch` | ✅ | ✅ | ✅ | — |

Publishing only from `v*` tags: the `publish` job runs only for a `refs/tags/v*` push, and the
build fails first if the tag does not match the package version. `workflow_dispatch` builds, tests
and packs but never publishes. `NUGET_API_KEY` is a repository secret, shared with the unlist
workflow. Unlisting the deprecated ghost ids is **not** part of
this pipeline — it lives in its own manual-only workflow (see below), so it can never run on a tag
or `master` push. The workflow token is read-only (`permissions: contents: read`).

### `build` job

1. Checkout + set up .NET 10 (`10.0.x`).
2. `dotnet workload restore SolTechnology.Core.slnx` — required before restore/build on a clean CI box.
3. Restore + `dotnet build SolTechnology.Core.slnx --no-restore`.
4. Test via `./.github/runTests.ps1` — runs every `tests/` project listed in the slnx and fails the
   job if any of them fails. The script resolves the slnx from its own location, so it works from
   any working directory, and it fails when `dotnet sln list` fails or finds no test project.
5. **slnx-membership guard** — every packable `src/*` project must be listed in
   `SolTechnology.Core.slnx`, otherwise the pack glob would silently skip it. Projects with
   `<IsPackable>false>` and the two deprecated ids kept out of the slnx (`Scheduler`, `Guards`) are
   excluded from the check.
6. **Pack** — enumerates `src/*.csproj` from `dotnet sln … list` and `dotnet pack -c Release` each into
   `./artifacts`.
7. **Tag guard** (tags only) — fails the build when the tag is not `v` + `<Version>` from
   [`src/Directory.Build.props`](../src/Directory.Build.props). The package version comes from the
   props file, not from the tag, and nuget.org versions can never be reused.
8. **Upload** — `*.nupkg` and `*.snupkg` are uploaded as the `nuget-packages` artifact.

### `dreamtravel-tests` job

Runs after `build` on every trigger and fails the workflow when a DreamTravel test fails. The
`build` job's test script walks only the `tests/` projects, so this job is where the reference
app's tests run on GitHub. It checks out the code again (the `build` artifact holds only the
packages), restores the workloads, and runs `dotnet test` on each DreamTravel test project: the
Commands, Queries, TravelingSalesmanProblem, and Ui unit tests, the GeolocationDataClients tests
(their paid Google and Michelin tests are `[Ignore]`d), and `DreamTravel.Component.Tests`, which
starts SQL Server through Testcontainers on the runner's Docker and fakes Google with WireMock.
`DreamTravel.E2E.Tests` is a placeholder and stays out. Every project runs even after a failure;
the job lists the failed ones at the end.

The job has `timeout-minutes: 30`, no `continue-on-error`, and no `if:`. It is not a required
status check, because `master` has no branch protection, but `publish` needs it, so a red or
skipped `dreamtravel-tests` also stops the tag publish. A failure that only shows on the Linux
runner is fixed in the tests or their fixtures, never by weakening the job.

### `publish` job

Runs after `build` and `dreamtravel-tests`, only for `v*` tags. It downloads the artifact and runs
`dotnet nuget push ./artifacts/*.nupkg --skip-duplicate`, which also pushes the matching `.snupkg`
symbol packages. To release, bump `<Version>` in `src/Directory.Build.props`, push it to `master`,
then push the tag `v<Version>`.

If `dreamtravel-tests` fails on a release tag, nothing has reached nuget.org yet: re-run the failed
job from the workflow run page (Re-run failed jobs) before re-tagging. A passing re-run lets
`publish` continue with the same tag.

## Unlisting deprecated packages — `Unlist deprecated packages`

A dedicated workflow, [`unlistDeprecatedPackages.yml`](../.github/workflows/unlistDeprecatedPackages.yml),
with **only** a `workflow_dispatch` trigger — no `push`, tag, or `pull_request` hook, so a release
publish can never reach it. Running it requires typing `UNLIST` into the `confirm` input
(`if: inputs.confirm == 'UNLIST'`); any other value skips the job. The job enumerates every
published version of the eight ghost ids — `ApiClient`, `Story`, `Scheduler`, `Guards`,
`BlobStorage`, `BlobStorage.Testing`, `Faker`, `Flow` — live from the flat-container index and
unlists each with `dotnet nuget delete`. The ids are **hardcoded** in the job, so even a
full-account key cannot unlist a supported package. The workflow token is read-only
(`permissions: contents: read`).

## Supply chain — pinned actions and Dependabot

- Every `uses:` in `.github/workflows/` is pinned to a full commit SHA with the release it came from
  as a comment (`actions/checkout@<sha> # v4.4.0`). A moving tag such as `@v4` is not allowed;
  `grep -rnE "uses: [^@]+@v[0-9]" .github/workflows` must print nothing. Resolve a SHA with
  `git ls-remote https://github.com/<owner>/<action> 'refs/tags/vX.Y.Z^{}'` (the commit, not an
  annotated tag object; for a lightweight tag the plain ref is already the commit).
- [`.github/dependabot.yml`](../.github/dependabot.yml) checks `nuget` and `github-actions` weekly
  and labels its PRs `dependencies`.
  - `nuget` ignores major updates; a major goes through the
    [package-management skill](../.github/skills/package-management/SKILL.md) by hand.
  - Its groups never mix the published dependency floors ("Library and testing-kit dependencies" in
    `Directory.Packages.props`) with test-only or DreamTravel packages: `Testcontainers*` and
    `Microsoft.Extensions.*` (minus the test-only and DreamTravel members) are floors,
    `OpenTelemetry*` and `Aspire*` are DreamTravel. Every other package gets its own PR.
  - A floor change needs the `canonical-versions.md` and `CHANGELOG.md` updates from
    [CONTRIBUTING.md](../CONTRIBUTING.md#dependency-floors), pushed to the Dependabot branch before
    merging.
  - An `Aspire*` PR also moves the `Aspire.AppHost.Sdk` version in
    `sample-tale-code-apps/DreamTravel/src/DreamTravel.Aspire/DreamTravel.Aspire.csproj` if
    Dependabot left it behind.
  - `github-actions` uses one group for every action, so `upload-artifact` and `download-artifact`
    (which runs only on tags) always move together.
- Dependabot PRs pass the same CI as any other PR; `NU1901`–`NU1904` warnings go through the
  [dependency-audit skill](../.github/skills/dependency-audit/SKILL.md).

---

## Versioning policy

- The shared package version lives once in [`src/Directory.Build.props`](../src/Directory.Build.props)
  as `<Version>1.0.0</Version>` and every supported `src/*` project inherits it.
- **One documented exception:** `SolTechnology.Core.Logging` overrides to `<Version>1.2.0</Version>`
  because it was already published ahead of the rest; keeping its history monotonic requires a version
  above `1.0.0`.
- The deprecated ids (`Scheduler`, `Guards`) stay frozen at `0.5.0` and are `<IsPackable>false>`;
  `ApiClient` and `Story` have no source in the repo.

To cut a new release, bump the shared version in `src/Directory.Build.props` (and Logging's override
if it needs to move independently), then follow the [release runbook](release-runbook-1.0.md).

---

## Unlist vs. deprecate

| Action | What it does | Automatable? |
|---|---|---|
| **Unlist** (`dotnet nuget delete`) | Hides a version from search; still installable by exact version. | ✅ CI (`unlistDeprecatedPackages.yml` workflow). Undo (re-list) is web-UI only. |
| **Deprecate** | Adds the "deprecated" badge + successor message on nuget.org. | ❌ Web UI only — no `dotnet nuget deprecate` command or public API. Optional, not a release blocker. |

---

## `nuget-stats.json`

[`nuget-stats.json`](../nuget-stats.json) at the repo root is a **generated snapshot** of published
package metadata — a convenience cache for tooling and docs, not a source of truth. Do not hand-edit it.
