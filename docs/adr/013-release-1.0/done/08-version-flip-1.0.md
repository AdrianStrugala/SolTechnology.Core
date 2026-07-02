---
adr: 013-release-1.0
step: 08 of 11
status: done
---

<!-- Reviewed (2026-06-30): tidied the Scheduler/Guards version note to a plain explicit 0.5.0 (answer
     10 = stop packing now, so there is no "final deprecated version"). Versioning itself was already
     locked by ADR-013 §Decision (1).
     2026-06-30 (Tale decision): the supported-project list now reads `Tale` (renamed from `Story` in
     step 05b); SolTechnology.Core.Tale ships its first version cleanly at 1.0.0 (new id, no downgrade). -->

# Step 08: Flip the shared version to `1.0.0` (Logging `1.2.0`) — release gating

## Summary
Switch every supported package to its `1.0` version via a single shared default, with the one
mandatory override for `Logging`. This is sequenced **after** the rename + deprecation so the very
first `1.0.0` artifacts carry the renamed APIs — never a half-renamed `0.x`. Combined with the
release-trigger gate from step 01, this is the deliberate "go-live version" change; the actual push
is the runbook action in step 10.

## Affected components
- `src/Directory.Build.props` — EDIT — add shared `<Version>1.0.0</Version>`.
- `src/SolTechnology.Core.*/*.csproj` (supported) — EDIT — remove the per-project `<Version>` so the shared default applies.
- `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` — EDIT — keep `<Version>1.2.0</Version>` (override).
- `src/SolTechnology.Core.Scheduler/*.csproj`, `src/SolTechnology.Core.Guards/*.csproj` — EDIT — explicit `<Version>0.5.0</Version>` (deprecated; **not** `1.0`).

## Changes
- Add `<Version>1.0.0</Version>` to the shared `<PropertyGroup>` in `src/Directory.Build.props`.
- Delete `<Version>` from each supported project so it inherits `1.0.0`: `Core`, `API`, `HTTP`,
  `AUID`, `Authentication`, `BlobStorage`, `Cache`, `CQRS`, `Tale` (renamed from `Story` in step 05b),
  `MessageBus`, `Hangfire`, `SQL`, and the 7 `.Testing` companions (`Testing`, `API.Testing`,
  `HTTP.Testing`, `SQL.Testing`, `Redis.Testing`, `BlobStorage.Testing`, `ServiceBus.Testing`).
- **`SolTechnology.Core.Tale` ships its FIRST version as `1.0.0` (decision 13).** As a brand-new
  package id it inherits the shared default with **no downgrade concern** — unlike `Logging`, there is
  no prior `Tale` version on nuget.org. The old `SolTechnology.Core.Story` keeps its `0.8.0` (frozen,
  deprecated/unlisted — steps 07/10) and is **not** in this list because step 05b already removed that
  project id from the solution.
- **Keep** `Logging` at `1.2.0` (an inherited `1.0.0` would be a downgrade NuGet rejects — Logging is
  already `1.1.1` on nuget.org).
- **Keep** `Scheduler` / `Guards` off the shared default — pin an explicit `<Version>0.5.0</Version>`
  (they are no longer packed at all after step 07, so they simply retain their current nuget.org
  version; there is no "final deprecated bump").

## Acceptance criteria
- [ ] `dotnet msbuild -getProperty:Version` on every supported `src/` project prints `1.0.0`, except
      `Logging` → `1.2.0`.
- [ ] `SolTechnology.Core.Tale` resolves to `1.0.0` (its first published version — clean, no
      downgrade); no `SolTechnology.Core.Story` project remains to version (removed in step 05b).
- [ ] `Scheduler` / `Guards` resolve to `0.5.0` (non-`1.0`).
- [ ] No supported project keeps a redundant per-project `<Version>` equal to `1.0.0`.
- [ ] `dotnet pack -c Release` (via the step-01 glob) produces `*.1.0.0.nupkg` (and
      `Logging.1.2.0.nupkg`); `dotnet build SolTechnology.Core.slnx` green.

## Open questions
- none — versioning is locked by ADR-013 §Decision (1) and the `Logging` override is mandatory.




