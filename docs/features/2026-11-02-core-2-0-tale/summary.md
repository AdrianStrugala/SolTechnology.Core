---
spec: 2026-11-02-core-2-0-tale
review: done (2026-11-02)
premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)
---

# SolTechnology.Core 2.0 — Tale — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-tale.md`](../2026-11-02-core-2-0-tale.md).

Gate order: `review` (plan-reviewer) → steps `01`–`16` → `17` retrospective. There is no
step `00`: the premortem gate is skipped by the user (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `result`; `auid`. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | DI resolution test — Tale | [`steps/01-di-resolution-test.md`](steps/01-di-resolution-test.md) | done |
| 02 | Tale — public `TaleJsonOptions.Default` with Tale error discriminators | [`steps/02-tale-public-json-options.md`](steps/02-tale-public-json-options.md) | done |
| 03 | Tale — per-run state isolation; persist only `TaleManager`-driven runs | [`steps/03-tale-per-run-state-and-scoped-persistence.md`](steps/03-tale-per-run-state-and-scoped-persistence.md) | done |
| 04 | Tale engine — caller-only cancellation, no resume of failed tales, deterministic tests | [`steps/04-tale-engine-cancellation-and-resume.md`](steps/04-tale-engine-cancellation-and-resume.md) | done |
| 05 | Tale — registration and schema safety, correct log scope | [`steps/05-tale-registration-and-schema-safety.md`](steps/05-tale-registration-and-schema-safety.md) | done |
| 06 | Tale controller — consistent JSON options, no internal detail in responses | [`steps/06-tale-controller-hardening.md`](steps/06-tale-controller-hardening.md) | to-do |
| 07 | Tale — `CancellationToken` on chapters | [`steps/07-tale-chapter-cancellation-token.md`](steps/07-tale-chapter-cancellation-token.md) | to-do |
| 08 | Tale — `ITaleRepository` 2.0 signatures (cancellation and names) | [`steps/08-tale-repository-cancellation-and-names.md`](steps/08-tale-repository-cancellation-and-names.md) | to-do |
| 09 | Tale — optimistic concurrency on `ITaleRepository.Save` | [`steps/09-tale-repository-optimistic-concurrency.md`](steps/09-tale-repository-optimistic-concurrency.md) | to-do |
| 10 | Tale — chapter history keyed by step index and full type name | [`steps/10-tale-chapter-history-key.md`](steps/10-tale-chapter-history-key.md) | to-do |
| 11 | Tale — explicit HTTP exposure; internal plumbing types | [`steps/11-tale-http-exposure-opt-in.md`](steps/11-tale-http-exposure-opt-in.md) | to-do |
| 12 | XML docs shipped — Tale | [`steps/12-xml-docs.md`](steps/12-xml-docs.md) | to-do |
| 13 | Module docs — `docs/Tale.md` for the 2.0 Tale contract | [`steps/13-docs-tale.md`](steps/13-docs-tale.md) | to-do |
| 14 | Guide conformance — Tale engine split | [`steps/14-conform-tale-engine.md`](steps/14-conform-tale-engine.md) | to-do |
| 15 | Guide conformance — Tale controller, manager, handler, `Tale`, and tests | [`steps/15-conform-tale-controller-manager-handler.md`](steps/15-conform-tale-controller-manager-handler.md) | to-do |
| 16 | Log templates — `[{Placeholder}]` in Tale (L-03) | [`steps/16-log-template-brackets.md`](steps/16-log-template-brackets.md) | to-do |
| 17 | Retrospective | [`steps/17-retrospective.md`](steps/17-retrospective.md) | to-do |
