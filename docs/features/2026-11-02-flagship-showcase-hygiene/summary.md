---
spec: 2026-11-02-flagship-showcase-hygiene
review: done (2026-11-02)
premortem: go-with-mitigations (2026-11-10)
---

# SolTechnology.Core — flagship showcase hygiene — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-flagship-showcase-hygiene.md`](../2026-11-02-flagship-showcase-hygiene.md).

Gate order: `review` (plan-reviewer) → step `00` premortem → steps `01`–`07` → `08` retrospective.
Split from
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md) on
2026-11-02 (RQ8, user). Implementation starts only after core 02, 03, 04, 05, 07, result 02,
auid 03, tale 03, 07, 11, and api-auth 05 are `done` (former core steps 02, 03, 04, 08, 54, 38,
44, 18, 39, 43, 22; per-step table and links in the brief). All of them are done (verified
2026-11-10 at `c0eb9482`; the step files were resynced the same day). No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | done |
| 01 | Community health files and root README | [`steps/01-community-health-files-and-root-readme.md`](steps/01-community-health-files-and-root-readme.md) | done |
| 02 | Workflow supply-chain hardening | [`steps/02-workflow-supply-chain-hardening.md`](steps/02-workflow-supply-chain-hardening.md) | done |
| 03 | DreamTravel — mediator-only, thin controllers | [`steps/03-dreamtravel-mediator-controllers.md`](steps/03-dreamtravel-mediator-controllers.md) | to-do |
| 04 | Coding Guide self-consistency | [`steps/04-coding-guide-self-consistency.md`](steps/04-coding-guide-self-consistency.md) | to-do |
| 05 | DreamTravel — logic and domain cleanup | [`steps/05-dreamtravel-logic-and-domain-cleanup.md`](steps/05-dreamtravel-logic-and-domain-cleanup.md) | to-do |
| 06 | DreamTravel — run-in-two-minutes README and an honest architecture section | [`steps/06-dreamtravel-run-instructions.md`](steps/06-dreamtravel-run-instructions.md) | to-do |
| 07 | DreamTravel — tests in GitHub CI | [`steps/07-dreamtravel-tests-in-github-ci.md`](steps/07-dreamtravel-tests-in-github-ci.md) | to-do |
| 08 | Retrospective | [`steps/08-retrospective.md`](steps/08-retrospective.md) | to-do |
