---
spec: 2026-07-06-opentelemetry-support
review: pending
premortem: pending
---

# Plug-and-play OpenTelemetry support — Implementation Summary

Tracking the implementation steps for the spec
[`../2026-07-06-opentelemetry-support.md`](../2026-07-06-opentelemetry-support.md),
implementing [ADR-015](../../adr/015-opentelemetry-first-class-telemetry.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | ⬜ to-do |
| 01 | Logging: `AddSolTelemetry` core | [`steps/01-logging-addsoltelemetry-core.md`](steps/01-logging-addsoltelemetry-core.md) | ⬜ to-do |
| 02 | MessageBus telemetry | [`steps/02-messagebus-telemetry.md`](steps/02-messagebus-telemetry.md) | ⬜ to-do |
| 03 | Tale telemetry | [`steps/03-tale-telemetry.md`](steps/03-tale-telemetry.md) | ⬜ to-do |
| 04 | SQL telemetry | [`steps/04-sql-telemetry.md`](steps/04-sql-telemetry.md) | ⬜ to-do |
| 05 | Cache telemetry | [`steps/05-cache-telemetry.md`](steps/05-cache-telemetry.md) | ⬜ to-do |
| 06 | Documentation | [`steps/06-documentation.md`](steps/06-documentation.md) | ⬜ to-do |
| 07 | DreamTravel migration + E2E verification | [`steps/07-dreamtravel-migration.md`](steps/07-dreamtravel-migration.md) | ⬜ to-do |
| 08 | Retrospective | [`steps/08-retrospective.md`](steps/08-retrospective.md) | ⬜ to-do |

Status values: `⬜ to-do` / `⛔ blocked` / `🔧 in-progress` / `✅ done` — mirrored from each
step file's frontmatter (the source of truth) in the same change that flips it.
Gates per ADR-006 §6–§7: step `00` blocks `01..08` until the `premortem:` field reads
`go` / `go-with-mitigations` / `waived` / `skipped`; the retrospective runs only when every
other step is `✅ done`.
