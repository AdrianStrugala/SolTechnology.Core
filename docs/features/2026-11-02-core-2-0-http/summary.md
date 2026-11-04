---
spec: 2026-11-02-core-2-0-http
review: done (2026-11-02)
premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)
---

# SolTechnology.Core 2.0 — HTTP — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-http.md`](../2026-11-02-core-2-0-http.md).

Gate order: `review` (plan-reviewer) → steps `01`–`10` → `11` retrospective. There is no
step `00`: the premortem gate is skipped by the user (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `result`; `logging`. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | DI resolution test — HTTP | [`steps/01-di-resolution-test.md`](steps/01-di-resolution-test.md) | done |
| 02 | HTTP — never retry unsafe verbs after an exception or timeout | [`steps/02-http-no-unsafe-verb-retry-on-exception.md`](steps/02-http-no-unsafe-verb-retry-on-exception.md) | done |
| 03 | HTTP — request pipeline correctness | [`steps/03-http-request-pipeline-correctness.md`](steps/03-http-request-pipeline-correctness.md) | done |
| 04 | HTTP — upstream health-check disposal and options validation | [`steps/04-http-health-check-and-options-validation.md`](steps/04-http-health-check-and-options-validation.md) | done |
| 05 | HTTP — own namespace for `HttpClientExtensions`; factory-based upstream health check | [`steps/05-http-namespace-and-health-check-factory.md`](steps/05-http-namespace-and-health-check-factory.md) | done |
| 06 | HTTP — drop `Async` from `RequestBuilder` verbs (optional) | [`steps/06-http-request-builder-async-suffix.md`](steps/06-http-request-builder-async-suffix.md) | done |
| 07 | XML docs shipped — HTTP | [`steps/07-xml-docs.md`](steps/07-xml-docs.md) | done |
| 08 | Module docs — Clients and the HTTP production checklist | [`steps/08-docs-clients-http.md`](steps/08-docs-clients-http.md) | to-do |
| 09 | Guide conformance — HTTP | [`steps/09-conform-http.md`](steps/09-conform-http.md) | to-do |
| 10 | Log templates — `[{Placeholder}]` in HTTP (L-03) | [`steps/10-log-template-brackets.md`](steps/10-log-template-brackets.md) | to-do |
| 11 | Retrospective | [`steps/11-retrospective.md`](steps/11-retrospective.md) | to-do |
