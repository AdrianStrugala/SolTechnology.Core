# ADR-010: Production hardening — Implementation Summary

Tracking the implementation steps for
[ADR-010](../010-production-pattern-adoption-programme.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Logging: document correlation + `PushToScope` helpers + `PiiMask`/`[Masked]` | [`to-do/01-logging.md`](to-do/01-logging.md) | ⬜ to-do |
| 02 | Cache: `IDistributedTaskCache` + resilient cache-aside + decorator + invalidator | [`to-do/02-cache.md`](to-do/02-cache.md) | ⬜ to-do |
| 03 | SQL: provider seam + error translator + repository convention docs | [`to-do/03-sql.md`](to-do/03-sql.md) | ⬜ to-do |
| 04 | Cross-cutting: `MapError` + `ValidateOnStart` + `TimeProvider` + coding-guide rules | [`to-do/04-cross-cutting.md`](to-do/04-cross-cutting.md) | ⬜ to-do |
| 05 | MessageBus: correlation in receiver + in-process pipeline extraction | [`to-do/05-messagebus.md`](to-do/05-messagebus.md) | ⬜ to-do |
| 06 | Testing: `UtcDateTimeSpecimen` + composable `AutoNSubstituteDataAttribute` | [`to-do/06-testing.md`](to-do/06-testing.md) | ⬜ to-do |
| 07 | Hangfire: document defaults + `MigrateHangfire()` pattern | [`to-do/07-hangfire.md`](to-do/07-hangfire.md) | ⬜ to-do |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`.

