---
spec: 2026-11-02-flagship-showcase-hygiene
step: 01
status: to-do
---

# Step 01: Community health files and root README

## Summary
A visitor arriving from the product site checks the community files and the landing page first.
Both are missing pieces; they ship as one PR (merged during plan review, RQ11):

- No CONTRIBUTING, SECURITY, CODE_OF_CONDUCT, issue/PR templates, CODEOWNERS, or CHANGELOG
  (`repo#A6`, `P1-9`). They live in this repository, not in an org-wide `.github` repo, so the
  flagship stays self-contained. Q8 (user, 2026-11-02) fixes the reporting channel, the Code of
  Conduct text, and the code owners.
- The README has no install command, quick start, or minimal sample before the module tables,
  and it ends with the placeholder "*Some ending words*" (`repo#A7`, `README.md:32-53,125`). The
  "API Testing" and "SQL Testing" rows (`README.md:65-66`) link to `docs/Api.md#testing` and
  `docs/SQL.md#testing` instead of the packed testing pages `docs/API.Testing.md` and
  `docs/SQL.Testing.md` that every other companion row uses (`repo#A10`; resync 2026-11-10: both
  anchors exist since the module-docs steps, so the links resolve, but they skip the package
  pages). The "Core (foundation)" row (`README.md:39`) links to `docs/CQRS.md#result-pattern`,
  although `docs/Core.md` documents the package since core-result. The deprecated-package table
  keeps its joke name `dontreadme.md` at the repository root (user, 2026-11-03: "nie dropuj joke z
  kodu"); only its Guards row changes.

## Affected components
- `CONTRIBUTING.md` — NEW — build and test commands (`dotnet build SolTechnology.Core.slnx`, `pwsh ./.github/runTests.ps1`, Docker), links to `docs/ClaudeCodingGuide.md`, `docs/architecture/delivery-workflow.md`, Conventional Commits; the dependency-floor rule for Dependabot PRs (premortem SRM-2)
- `SECURITY.md` — NEW — supported versions (2.x), reporting channel: GitHub Private Vulnerability Reporting (enabled in release step 03, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md))
- `CODE_OF_CONDUCT.md` — NEW — Contributor Covenant 2.1 with attribution
- `CHANGELOG.md` — NEW — `## [2.0.0] - Unreleased` heading filled in release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)); link to the 1.0 release record
- `.github/ISSUE_TEMPLATE/bug_report.yml` — NEW
- `.github/ISSUE_TEMPLATE/feature_request.yml` — NEW
- `.github/ISSUE_TEMPLATE/config.yml` — NEW — security issues routed to SECURITY.md
- `.github/PULL_REQUEST_TEMPLATE.md` — NEW — semver impact, `CLAUDE.md §2` checklist, tests, "dependency floor" item (premortem SRM-2)
- `.github/CODEOWNERS` — NEW — `* @AdrianStrugala`
- `README.md` — EDIT — NEW "Quick start" section (≤ 15 lines: `dotnet add package SolTechnology.Core.CQRS`, `AddSolCQRS(o => o.RegisterQueriesFromAssembly(...))`, one query + handler, `IMediator.Send`); `:65-66` links → `docs/API.Testing.md`, `docs/SQL.Testing.md`; `:39` "Core (foundation)" link → `docs/Core.md`; `:125` placeholder removed; `:53` "Deprecated libraries" link to `dontreadme.md` unchanged
- `dontreadme.md` — EDIT — stays at the root under its name; the `SolTechnology.Core.Guards` row names BCL guards (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`) plus `FluentValidation` as the replacement, the same guards step 04 writes into Guide §12; the projects themselves stay in `src/` for reference (Q1, RQ4)

## Changes
- English only. Each community file stays under one screen; link to existing docs instead of copying them.
- The quick-start snippet compiles against 2.0: copy it into a throwaway console project (not committed) that references `src/SolTechnology.Core.CQRS` through a `ProjectReference` or a local `dotnet pack` feed, then run `dotnet build` (the method of api-auth 08 and the other module-docs steps; former core step 59). Release 05 repeats the check against the published 2.0.0 package.
- Badge and doc URLs keep `AdrianStrugala` until release step 02 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)).
- Prerequisite: core 04 and core 07 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)) and result 02 ([`2026-11-02-core-2-0-core-result`](../../2026-11-02-core-2-0-core-result.md)) are merged (former core steps 04, 54, 38), so the quick start compiles against the final 2.0 `Result` and package graph. Done (verified 2026-11-10 at `c0eb9482`: `<Version>2.0.0</Version>` in `src/Directory.Build.props`, `Result` in `src/SolTechnology.Core/Result.cs`).
- Community Standards items that are repository settings, not files (the About description and accepting content reports), need admin rights; the owner sets them (resync 2026-11-10).

- Premortem SRM-1 (step 00, 2026-11-10, scenario 1: `SECURITY.md` promises a private channel that release 03 enables only after the transfer): the owner enables Private Vulnerability Reporting on the current repository (Settings → Code security; admin only) before `SECURITY.md` merges. The setting moves with the repository; release 03 only re-checks it.
- Premortem SRM-2 (scenario 2: Dependabot raises published dependency floors): `CONTRIBUTING.md` and the PR template state that a PR changing a version in the `Directory.Packages.props` group "Library and testing-kit dependencies" updates `.github/skills/package-management/references/canonical-versions.md` and adds a line under `CHANGELOG.md` `Unreleased` in the same PR; for a Dependabot PR, push both to its branch before merging. Major bumps never come from Dependabot (step 02) and go through the `package-management` skill.
- Premortem SRM-18 (scenario 18): the throwaway quick-start project lives outside the repository tree (so it inherits neither the root `Directory.Build.props` nor Central Package Management) and restores from a local `dotnet pack` feed; it is never committed.

## Acceptance criteria
- [ ] GitHub "Community Standards" page (Insights → Community) shows every item checked after merge (the settings-only items owner-confirmed).
- [ ] `grep -n "Some ending words" README.md` prints nothing.
- [ ] `test -f dontreadme.md` succeeds, `test -e docs/deprecated-packages.md` fails, and `grep -n "ThrowIfNull" dontreadme.md` prints the Guards row.
- [ ] The documentation-cleanup skill reports no broken links in `README.md`, `dontreadme.md`, and the new community files.
- [ ] The quick-start snippet builds in a project outside the repository tree (SRM-18).
- [ ] SRM-1: Private Vulnerability Reporting is enabled on the repository before `SECURITY.md` merges (owner-confirmed, dated).
- [ ] SRM-2: `CONTRIBUTING.md` and `.github/PULL_REQUEST_TEMPLATE.md` carry the dependency-floor rule.

## Open questions
- none (Q8 resolved by the user on 2026-11-02: GitHub Private Vulnerability Reporting; Contributor Covenant 2.1; `* @AdrianStrugala`)

## Deviations
