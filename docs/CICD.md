# CI/CD

How `SolTechnology.Core` is built, tested, packed, and published to nuget.org. The single automation
entry point is [`.github/workflows/publishPackages.yml`](../.github/workflows/publishPackages.yml).
The manual release steps that CI cannot perform are captured in the
[release runbook](release-runbook-1.0.md).

---

## Pipeline — `Build, test and publish`

### Triggers

| Trigger | Build + test | Pack | Publish | Unlist |
|---|:---:|:---:|:---:|:---:|
| `pull_request` → `master` | ✅ | ✅ | — | — |
| `push` → `master` | ✅ | ✅ | ✅ | — |
| `push` tag `v*` | ✅ | ✅ | ✅ | — |
| `workflow_dispatch` (`unlist_deprecated` = false) | ✅ | ✅ | ✅ | — |
| `workflow_dispatch` (`unlist_deprecated` = true) | ✅ | ✅ | ✅ | ✅ |

Publish runs only for `workflow_dispatch` or a `refs/tags/v*` push. Unlisting the deprecated ghost
ids is a separate, opt-in `workflow_dispatch` boolean — it never runs on a tag or `master` push.

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

### `unlist-deprecated` job

Opt-in only (`workflow_dispatch` + `unlist_deprecated` = true). Enumerates every published version of
the four ghost ids — `ApiClient`, `Story`, `Scheduler`, `Guards` — live from the flat-container index
and unlists each with `dotnet nuget delete`. The four ids are **hardcoded** in the job, so even a
full-account key cannot unlist a supported package.

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
| **Unlist** (`dotnet nuget delete`) | Hides a version from search; still installable by exact version. | ✅ CI (`unlist-deprecated` job). Undo (re-list) is web-UI only. |
| **Deprecate** | Adds the "deprecated" badge + successor message on nuget.org. | ❌ Web UI only — no `dotnet nuget deprecate` command or public API. Optional, not a release blocker. |

---

## `nuget-stats.json`

[`nuget-stats.json`](../nuget-stats.json) at the repo root is a **generated snapshot** of published
package metadata — a convenience cache for tooling and docs, not a source of truth. Do not hand-edit it.
