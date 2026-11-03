---
spec: 2026-11-02-core-2-0-core-result
review: done (2026-11-02)
premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)
---

# SolTechnology.Core 2.0 — Core `Result` and `Error` — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-core-result.md`](../2026-11-02-core-2-0-core-result.md).

Gate order: `review` (plan-reviewer) → steps `01`–`05` → `06` retrospective. There is no
step `00`: the premortem gate is skipped by the user (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: none. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Core — open `Error` polymorphism; `AggregateError` round-trip | [`steps/01-core-open-error-polymorphism.md`](steps/01-core-open-error-polymorphism.md) | done |
| 02 | Core — immutable `Result` and a typed `Result<T>.FailAsTask` | [`steps/02-core-result-immutability.md`](steps/02-core-result-immutability.md) | to-do |
| 03 | XML docs shipped — Core | [`steps/03-xml-docs.md`](steps/03-xml-docs.md) | to-do |
| 04 | Module docs — `docs/Core.md` as the `SolTechnology.Core` package README | [`steps/04-docs-core.md`](steps/04-docs-core.md) | to-do |
| 05 | Guide conformance — Core | [`steps/05-conform-core.md`](steps/05-conform-core.md) | to-do |
| 06 | Retrospective | [`steps/06-retrospective.md`](steps/06-retrospective.md) | to-do |
