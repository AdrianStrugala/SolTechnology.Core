---
spec: 2026-11-02-core-2-0-sql
review: done (2026-11-02)
premortem: skipped (covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)
---

# SolTechnology.Core 2.0 — SQL — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-sql.md`](../2026-11-02-core-2-0-sql.md).

Gate order: `review` (plan-reviewer) → steps `01`–`07` → `08` retrospective. There is no
step `00`: the premortem gate is skipped by the user (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `result`. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | DI resolution test — SQL | [`steps/01-di-resolution-test.md`](steps/01-di-resolution-test.md) | to-do |
| 02 | SQL — `Begin` on `IUnitOfWork` | [`steps/02-sql-unit-of-work-begin-on-interface.md`](steps/02-sql-unit-of-work-begin-on-interface.md) | to-do |
| 03 | SQL — transient-only connection retry, safer error translation, options validation | [`steps/03-sql-connection-retry-and-validation.md`](steps/03-sql-connection-retry-and-validation.md) | to-do |
| 04 | SQL — internal implementations; safe-by-default DACPAC deployment | [`steps/04-sql-internal-implementations-and-safe-dacpac.md`](steps/04-sql-internal-implementations-and-safe-dacpac.md) | to-do |
| 05 | XML docs shipped — SQL | [`steps/05-xml-docs.md`](steps/05-xml-docs.md) | to-do |
| 06 | Module docs — `docs/SQL.md` | [`steps/06-docs-sql.md`](steps/06-docs-sql.md) | to-do |
| 07 | Guide conformance — SQL | [`steps/07-conform-sql.md`](steps/07-conform-sql.md) | to-do |
| 08 | Retrospective | [`steps/08-retrospective.md`](steps/08-retrospective.md) | to-do |
