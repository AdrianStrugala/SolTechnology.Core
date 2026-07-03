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

| Trigger | Build + test | Pack | Publish |
|---|:---:|:---:|:---:|
| `pull_request` → `master` | ✅ | ✅ | — |
| `push` → `master` | ✅ | ✅ | ✅ |
| `push` tag `v*` | ✅ | ✅ | ✅ |
| `workflow_dispatch` | ✅ | ✅ | ✅ |

Publish runs only for `workflow_dispatch` or a `refs/tags/v*` push. Unlisting the deprecated ghost
ids is **not** part of this pipeline — it lives in its own manual-only workflow (see below), so it can
never run on a tag or `master` push.

### `build` job

1. Checkout + set up .NET 10 (`10.0.x`).
2. `dotnet workload restore SolTechnology.Core.slnx` — required before restore/build on a clean CI box.
3. Restore + `dotnet build SolTechnology.Core.slnx --no-restore`.
4. Test via `./.github/runTests.ps1` (walks every project under `tests/`).
5. **slnx-membership guard** — every packable `src/*` project must be listed in
   `SolTechnology.Core.slnx`, otherwise the pack glob would silently skip it. Projects with
   `<IsPackable>false>` and the two deprecated ids kept out of the slnx (`Scheduler`, `Guards`) are
   excluded from the check.
6. **Pack** — enumerates `src/*.csproj` from `dotnet sln … list` and `dotnet pack -c Release` each into
   `./artifacts`.
7. **Publish** (gated) — `dotnet nuget push ./artifacts/*.nupkg` with `--skip-duplicate`, so re-runs
   cannot clobber an already-published version. Needs the `NUGET_API_KEY` repo secret.

## Unlisting deprecated packages — `Unlist deprecated packages`

A dedicated workflow, [`unlistDeprecatedPackages.yml`](../.github/workflows/unlistDeprecatedPackages.yml),
with **only** a `workflow_dispatch` trigger — no `push`, tag, or `pull_request` hook, so a release
publish can never reach it. Running it requires typing `UNLIST` into the `confirm` input
(`if: inputs.confirm == 'UNLIST'`); any other value is a no-op. The job enumerates every published
version of the six ghost ids — `ApiClient`, `Story`, `Scheduler`, `Guards`, `BlobStorage`,
`BlobStorage.Testing` — live from the flat-container index and unlists each with
`dotnet nuget delete`. The ids are **hardcoded** in the job, so even a full-account key cannot unlist
a supported package.

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
