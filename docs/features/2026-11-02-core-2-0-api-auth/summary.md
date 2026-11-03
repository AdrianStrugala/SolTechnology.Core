---
spec: 2026-11-02-core-2-0-api-auth
review: pending
premortem: pending
---

# SolTechnology.Core 2.0 — Api and Auth — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-api-auth.md`](../2026-11-02-core-2-0-api-auth.md).

Gate order: `review` (plan-reviewer) → step `00` premortem (step 01 only) → steps `01`–`10` →
`11` retrospective. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `result`; `logging`; `http`. No step
is `blocked`.

Gate history:

- 2026-11-02: `review` was `done (2026-11-02)`; `premortem` was `skipped (covered by core premortem
  go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)`.
  That skip still covers steps 02–10 (former 01–09); their `RM-n` mitigations stay verbatim in the
  steps that carry them.
- 2026-11-03 (scope change, user: rename `SolTechnology.Core.Authentication` to
  `SolTechnology.Core.Auth`, `C-28`): step 01 was inserted and former steps 01–10 became 02–11.
  `review` is `pending` again because the plan gained a breaking public API step after review.
  `premortem` is `pending` because step 01 edits a `ModuleInstaller.cs` and the core premortem
  predates `C-28`; step 00 runs it for step 01 only. Only the user may skip either gate.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) — step 01 rename | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | to-do |
| 01 | Auth — rename SolTechnology.Core.Authentication to SolTechnology.Core.Auth | [`steps/01-rename-authentication-to-auth.md`](steps/01-rename-authentication-to-auth.md) | to-do |
| 02 | DI resolution test — Api and Auth | [`steps/02-di-resolution-test.md`](steps/02-di-resolution-test.md) | to-do |
| 03 | Keep upstream URIs and query strings out of `ProblemDetails` and exception messages | [`steps/03-no-upstream-uri-in-problem-details.md`](steps/03-no-upstream-uri-in-problem-details.md) | to-do |
| 04 | Auth — constant-time API-key comparison and per-instance logger | [`steps/04-constant-time-api-key-comparison.md`](steps/04-constant-time-api-key-comparison.md) | to-do |
| 05 | Api — keep 201/202 in `ResultConversionFilter`; real filter registration | [`steps/05-api-result-filter-status-preservation.md`](steps/05-api-result-filter-status-preservation.md) | to-do |
| 06 | Auth and Api — public surface cleanup | [`steps/06-auth-api-public-surface.md`](steps/06-auth-api-public-surface.md) | to-do |
| 07 | XML docs shipped — API, Auth | [`steps/07-xml-docs.md`](steps/07-xml-docs.md) | to-do |
| 08 | Module docs — Api and Auth | [`steps/08-docs-api-auth.md`](steps/08-docs-api-auth.md) | to-do |
| 09 | Guide conformance — Api and Auth | [`steps/09-conform-api-auth.md`](steps/09-conform-api-auth.md) | to-do |
| 10 | Log templates — `[{Placeholder}]` in Api (L-03) | [`steps/10-log-template-brackets.md`](steps/10-log-template-brackets.md) | to-do |
| 11 | Retrospective | [`steps/11-retrospective.md`](steps/11-retrospective.md) | to-do |
