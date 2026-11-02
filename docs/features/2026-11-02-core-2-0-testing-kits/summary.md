---
spec: 2026-11-02-core-2-0-testing-kits
review: done (2026-11-02)
premortem: waived (no CLAUDE.md §4 trigger: no ModuleInstaller.cs, Directory.Build.props, or persisted-contract edit; content covered by core premortem go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — planner, 2026-11-02)
---

# SolTechnology.Core 2.0 — Testing kits — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-testing-kits.md`](../2026-11-02-core-2-0-testing-kits.md).

Gate order: `review` (plan-reviewer) → steps `01`–`06` → `07` retrospective. There is no
step `00`: the premortem gate is waived (frontmatter), and the core premortem's `RM-n` mitigations
stay verbatim in the steps that carry them. Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `http`; `api-auth`; `cache`; `sql`; `blob`; `messagebus`. No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | API and HTTP testing kits — dispose the test host; strict WireMock by default | [`steps/01-http-api-testing-kit-fixes.md`](steps/01-http-api-testing-kit-fixes.md) | to-do |
| 02 | Container fixtures — per-instance state; honour Ryuk settings | [`steps/02-container-fixture-lifecycle.md`](steps/02-container-fixture-lifecycle.md) | to-do |
| 03 | Testing kits — 2.0 surface (optional `Async` renames, WireMock fields) | [`steps/03-testing-kit-surface.md`](steps/03-testing-kit-surface.md) | to-do |
| 04 | XML docs shipped — Testing, HTTP.Testing, API.Testing, SQL.Testing, Redis.Testing, Blob.Testing, ServiceBus.Testing | [`steps/04-xml-docs.md`](steps/04-xml-docs.md) | to-do |
| 05 | Module docs — testing kits | [`steps/05-docs-testing-kits.md`](steps/05-docs-testing-kits.md) | to-do |
| 06 | Guide conformance — testing kits | [`steps/06-conform-testing-kits.md`](steps/06-conform-testing-kits.md) | to-do |
| 07 | Retrospective | [`steps/07-retrospective.md`](steps/07-retrospective.md) | to-do |
