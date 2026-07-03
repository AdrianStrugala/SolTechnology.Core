# Release runbook — 1.0.0

Ordered, repeatable steps for cutting the `1.0.0` release of the `SolTechnology.Core.*` packages.
Everything the repository can automate runs from
[`.github/workflows/publishPackages.yml`](../.github/workflows/publishPackages.yml); the remaining
steps are manual nuget.org web-UI actions that **cannot** be scripted (no CLI/API exists).

> **Pre-1.0 note.** No consumer-migration artifact ships with this release: the project was pre-1.0,
> so no cross-version state or API compatibility was ever promised. Persisted `Story`/`Tale`
> workflow-state migration is explicitly out of scope — the default in-memory repository is
> unaffected, and the `Story → Tale` change is a package + `using` + type rename.

---

## 1. Pre-flight (before tagging)

- [ ] `master` is green: `dotnet build SolTechnology.Core.slnx -c Release` + `./.github/runTests.ps1`.
- [ ] Versions resolve as expected (shared `1.0.0`, Logging override `1.2.0`):
      `dotnet msbuild src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj -getProperty:Version`
      returns `1.2.0`; a spot-check of any other supported project returns `1.0.0`.
- [ ] The four deprecated ids (`ApiClient`, `Story`, `Scheduler`, `Guards`) are **not** in the pack set:
      `Scheduler` + `Guards` are outside `SolTechnology.Core.slnx` and carry `<IsPackable>false>`;
      `ApiClient` + `Story` have no source in the repo (ghost ids).
- [ ] `NUGET_API_KEY` is present as a repository secret (used by both the publish push and the
      opt-in unlist job).

---

## 2. Publish (automated)

The publish job pushes **every** packable `src/*` project picked up by the slnx pack glob.

Trigger **one** of:

- **Tag** — push an annotated tag `v1.0.0`:
  ```bash
  git tag -a v1.0.0 -m "Release 1.0.0"
  git push origin v1.0.0
  ```
  The `Publish all nuget packages` step runs on `refs/tags/v*`.
- **Manual** — run the `Build, test and publish` workflow via **workflow_dispatch**.

The push uses `--skip-duplicate`, so a re-run cannot clobber an already-published version.

---

## 3. Verify the publish

- [ ] `SolTechnology.Core.Logging` published as **`1.2.0`** (not `1.0.0`) — the one deliberate version
      override.
- [ ] `SolTechnology.Core.Tale` published as **`1.0.0`** — a **new package id**, so this is its first
      version (not an upgrade of `SolTechnology.Core.Story`).
- [ ] `SolTechnology.Core.Hangfire` and the seven `.Testing` companions
      (`API.Testing`, `Blob.Testing`, `HTTP.Testing`, `Redis.Testing`, `ServiceBus.Testing`,
      `SQL.Testing`, `Testing`) appear on nuget.org at `1.0.0`.
- [ ] Each package's per-package README renders on its nuget.org page. For `Tale`, the README source
      is the `docs/Tale.md` content.

---

## 4. Unlist the deprecated ghost ids (automated, manual-only workflow)

Hides the deprecated ghost ids from nuget.org search. Lives in its **own** workflow with no `push`,
tag, or `pull_request` trigger, so a normal release publish can never reach it.

- Run the `Unlist deprecated packages` workflow
  ([`unlistDeprecatedPackages.yml`](../.github/workflows/unlistDeprecatedPackages.yml)) via
  **workflow_dispatch**, typing **`UNLIST`** into the `confirm` input. Any other value is a no-op
  (`if: inputs.confirm == 'UNLIST'`).
- The `unlist-deprecated` job enumerates every published version of `ApiClient`, `Story`, `Scheduler`,
  `Guards`, `BlobStorage` and `BlobStorage.Testing` live from the flat-container index and unlists each
  with `dotnet nuget delete` (needs `NUGET_API_KEY`). The ids are **hardcoded** in the job, so even a
  full-account key cannot unlist a supported package.

> **Undo.** Re-listing an accidentally unlisted version is a **manual web-UI action only** — there is
> no `dotnet nuget` re-list command. If a wrong version is unlisted, re-list it from
> *Manage package → Listing* on nuget.org.

---

## 5. Deprecate on nuget.org (manual, web-UI only, OPTIONAL)

Server-side **deprecation** — the "deprecated" badge plus a successor message — is **not**
automatable: nuget.org exposes it only through the web UI (*Manage package → Deprecation*). There is
no `dotnet nuget deprecate` command or public API (verified MS Learn, 2025-10-31).

This step is **optional** and is **not** a release blocker. If done, mark each ghost id deprecated and
name its successor:

| Deprecated id | Successor to name |
|---|---|
| `SolTechnology.Core.ApiClient` | `SolTechnology.Core.HTTP` |
| `SolTechnology.Core.Story` | `SolTechnology.Core.Tale` |
| `SolTechnology.Core.Scheduler` | `SolTechnology.Core.Hangfire` |
| `SolTechnology.Core.Guards` | `FluentValidation` (auto-discovered by the CQRS pipeline) |
| `SolTechnology.Core.BlobStorage` | `SolTechnology.Core.Blob` |

See [`dontreadme.md`](../dontreadme.md) for the consumer-facing successor map.

---

## 6. Post-release

- [ ] Confirm the four ghost ids show as **unlisted** on nuget.org (hidden from search, still
      installable by exact version).
- [ ] `README.md` badges and the successor notes point at the live packages.
