# ADR-010: Production-pattern adoption programme — Implementation Summary

Tracking the workstream-launch steps for
[ADR-010](../010-production-pattern-adoption-programme.md). Each step authors one **child ADR** for a
module surface; the child ADRs make the per-module decisions and carry their own premortems. The
final step gates the **programme structure** with a premortem before any workstream code is
implemented.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Resolve programme open questions | [`to-do/01-resolve-open-questions.md`](to-do/01-resolve-open-questions.md) | ⬜ to-do |
| 02 | Author Logging child ADR (L1, L2, L3) | [`to-do/02-author-logging-adr.md`](to-do/02-author-logging-adr.md) | ⬜ to-do |
| 03 | Author Cache child ADR (C1–C4) | [`to-do/03-author-cache-adr.md`](to-do/03-author-cache-adr.md) | ⬜ to-do |
| 04 | Author SQL connection-providers child ADR (S1, S2, S4) | [`to-do/04-author-sql-providers-adr.md`](to-do/04-author-sql-providers-adr.md) | ⬜ to-do |
| 05 | Author cross-cutting child ADR (G1, G2, G3, G5, G6, G7) | [`to-do/05-author-cross-cutting-adr.md`](to-do/05-author-cross-cutting-adr.md) | ⬜ to-do |
| 06 | Author MessageBus child ADR (M1–M4) | [`to-do/06-author-messagebus-adr.md`](to-do/06-author-messagebus-adr.md) | ⬜ to-do |
| 07 | Author Testing-companions child ADR (T1, T2) | [`to-do/07-author-testing-companions-adr.md`](to-do/07-author-testing-companions-adr.md) | ⬜ to-do |
| 08 | Author Hangfire-defaults child ADR (H4) | [`to-do/08-author-hangfire-defaults-adr.md`](to-do/08-author-hangfire-defaults-adr.md) | ⬜ to-do |
| 09 | Author SQL EF-companion child ADR (S3) — conditional | [`to-do/09-author-sql-ef-companion-adr.md`](to-do/09-author-sql-ef-companion-adr.md) | ⬜ to-do |
| 10 | Run the premortem on the programme | [`to-do/10-run-premortem.md`](to-do/10-run-premortem.md) | ⬜ to-do |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's current
location (`to-do/` / `reviewed/` / `done/`).

## Workstream map

| Child ADR (provisional) | Workstream | Items | Depends on | Sequence |
|---|---|---|---|---|
| 011 | Logging correlation + scopes + masking | L1, L2, L3 | — | 1 (foundational) |
| 012 | Cache distributed + resilient + decorator + invalidator | C1–C4 | Q1 | 2 |
| 013 | SQL connection providers + error translation + `Result` | S1, S2, S4 | Azure.Identity | 3 |
| 014 | Cross-cutting coding-guide rules | G1, G2, G3, G5, G6, G7 | Q4, Q5 | 4 |
| 015 | MessageBus broker-agnostic seam | M1–M4 | ADR-011, Q3, Q4 | 5 |
| 016 | Testing companions (UTC specimen + data attribute) | T1, T2 | — | 6 |
| 017 | Hangfire defaults + `MigrateHangfire()` | H4 | — | 6 |
| 018 (conditional) | SQL EF companion (`EntityBase` + timestamps) | S3 | Q2 | 6 |

Child-ADR numbers are provisional; the real number is assigned next-free per
[`docs/adr/README.md`](../README.md) at authoring time. Lower-priority items (C3/C4, S2/S4, L2/L3)
ship as later code steps **inside** their module ADR's own plan, not as separate ADRs.

## Premortem verdict

_Pending step 10._ Implementation of any workstream is blocked until the programme premortem returns
*Go* / *Go with mitigations* and the relevant child ADR's own premortem passes.

