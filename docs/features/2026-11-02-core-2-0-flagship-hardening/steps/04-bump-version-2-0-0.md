---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 04
status: to-do
---

# Step 04: Bump every package to 2.0.0

## Summary
D2 makes this a major release. All supported packages move from `1.0.0` to `2.0.0`. Logging
drops its `1.2.0` override and joins the shared version, so every package carries the same
number again. It runs right after step 03 (RQ3, user, 2026-11-02), so every behavioural break in
phases D–F lands on a `master` that already says 2.0.0, and no 1.x tag can be cut from it by
mistake. Publishing stays tag-only (step 03), so the bump has no effect on nuget.org until
`v2.0.0` is tagged in release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)). The step edits `src/Directory.Build.props`, which is
premortem-gated.

## Affected components
- `src/Directory.Build.props` — EDIT — `<Version>1.0.0</Version>` → `<Version>2.0.0</Version>`
- `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` — EDIT — delete `<Version>1.2.0</Version>` (`:4`)
- `src/SolTechnology.Core.Lockstep.targets` — NEW — premortem RM-1: `buildTransitive` lockstep check (a file directly under `src/`, not a new folder, per `CLAUDE.md §2`)
- `src/Directory.Build.props` — EDIT — premortem RM-1: pack the targets file into every packable package as `buildTransitive/$(PackageId).targets`
- `docs/features/2026-11-02-core-2-0-flagship-hardening.md` — EDIT — premortem RM-1: release-notes input
- `docs/architecture/package-release.md` — not edited here (the retrospective updates it)

## Changes
- Guards and Scheduler keep `0.5.0` (kept for reference, deprecated, not packable; Q1, RQ4).
- 1.x hotfixes, if ever needed, branch from the `v1.0.0` tag, never from `master`.
- Premortem RM-1 (step 00, 2026-11-02, scenario 1: Logging 2.0 next to CQRS 1.x throws `FieldAccessException` on `CoreLoggingActivitySources.Operations`; AUID 2.0 next to Tale 1.x throws `TypeLoadException`):
  - Ship a `buildTransitive` target in every packable package. After package assets are resolved, it inspects the resolved items (for example `@(ReferencePath)` metadata `NuGetPackageId`/`NuGetPackageVersion`). It emits warning `SOLCORE001` ("upgrade every SolTechnology.Core.* package together") when a `SolTechnology.Core.*` package other than Guards and Scheduler has a different major version than the importing package.
  - Use an import guard so the check runs once per project, not once per package.
  - Add to the brief's "Release-notes inputs": "Upgrade every `SolTechnology.Core.*` package to 2.0 together. A mixed 1.x/2.x graph compiles but fails at runtime (`FieldAccessException`, `MissingMethodException`, `TypeLoadException`). Group Dependabot/Renovate updates for `SolTechnology.Core*`."
  - If the check cannot be made reliable, record a deviation; the release-notes input stays mandatory.

## Acceptance criteria
- [ ] `dotnet pack src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj -o /tmp/p` produces `SolTechnology.Core.Logging.2.0.0.nupkg`.
- [ ] `dotnet msbuild src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -getProperty:Version` prints `2.0.0`.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Premortem mitigations for `Directory.Build.props` are applied.
- [ ] RM-1: a throwaway consumer on a local feed referencing the locally packed `SolTechnology.Core.Logging 2.0.0` and nuget.org `SolTechnology.Core.CQRS 1.0.0` builds with warning `SOLCORE001`. The same consumer with both packages at `2.0.0` builds with no `SOLCORE001`. Both packages contain `buildTransitive/<PackageId>.targets` (`unzip -l`).
- [ ] RM-1: the brief's "Release-notes inputs" contains the lockstep-upgrade note.

## Open questions
- none (`C-01` confirmed by the user on 2026-11-02)

## Deviations
