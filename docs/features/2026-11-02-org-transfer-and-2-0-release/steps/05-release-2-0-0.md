---
spec: 2026-11-02-org-transfer-and-2-0-release
step: 05
status: to-do
---

# Step 05: Release 2.0.0

## Summary
This step publishes `SolTechnology.Core.*` 2.0.0 from tag `v2.0.0` in the organization repository,
with release notes and a migration guide. Consumers need both: the release contains every
confirmed `C-nn` change, the behavioural breaks P-01…P-17, and the log contract changes L-01…L-05.
The 1.0 runbook is the template. This is also the first run of Trusted Publishing (step 04); the
interim `NUGET_API_KEY` is revoked only after it succeeds (AR-5).

## Affected components
- `CHANGELOG.md` — EDIT (created in showcase step 01, [`2026-11-02-flagship-showcase-hygiene`](../../2026-11-02-flagship-showcase-hygiene.md)) — `## [2.0.0] - YYYY-MM-DD`: Security (`S3`–`S6`), Fixed (`B1`–`B11`, P1), Breaking (every confirmed `C-nn`), Additive (`C-26`, `C-27`), Behaviour (P-01…P-17), Logging (L-01…L-05)
- `docs/migration-2.0.md` — NEW — per package: before/after code for each confirmed `C-nn`; persisted-data actions for P-03 (restart 1.x tales), P-04 (add the `Version` column), P-05 (1.x event jobs keep running through a legacy overload until 3.0; no drain needed), P-06 (recurring IDs move to `FullName`; legacy `Type.Name` entries are removed automatically at start), P-07 (cold cache, set `InstanceName`), P-17 (set `MaxDeliveryCount` to the entity's value); custom Tale repositories use `TaleJsonOptions.Default` (`C-26`)
- `docs/release-runbook-2.0.md` — NEW — copy of `docs/release-runbook-1.0.md` adapted to tag-only publishing, `nuget-release`, and Trusted Publishing
- `README.md` — EDIT — link to `docs/migration-2.0.md`
- `SolTechnology.Core.slnx` — EDIT — add the two new docs to `/Docs/`
- Git tag `v2.0.0` — MANUAL (owner) — pushed after the PR merges; approval in `nuget-release` after checking that the tag equals `<Version>` in `src/Directory.Build.props` (AR-5)
- GitHub environment `nuget-release` and nuget.org — MANUAL (owner) — after the `v2.0.0` publish succeeds, delete the interim `NUGET_API_KEY` secret and revoke the key on nuget.org (step 04)

## Changes
- Source of the release notes and migration guide: the [`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md) brief — tables "Public API changes requiring `CLAUDE.md §2` confirmation" (`C-nn`), "Behavioural and persisted-contract breaks" (P-01…P-17), "Log contract changes" (L-01…L-05), and "Release-notes inputs" (including the core step 19 AUID note and the core step 32 P-05 note). Its `Deviations` and `Completion summary` override the planned rows.

## Acceptance criteria
- [ ] The `publish` job succeeded for `v2.0.0`.
- [ ] Each of the 20 package pages on nuget.org shows version 2.0.0, its README (no broken images), and the `sol-technology/SolTechnology.Core` repository link.
- [ ] `docs/migration-2.0.md` has one section per confirmed `C-nn` and per P-03/P-04/P-05/P-06/P-07/P-17.
- [ ] `NUGET_API_KEY` no longer exists in `nuget-release`, and the key is revoked on nuget.org (owner-confirmed, dated in Deviations).
- [ ] A fresh `dotnet new console` project using `dotnet add package SolTechnology.Core.CQRS --version 2.0.0` builds the README quick start.

## Open questions
- none

## Deviations
