---
spec: 2026-11-02-core-2-0-auid
review: done (2026-11-02)
premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)
---

# SolTechnology.Core 2.0 — AUID — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-auid.md`](../2026-11-02-core-2-0-auid.md).

Gate order: `review` (plan-reviewer) → steps `01`–`06` → `07` retrospective. There is no
step `00`: the premortem gate is skipped by the user (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: none. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | AUID — culture-invariant formatting, range-checked parsing | [`steps/01-auid-invariant-format-and-range-checked-parse.md`](steps/01-auid-invariant-format-and-range-checked-parse.md) | done |
| 02 | AUID — standards-compliant converters; honest uniqueness and ordering docs | [`steps/02-auid-converters-and-docs.md`](steps/02-auid-converters-and-docs.md) | done |
| 03 | AUID — `SolTechnology.Core.AUID` namespace; no process-wide `TimeProvider` | [`steps/03-auid-namespace-and-time-provider.md`](steps/03-auid-namespace-and-time-provider.md) | done |
| 04 | XML docs shipped — AUID | [`steps/04-xml-docs.md`](steps/04-xml-docs.md) | to-do |
| 05 | Module docs — `docs/AUID.md` for the 2.0 namespace and `TimeProvider` overloads | [`steps/05-docs-auid.md`](steps/05-docs-auid.md) | to-do |
| 06 | Guide conformance — AUID | [`steps/06-conform-auid.md`](steps/06-conform-auid.md) | to-do |
| 07 | Retrospective | [`steps/07-retrospective.md`](steps/07-retrospective.md) | to-do |
