---
spec: 2026-11-02-core-2-0-api-auth
review: pending
premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)
---

# SolTechnology.Core 2.0 — Api and Authentication — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-api-auth.md`](../2026-11-02-core-2-0-api-auth.md).

Gate order: `review` (plan-reviewer) → steps `01`–`09` → `10` retrospective. There is no
step `00`: the premortem gate is skipped by the user (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `result`; `logging`; `http`. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | DI resolution test — Api and Authentication | [`steps/01-di-resolution-test.md`](steps/01-di-resolution-test.md) | to-do |
| 02 | Keep upstream URIs and query strings out of `ProblemDetails` and exception messages | [`steps/02-no-upstream-uri-in-problem-details.md`](steps/02-no-upstream-uri-in-problem-details.md) | to-do |
| 03 | Authentication — constant-time API-key comparison and per-instance logger | [`steps/03-constant-time-api-key-comparison.md`](steps/03-constant-time-api-key-comparison.md) | to-do |
| 04 | Api — keep 201/202 in `ResultConversionFilter`; real filter registration | [`steps/04-api-result-filter-status-preservation.md`](steps/04-api-result-filter-status-preservation.md) | to-do |
| 05 | Authentication and Api — public surface cleanup | [`steps/05-auth-api-public-surface.md`](steps/05-auth-api-public-surface.md) | to-do |
| 06 | XML docs shipped — API, Authentication | [`steps/06-xml-docs.md`](steps/06-xml-docs.md) | to-do |
| 07 | Module docs — Api and Auth | [`steps/07-docs-api-auth.md`](steps/07-docs-api-auth.md) | to-do |
| 08 | Guide conformance — Api and Authentication | [`steps/08-conform-api-auth.md`](steps/08-conform-api-auth.md) | to-do |
| 09 | Log templates — `[{Placeholder}]` in Api (L-03) | [`steps/09-log-template-brackets.md`](steps/09-log-template-brackets.md) | to-do |
| 10 | Retrospective | [`steps/10-retrospective.md`](steps/10-retrospective.md) | to-do |
