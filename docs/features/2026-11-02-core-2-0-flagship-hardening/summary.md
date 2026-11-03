---
spec: 2026-11-02-core-2-0-flagship-hardening
review: done (2026-11-02)
premortem: go-with-mitigations (2026-11-02)
---

# SolTechnology.Core 2.0 — flagship hardening — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md).

Gate order: `review` (plan-reviewer) → step `00` premortem → steps `01`–`08` → `09` retrospective.
The user answered `Q1`–`Q10`, confirmed `C-01`–`C-24`, and rejected `C-25` on 2026-11-02, then
answered the plan-review questions `RQ1`–`RQ12` and confirmed the additive `C-26`/`C-27` the same
day (see the feature brief). No step is `blocked`.

Per-library regroup (user, 2026-11-02): former steps 05–70 were regrouped into one feature per
library, and the remaining steps here were renumbered `00`–`09`. The feature brief's "Per-library
regroup" section lists the library feature briefs, the delivery order, and the former → current
step mapping. Every library feature starts after steps 02–04 are `done`. Step 08 runs after every
library XML-docs step; step 09 runs last, after every library feature is `completed`. The review
and premortem verdicts above cover the regrouped plan (user decision, 2026-11-02).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | done |
| 01 | Clear NU1903 — Testcontainers and DreamTravel parent bumps, Pkcs override in SQL | [`steps/01-clear-nu1903-at-source.md`](steps/01-clear-nu1903-at-source.md) | done |
| 02 | Fail CI on any test-project failure | [`steps/02-fail-ci-on-any-test-failure.md`](steps/02-fail-ci-on-any-test-failure.md) | done |
| 03 | Publish to nuget.org only from `v*` tags behind a protected environment | [`steps/03-publish-only-from-tags.md`](steps/03-publish-only-from-tags.md) | done |
| 04 | Bump every package to 2.0.0 | [`steps/04-bump-version-2-0-0.md`](steps/04-bump-version-2-0-0.md) | done |
| 05 | DreamTravel — Development-only developer exception page | [`steps/05-dreamtravel-dev-only-exception-page-and-credentials.md`](steps/05-dreamtravel-dev-only-exception-page-and-credentials.md) | to-do |
| 06 | Remove leaked-secret artifacts and personal settings; record revocation | [`steps/06-remove-leaked-secret-artifacts.md`](steps/06-remove-leaked-secret-artifacts.md) | to-do |
| 07 | Packaging — a truly zero-dependency `SolTechnology.Core` | [`steps/07-zero-dependency-base-package.md`](steps/07-zero-dependency-base-package.md) | to-do |
| 08 | XML docs — one central `GenerateDocumentationFile` switch | [`steps/08-xml-docs-central-switch.md`](steps/08-xml-docs-central-switch.md) | to-do |
| 09 | Retrospective | [`steps/09-retrospective.md`](steps/09-retrospective.md) | to-do |
