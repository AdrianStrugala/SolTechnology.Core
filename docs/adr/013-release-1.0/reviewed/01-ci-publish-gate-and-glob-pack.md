---
adr: 013-release-1.0
step: 01 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): locked the release trigger to tag v1.0.0 AND workflow_dispatch (answer 12);
     locked "stop packing Scheduler/Guards now" (answer 10) and removed the "one final deprecated
     publish" machinery; added the M3 guard (the slnx-driven glob silently drops any packable project
     missing from .slnx); aligned the pack `-o` path with the push glob (nit); softened the actionlint
     acceptance to "valid YAML" (nit).
     2026-06-30 (Tale decision): noted the glob packs the renamed SolTechnology.Core.Tale automatically
     after step 05b and never re-packs the old .Story id.
     2026-06-30 (unlist decision — answer to "manage packages in one place"): added the automated
     unlist job for the four ghost packages (ApiClient, Story, Scheduler, Guards). nuget.org
     server-side *deprecation* has NO public CLI/API (web UI only — verified against MS Learn
     2025-10-31), so it is dropped; the repo-automatable action is `dotnet nuget delete` = *unlist*,
     done per-version across every published version (enumerated dynamically from the flat-container
     index — versions are NOT hardcoded). -->

# Step 01: Gate publishing behind a release trigger + pack every packable project by glob

## Summary
Rework `.github/workflows/publishPackages.yml` so `dotnet nuget push` runs only on a deliberate
release trigger (git tag `v1.0.0` **or** `workflow_dispatch`) instead of every `master` push, and
replace the 14 hand-listed `dotnet pack` steps with a single glob/loop over every packable project in
`SolTechnology.Core.slnx`. This lands **first** as a safety rail: the later breaking-rename PRs
(03–06) merge to `master` carrying renamed APIs at unchanged `0.x` versions, and this gate guarantees
none of them auto-publish a broken patch. Pure pipeline configuration — its own PR.

## Affected components
- `.github/workflows/publishPackages.yml` — EDIT — pack-by-glob + push gating + the slnx-membership guard + the gated unlist job.

## Changes
- Split the single `build` job: keep `restore` → `build` → `test` → `pack` running on `push` +
  `pull_request` (validation); move `dotnet nuget push` into a step/job guarded by
  `if: github.event_name == 'workflow_dispatch' || startsWith(github.ref, 'refs/tags/v')`.
- Add `workflow_dispatch:` and `push: tags: ['v*']` to the `on:` triggers **(answer 12: both)**.
- Replace the 14 `Pack SolTechnology.Core.*` steps with one loop over slnx members, e.g.:
  `for p in $(dotnet sln SolTechnology.Core.slnx list | grep '^src/'); do dotnet pack -c Release -o ./artifacts "$p"; done`
  packing only projects where `IsPackable` is true (test projects set `<IsPackable>false>` via
  `tests/Directory.Build.props`).
- Net effect of the glob: auto-includes `SolTechnology.Core.Hangfire` + the 7 `.Testing` companions
  (`Testing`, `API.Testing`, `HTTP.Testing`, `SQL.Testing`, `Redis.Testing`, `BlobStorage.Testing`,
  `ServiceBus.Testing` — all confirmed present in `.slnx`); removes the hard-coded
  `./src/SolTechnology.Core.API/…` path (folder is `SolTechnology.Core.Api`, csproj is
  `SolTechnology.Core.API.csproj` — a casing mismatch that breaks on case-sensitive Linux runners and
  is sidestepped by the glob).
- **`Story` → `Tale` rename is glob-transparent (decision 13 / step 05b).** Once 05b renames the
  folder + csproj to `SolTechnology.Core.Tale` and repoints the `.slnx` `src/` row, the same glob packs
  **`SolTechnology.Core.Tale`** automatically — no CI edit. The **old `SolTechnology.Core.Story` id is
  never re-packed** (intended: it is frozen at `0.8.0` and deprecated + unlisted server-side via the
  step-10 runbook). The slnx-membership guard therefore expects `.Tale` **present** and `.Story`
  **absent** after 05b; if both appear (an incomplete rename), the guard's "every packable `src/`
  project is in `.slnx`" check still passes, so 05b's own build-green criterion is the gate that a
  stray `.Story` project is gone — not this guard.
- **Scheduler / Guards: stop packing now (answer 10).** Both projects are already **absent** from
  `SolTechnology.Core.slnx`, so the slnx-driven glob excludes them for free — no `Pack` step, no
  `<IsPackable>false>` toggle required for CI. `[Obsolete]` in source (step 07) + nuget.org
  deprecate/unlist (step 10) protect existing consumers. There is **no** "one final deprecated
  publish."
- **Guard the relocated "forgot CI" risk (M3).** Driving the glob off `.slnx` means a *new* packable
  `src/` project that someone forgets to add to `.slnx` is silently never packed — the same bug class
  the glob was meant to kill, just moved from "forgot a Pack step" to "forgot the slnx entry". Add a
  fail-fast check before pack: enumerate every `src/**/*.csproj` whose effective `IsPackable != false`
  (or that declares a `PackageId`) and assert each appears in `dotnet sln SolTechnology.Core.slnx list`;
  fail the job if any packable project is missing. (`Scheduler`/`Guards` set `<IsPackable>false>` in
  step 07 so they are *expected* absentees and do not trip the guard.)
- **Align pack output with push input (nit).** Pack writes to `-o ./artifacts`; the push step pushes
  `./artifacts/*.nupkg` (not a bare `*.nupkg` from the working dir). Keep `--skip-duplicate` and
  `-k ${{secrets.NUGET_API_KEY}}` on push.
- **Automate unlisting the ghost packages (decision: "manage packages in one place").** Add a separate
  `unlist-deprecated` job, gated on `workflow_dispatch` with a boolean input (e.g.
  `unlist_deprecated: true`) so it never runs on a tag/`master` push. It enumerates **every published
  version** of each ghost id and unlists it — there is **no** "unlist whole package" command, unlist
  is strictly per-version, so versions are pulled live from the flat-container index (never
  hardcoded — `nuget-stats.json` only records the latest):
  ```bash
  for id in SolTechnology.Core.ApiClient SolTechnology.Core.Story \
            SolTechnology.Core.Scheduler SolTechnology.Core.Guards; do
    lower=$(echo "$id" | tr '[:upper:]' '[:lower:]')
    for v in $(curl -fsSL "https://api.nuget.org/v3-flatcontainer/$lower/index.json" | jq -r '.versions[]'); do
      dotnet nuget delete "$id" "$v" --non-interactive \
        --api-key ${{secrets.NUGET_API_KEY}} -s https://api.nuget.org/v3/index.json
    done
  done
  ```
- **Unlist ≠ deprecate (scope boundary — answer 14).** `dotnet nuget delete` on nuget.org **unlists**
  (hides from search); it does **not** set the "deprecated" badge or a successor message. nuget.org
  server-side *deprecation* has **no** public CLI/API (web UI only), so it is **dropped** from the
  automated flow. Consumers learn the successor (`Story → Tale`, `ApiClient → HTTP`,
  `Scheduler → Hangfire`, `Guards → FluentValidation`) from the doc-level migration map
  (`dontreadme.md` + `docs/MIGRATION-0.x-to-1.0.md`, step 10) and from `[Obsolete]` at compile time
  for the source-bearing libs (`Scheduler`/`Guards`, step 07) — **not** from a nuget.org deprecation
  message. The old `Story`/`ApiClient` ids carry no `[Obsolete]` (no source), so their only nuget.org
  signal is the unlist.

## Acceptance criteria
- [ ] `publishPackages.yml` packs every `src/` slnx project with `IsPackable != false` via a loop, with
      no per-project hard-coded `Pack` step.
- [ ] `dotnet nuget push` runs only on tag `v*` or `workflow_dispatch`, never on a plain `master` push
      or a `pull_request`.
- [ ] The pack output (`./artifacts/`) includes `SolTechnology.Core.Hangfire` and all 7 `.Testing`
      companions, and the push step pushes `./artifacts/*.nupkg`.
- [ ] `Scheduler` / `Guards` produce **no** `.nupkg` (absent from `.slnx`; `<IsPackable>false>` in step 07).
- [ ] After step 05b, the pack output contains `SolTechnology.Core.Tale.*` and **never**
      `SolTechnology.Core.Story.*` (the old id is deprecated + unlisted server-side, not re-packed).
- [ ] A guard step fails the job if any packable `src/` project is missing from `.slnx`.
- [ ] A `unlist-deprecated` job (gated on `workflow_dispatch` + a boolean input) unlists **every**
      published version of `ApiClient`, `Story`, `Scheduler`, `Guards` via `dotnet nuget delete`,
      with versions enumerated from the flat-container index (none hardcoded). It never runs on a tag
      or `master` push, and never fires on a normal publish.
- [ ] Workflow YAML parses cleanly (valid YAML; `actionlint` clean if it is available in the
      toolchain) and `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none — release trigger (both tag + dispatch) and the stop-packing decision are resolved at step 00.




