---
spec: 2026-11-02-org-transfer-and-2-0-release
review: done (2026-11-02)
premortem: go-with-mitigations (2026-11-10)
---

# SolTechnology.Core — Sol Technology org transfer and 2.0.0 release — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-org-transfer-and-2-0-release.md`](../2026-11-02-org-transfer-and-2-0-release.md).

Gate order: `review` (plan-reviewer) → step `00` premortem → steps `01`–`05` → `06` retrospective.
Split from
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md) on
2026-11-02 (RQ8, user). Implementation starts only after the core feature, its 13 per-library
features (listed in the brief's "Depends on"),
[`../2026-11-02-flagship-showcase-hygiene.md`](../2026-11-02-flagship-showcase-hygiene.md),
[`../2026-11-02-logging-chronicle.md`](../2026-11-02-logging-chronicle.md), and
[`../2026-11-05-cache-redis-fail-fast.md`](../2026-11-05-cache-redis-fail-fast.md) (2026-11-05, user: replaces the
abandoned `2026-11-04-cache-redis-reconnect`) are all `completed` (checked 2026-11-10, together with
[`../2026-11-10-chronicle-sql-step-targets.md`](../2026-11-10-chronicle-sql-step-targets.md)). No step is `blocked`.

Resync 2026-11-10 (owner decisions R1–R6 in the brief): no `nuget-release` environment and no reviewer
(tag guard, Trusted Publishing without an environment); the CI gate is phase A of step 01; step 03 is re-scoped
to organization hygiene with no ruleset. The premortem's RRM-1…RRM-18 are folded into steps 01–06.

Who acts: **owner-only** = GitHub or nuget.org admin UI, pushes and tags (`CLAUDE.md §2`); **agent** = repository
edits; **mixed** = both, in the order the step gives.

## Steps

| # | Title | File | Who | Status |
|---|---|---|---|---|
| 00 | Run premortem (gate) | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | agent | done |
| 01 | Push `master`, pass CI on GitHub, transfer the repository, and set up the nuget.org organization | [`steps/01-transfer-repository-and-nuget-org.md`](steps/01-transfer-repository-and-nuget-org.md) | **owner-only** | to-do |
| 02 | License, authorship, and repository URLs for the organization | [`steps/02-license-authorship-and-repository-urls.md`](steps/02-license-authorship-and-repository-urls.md) | agent (owner pushes) | to-do |
| 03 | Org repository hygiene — security settings, presentation, and ghost-ID deprecations (no ruleset) | [`steps/03-org-repository-hygiene.md`](steps/03-org-repository-hygiene.md) | mixed (mostly owner) | to-do |
| 04 | Switch publishing to NuGet Trusted Publishing (no environment) | [`steps/04-switch-to-trusted-publishing.md`](steps/04-switch-to-trusted-publishing.md) | mixed | to-do |
| 05 | Release 2.0.0 | [`steps/05-release-2-0-0.md`](steps/05-release-2-0-0.md) | mixed | to-do |
| 06 | Retrospective | [`steps/06-retrospective.md`](steps/06-retrospective.md) | agent (owner confirms `completed`) | to-do |
