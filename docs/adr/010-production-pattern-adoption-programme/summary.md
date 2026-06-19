# ADR-010: Production hardening — Implementation Summary

Tracking the implementation steps for
[ADR-010](../010-production-pattern-adoption-programme.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Logging: document correlation + `PushToScope` helpers + `PiiMask`/`[Masked]` | [`done/01-logging.md`](done/01-logging.md) | ✅ done |
| 02 | Cache: `IDistributedTaskCache` + resilient cache-aside + decorator + invalidator | [`done/02-cache.md`](done/02-cache.md) | ✅ done |
| 03 | SQL: error translator + repository convention | [`done/03-sql.md`](done/03-sql.md) | ✅ done |
 04  Cross-cutting: `MapError` + `ValidateOnStart` + `TimeProvider` + coding-guide rules  [`done/04-cross-cutting.md`](done/04-cross-cutting.md)  ✅ done 
 05  MessageBus: correlation in receiver + in-process pipeline extraction  [`done/05-messagebus.md`](done/05-messagebus.md)  ✅ done 
 06  Testing: `UtcDateTimeSpecimen` + composable `AutoNSubstituteDataAttribute`  [`done/06-testing.md`](done/06-testing.md)  ✅ done 
| 07 | Hangfire: document defaults + `MigrateHangfire()` pattern | [`to-do/07-hangfire.md`](to-do/07-hangfire.md) | ⬜ to-do |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`.

