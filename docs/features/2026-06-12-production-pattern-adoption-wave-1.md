---
status: completed
created: 2026-06-12
completed: 2026-06-19
---

# Production Hardening of SolTechnology.Core Libraries

> Historical delivery record. It may not describe the current system.

## Goal

A catalogue of patterns harvested from a sample production application identified gaps in the
`SolTechnology.Core.*` libraries. Hangfire filters had already shipped. This feature adopted the
remaining backlog as a single production-hardening effort: one plan, steps grouped by module.

### Resolved open questions (2026-06-16)

| # | Question | Decision |
|---|---|---|
| Q1 | Redis client | `Microsoft.Extensions.Caching.StackExchangeRedis` (`IDistributedCache`) |
| Q2 | EF companion package | Documentation-only (`EntityBase` guidance); no new `src/` package |
| Q3 | MessageBus broker switch | **No** seam / `MessageBrokerType` / RabbitMQ — ServiceBus-only |
| Q4 | Single `Result` | Confirmed: `Result`/`Error` canonical in `SolTechnology.Core`; `ResultExtensions` removed (railway moved to Tale DSL) |
| Q5 | `ValidateOnStart` scope | Every module with `AddOptions<T>` (fail-fast everywhere) |

## Scope

Ship all production-hardening work under one feature plan, grouped by module but sequenced as steps.

### What ships

| Group | Items | Module(s) | New dependency | Semver |
|---|---|---|---|---|
| Logging | L1: document `ICorrelationIdService` as canonical · L2: `PushToScope` helpers · L3: `PiiMask` + `[Masked]` attribute | Logging | none | MINOR |
| Cache | C1: `IDistributedTaskCache` over Redis · C2: resilient cache-aside (fail-open) · C3: `AddCachedDecorator` via Scrutor · C4: `CacheKey` + `ICacheInvalidator` | Cache | `Microsoft.Extensions.Caching.StackExchangeRedis`, `Scrutor` 5.0.2 | MINOR |
| SQL | S1: `ISqlConnectionStringProvider` seam · S2: `SqlErrorTranslator` → `Result` · S4: repository `Result` convention (docs) | SQL | `Azure.Identity` (managed-identity provider only) | MINOR |
| Cross-cutting | G1: `TimeProvider` (AUID, Story) · G2: static `JsonSerializerOptions` rule · G3: `ValidateOnStart` everywhere · G5: `MapError` combinator · G6: `[ExcludeFromCodeCoverage]` · G7: primary-ctor rule | CQRS + cross-cutting | none | MINOR |
| MessageBus | M2: wire `ICorrelationIdService` into receiver · M4: extract in-process pipeline | MessageBus | none | MINOR |
| Testing | T1: `UtcDateTimeSpecimen` · T2: composable `AutoNSubstituteDataAttribute` | Testing | none | MINOR |
| Hangfire | H4: document retry-backoff defaults + `MigrateHangfire()` pattern | Hangfire (docs) | none | PATCH |

### Out of scope

- **M1** (broker-agnostic seam) — descoped; MessageBus stays ServiceBus-only.
- **S3** (EF `EntityBase` package) — deferred; documented as guidance only.
- **M3** (dead-letter-with-reason) — already shipped in `MessageBusReceiver`.

### Guard-rails (source-defect rules)

- Masking contract MUST be explicit (`MaskMode.Full` vs `MaskMode.Partial`); never returns empty.
- Managed-identity provider MUST cache token until near expiry; never fetch per-call.
- Logging template/argument mismatch is a review-checklist item in `ClaudeCodingGuide.md` §11.

### Dependency impact

| Package | Module | Already in repo? | Note |
|---|---|---|---|
| `Microsoft.Extensions.Caching.StackExchangeRedis` | Cache | `StackExchange.Redis` 2.8.16 in test companion | Runtime dep via `IDistributedCache` |
| `Scrutor` | Cache | 5.0.2 in DreamTravel | For `AddCachedDecorator<,>` |
| `Azure.Identity` | SQL | no | Only if managed-identity provider is configured |

## Affected modules

`Logging`, `Cache`, `SQL`, `CQRS`, `MessageBus`, `Testing`, `Hangfire` (docs) + AUID & Story for
`TimeProvider`. Sample app: DreamTravel.

## Semver impact

**MINOR** overall (additive APIs + `ValidateOnStart` behaviour fix).

## Related

- [Delivery workflow](../architecture/delivery-workflow.md)
- [CQRS architecture](../architecture/cqrs.md)
- [Background processing architecture](../architecture/background-processing.md)
- [Production pattern adoption — wave 2](2026-06-24-production-pattern-adoption-wave-2.md)

## Completion summary

Completed 2026-06-19. The temporary step folder was deleted after consolidation.

| # | Step | Shipped |
|---|---|---|
| 01 | Logging: correlation + PushToScope + PiiMask | `src/SolTechnology.Core.Logging/` — `ICorrelationIdService`, scope helpers, `[Masked]` attribute |
| 02 | Cache: local + distributed with unified interface | `src/SolTechnology.Core.Cache/` — `ISingletonCache`, `IScopedCache<,>`, Redis `IDistributedCache` wrapper |
| 03 | SQL: error translator + repository convention | `src/SolTechnology.Core.SQL/SqlErrorTranslator.cs` — maps `SqlException` → typed `Error` |
| 04 | Cross-cutting: ValidateOnStart + TimeProvider + coding-guide | `.ValidateOnStart()` on all module installers; `TimeProvider` in AUID + Story; §9.12 + §14 rules |
| 05 | MessageBus: correlation propagation | `MessagePublisher` stamps `CorrelationId`; `MessageBusReceiver` reads + sets + pushes log scope |
| 06 | Testing: UtcDateTimeSpecimen | `src/SolTechnology.Core.Testing/Customizations/UtcDateTimeSpecimen.cs` — default in `AutoNSubstituteData` |
| 07 | Hangfire: document defaults | `docs/Hangfire.md` — retry backoff, worker count, database migration snippet |

### Preserved deviations

- **Step 04** — `MapError` combinator removed from scope; `ResultExtensions` no longer exists after Tale DSL migration.
- **Step 05** — Pipeline extraction (M4) skipped; `HandleMessageAsync` is under the size budget and extracting 5 middleware files would be over-engineering for zero functional gain.


