# ADR-008: Testing framework companions — Implementation Summary

Tracking the implementation steps for [ADR-008](../008-testing-framework-companions.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Run premortem (plan gate) | [`done/01-run-premortem.md`](done/01-run-premortem.md) | ✅ done |
| 02 | Foundation package `SolTechnology.Core.Testing` (+ container lifetime/reuse) | [`done/02-core-testing-foundation.md`](done/02-core-testing-foundation.md) | ✅ done |
| 03 | Extract `SQL.Testing` (MSSQL + Postgres, ORM-agnostic) | [`done/03-sql-testing-extract.md`](done/03-sql-testing-extract.md) | ✅ done |
| 04 | Migrate WireMock DSL to `HTTP.Testing` (breaking `Faker` rename) | [`done/04-http-testing-from-faker.md`](done/04-http-testing-from-faker.md) | ✅ done |
| 05 | Extend `API.Testing` (auth clients + config overrides) | [`done/05-api-testing-extensions.md`](done/05-api-testing-extensions.md) | ✅ done |
| 06 | `Redis.Testing` fixture | [`reviewed/06-redis-testing.md`](reviewed/06-redis-testing.md) | 🔍 reviewed |
| 07 | `BlobStorage.Testing` (Azurite, Azure-specific) | [`reviewed/07-blobstorage-testing.md`](reviewed/07-blobstorage-testing.md) | 🔍 reviewed |
| 08 | `ServiceBus.Testing` (Azure Service Bus emulator) | [`reviewed/08-servicebus-testing.md`](reviewed/08-servicebus-testing.md) | 🔍 reviewed |
| 09 | Wire publish workflow for the seven companion packages | [`reviewed/09-publish-workflow.md`](reviewed/09-publish-workflow.md) | 🔍 reviewed |
| 10 | Dogfood: migrate sample apps, delete duplicates | [`reviewed/10-dogfood-sample-apps.md`](reviewed/10-dogfood-sample-apps.md) | 🔍 reviewed |
| 11 | Documentation + per-package readmes | [`reviewed/11-documentation.md`](reviewed/11-documentation.md) | 🔍 reviewed |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's
current location (`to-do/` / `reviewed/` / `done/`).

## Package map

| Package | Step | Action | Version |
|---|---|---|---|
| `SolTechnology.Core.Testing` | 02 | new | 0.1.0 |
| `SolTechnology.Core.SQL.Testing` | 03 | new (extract from `Sql`) | 0.1.0 |
| `SolTechnology.Core.HTTP.Testing` | 04 | new (migrate from `Faker`, **breaking**) | 0.1.0 |
| `SolTechnology.Core.API.Testing` | 05 | extend existing | 0.6.0 → 0.7.0 |
| `SolTechnology.Core.Redis.Testing` | 06 | new | 0.1.0 |
| `SolTechnology.Core.BlobStorage.Testing` | 07 | new (Azurite only) | 0.1.0 |
| `SolTechnology.Core.ServiceBus.Testing` | 08 | new | 0.1.0 |

`SolTechnology.Core.Sql` bumps to the next MINOR in step 03 (type-removal is breaking-in-principle
but test-only and pre-1.0). All seven companions are packed/published by the workflow in step 09.
No `tests/SolTechnology.Core.*.Testing.Tests` projects are created — verification is build + a
documented manual smoke, so PR/CI builds are unaffected.

## Out of scope (follow-up)

- `SolTechnology.Core.MessageBus.Testing` (**RabbitMQ**) — deferred.
- Migrating external snapshots `tests/tests-kyc` and `tests/tests-mts` (used as reference only).
