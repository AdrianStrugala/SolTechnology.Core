# Canonical NuGet versions

Authoritative version pins for SolTechnology.Core. Read by the
[`package-management`](../SKILL.md) skill before any `PackageReference` is added or edited.

When you add a new package or bump an existing one, **update the matching row here in the same
change**. When rows drift between projects, the row records the highest version in use; the
"Known drift" column flags the laggards.

## Build foundation (`src/Directory.Build.props`)

| Package | Version | Why pinned here | Notes |
|---|---|---|---|
| `Microsoft.Extensions.Configuration` | `10.0.9` | All `src/SolTechnology.Core.*` inherit. | Shared-framework family — keep all `Microsoft.Extensions.*` on the same minor. |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.9` | Same. | — |
| `Microsoft.Extensions.DependencyInjection` | `10.0.9` | Same. | — |
| `Microsoft.Extensions.Options` | `10.0.9` | Same. | — |

## `Microsoft.Extensions.*` per-module references

| Package | Version | Used by | Known drift |
|---|---|---|---|
| `Microsoft.Extensions.Hosting.Abstractions` | `10.0.9` | `SolTechnology.Core.MessageBus` | — |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.9` | `SolTechnology.Core.Scheduler` | — |
| `Microsoft.Extensions.Caching.Memory` | `10.0.9` | `SolTechnology.Core.Cache` | Aligned with the rest of the MEL family. |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | `10.0.1` | `SolTechnology.Core.Cache` | — |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.9` | Various | — |
| `Microsoft.Extensions.Logging.Console` | `10.0.9` | Various | — |
| `Microsoft.Extensions.Http.Resilience` | `10.7.0` | `SolTechnology.Core.HTTP` | Aspire-family version (not aligned to plain `10.0.x` MEL minor — ships on its own cadence). |
| `Microsoft.Extensions.ServiceDiscovery` | `10.7.0` | Aspire-hosted apps | Same Aspire-family cadence as `Http.Resilience`. |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.9` | `SolTechnology.Core.API.Testing` | Mirrors target framework. |
| `Microsoft.AspNetCore.TestHost` | `10.0.9` | `SolTechnology.Core.API.Testing` | — |

## Resilience / HTTP

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Polly` | `8.7.0` | `SolTechnology.Core.SQL`, indirectly via `Microsoft.Extensions.Http.Resilience` in `SolTechnology.Core.HTTP` | Polly v8 family. v7 is forbidden in new code. |

## CQRS / validation

| Package | Version | Used by | Notes |
|---|---|---|---|
| `MediatR` | `12.3.0` | `SolTechnology.Core.CQRS` | — |
| `FluentValidation` | `12.1.1` | `SolTechnology.Core.CQRS`, `tests/*` (via `tests/Directory.Build.props`) | Aligned repo-wide. |
| `FluentValidation.DependencyInjectionExtensions` | `11.11.0` | `SolTechnology.Core.CQRS` | Lags `FluentValidation` 12.x — no compatible 12.x release was available at bump time; revisit on next touch. |

## API / OpenAPI

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Swashbuckle.AspNetCore` | `10.2.2` | `SolTechnology.Core.Api`, `DreamTravel.Api` | Pulls `Microsoft.OpenApi` v2 transitively — model types live under `Microsoft.OpenApi` namespace (not `Microsoft.OpenApi.Models`), and `*Reference` wrapper types (e.g. `OpenApiSecuritySchemeReference(id, hostDocument)`) replace the old settable `.Reference` property. `AddSecurityRequirement` now takes `Func<OpenApiDocument, OpenApiSecurityRequirement>` — the document parameter MUST be passed into the reference constructor, or the security scheme key serializes as an empty `{}` instead of `SchemeName: []`. |
| `Swashbuckle.AspNetCore.SwaggerGen` | `10.2.2` | Same | Match `Swashbuckle.AspNetCore`. |
| `Swashbuckle.AspNetCore.SwaggerUI` | `10.2.2` | Same | Match `Swashbuckle.AspNetCore`. |
| `Asp.Versioning.Mvc` | `10.0.0` | `SolTechnology.Core.Api` | — |
| `Asp.Versioning.Mvc.ApiExplorer` | `10.0.0` | Same | Match `Asp.Versioning.Mvc`. |
| `MicroElements.Swashbuckle.FluentValidation` | `7.1.6` | `DreamTravel.Api` | — |

## Azure SDKs

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Azure.Messaging.ServiceBus` | `7.20.1` | `SolTechnology.Core.MessageBus` | Modern SDK. NEVER add `Microsoft.Azure.ServiceBus` (deprecated). |
| `Azure.Storage.Blobs` | `12.29.0` | `SolTechnology.Core.BlobStorage`, `SolTechnology.Core.BlobStorage.Testing` | Match across runtime + testing. |

## SQL / data

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Microsoft.SqlServer.DacFx` | `170.4.83` | `SolTechnology.Core.SQL` (runtime, `SQLProjectDeployer`), `SolTechnology.Core.SQL.Testing` (dacpac deploy) | Stays in `SQL` even though testing moved out. |
| `Testcontainers` | `4.12.0` | `SolTechnology.Core.Testing`, `SolTechnology.Core.SQL.Testing` | Container fixtures. **Family pinned to 4.12.0.** `ContainerLifecycleHelper.cs` was migrated to the 4.12.0 builder API (the old fluent shortcuts changed shape between 4.3.0 and 4.12.0). |
| `Testcontainers.PostgreSql` | `4.12.0` | `SolTechnology.Core.SQL.Testing` | Postgres engine. Match `Testcontainers`. (MSSQL uses the generic builder — no `Testcontainers.MsSql`.) |
| `Microsoft.Data.SqlClient` | `7.0.1` | `SolTechnology.Core.SQL.Testing` | MSSQL ADO provider + login probe. |
| `Microsoft.Data.Sqlite.Core` | `10.0.9` | `SolTechnology.Core.Story` (Sqlite persistence) | Drags `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, which carries an unpatched NU1903 (CVE-2025-6965, no `first_patched_version` upstream yet) — left unmasked per dependency-audit §5 ("nothing to fix at source yet"), not introduced by this bump. |
| `SQLitePCLRaw.bundle_green` | `2.1.11` | `SolTechnology.Core.Story` | Pulls the same unpatched `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 — see above. |
| `Microsoft.EntityFrameworkCore` / `.Design` / `.InMemory` / `.SqlServer` | `10.0.9` | `SolTechnology.Core.SQL`-adjacent and DreamTravel data layers | Keep all four on the same minor. |
| `Npgsql` | `10.0.3` | `SolTechnology.Core.SQL.Testing` | Postgres ADO provider. |
| `Respawn` | `7.0.0` | `SolTechnology.Core.SQL.Testing` | Between-test database reset (SqlServer + Postgres adapters). |
| `Scrutor` | `7.0.0` | DI assembly-scanning consumers | — |
| `Testcontainers.Redis` | `4.12.0` | `SolTechnology.Core.Redis.Testing` | Redis container engine. Match `Testcontainers`. |
| `StackExchange.Redis` | `3.0.0` | `SolTechnology.Core.Redis.Testing` | Used by `RedisFixture.FlushAsync()` (admin-mode `FLUSHALL`). |
| `Testcontainers.Azurite` | `4.12.0` | `SolTechnology.Core.BlobStorage.Testing` | Azurite (Azure Storage emulator) container engine. Match `Testcontainers`. |
| `Testcontainers.ServiceBus` | `4.12.0` | `SolTechnology.Core.ServiceBus.Testing` | Azure Service Bus emulator engine. |
| `Docker.DotNet` | `3.125.15` | `SolTechnology.Core.Testing`, `SolTechnology.Core.ServiceBus.Testing` | Stable-name reuse + restart management for the emulator. |
| `Neo4j.Driver` | `6.2.0` | `DreamTravel.GraphDatabase` | `IDriver` now implements `IAsyncDisposable` directly — dispose via `await Driver.DisposeAsync()`, not the old `CloseAsync()` + `Dispose()` pair. |
| `Cronos` | `0.13.0` | `SolTechnology.Core.Scheduler`/Hangfire-adjacent cron parsing | — |
| `Hangfire.Core` / `.AspNetCore` / `.NetCore` / `.SqlServer` | `1.8.23` | `SolTechnology.Core.Hangfire` | Keep all four on the same minor. |
| `Hangfire.InMemory` | `1.0.0` | `SolTechnology.Core.Hangfire.Testing` | — |

## HTTP / mocking (test companions)

| Package | Version | Used by | Notes |
|---|---|---|---|
| `WireMock.Net` | `2.11.0` | `SolTechnology.Core.HTTP.Testing` (also hosts the retired `SolTechnology.Core.Faker`'s code under `Faker/` — no separate `.csproj` anymore) | Fake HTTP server. `IResponseBuilder` was decomposed into several sub-interfaces (`IBodyResponseBuilder`, `ICallbackResponseBuilder`, `IResponseProvider`, `IWebSocketResponseBuilder`) — old members typed against the concrete `ResponseMessage` class (instead of `IResponseMessage`) and the 3-arg `ProvideResponseAsync` were dropped, not just supplemented. `JsonResponseBuilderDecorator` was rewritten against the new interface members (`Mapping`/`ResponseMessage` properties, 4-arg HttpContext-aware `ProvideResponseAsync`, WebSocket/SSE methods). |
| `WireMock.Net.StandAlone` | `2.11.0` | `SolTechnology.Core.HTTP.Testing` | Standalone server host. Match `WireMock.Net`. |

`System.Linq.Dynamic.Core` was previously pinned here as a transitive-CVE override (CVE-2024-51417, via `WireMock.Net` 1.x → `Handlebars.Net.Helpers.DynamicLinq` → vulnerable `1.3.12`). **Removed** during the 2.11.0 bump: `WireMock.Net` 2.x's dependency tree (verified via `.nuspec` inspection of every `WireMock.Net.*` 2.11.0 package) no longer references `Handlebars.Net.Helpers.DynamicLinq` or `System.Linq.Dynamic.Core` at all, so the override had become dead weight with a stale comment (it still said "patched 1.6.0" while the pin had drifted to 1.7.2). Confirmed via `dotnet list ... --vulnerable` returning empty after removal — do not re-add unless a new transitive CVE is found and confirmed with the same `.nuspec`-inspection method.

## Serialisation

| Package | Version | Used by | Notes |
|---|---|---|---|
| `System.Text.Json` | built-in (`net10.0`) | All modules (default) | DEFAULT serialiser. No `PackageReference` needed. |
| `Newtonsoft.Json` | `13.0.4` | `SolTechnology.Core.MessageBus` | Reserved for Service Bus payload compatibility + Hangfire integration. NEVER add to new code without an ADR. |
| `AvroConvert` | `3.4.16` | `SolTechnology.Core.BlobStorage` | Avro support in `DataType.Avro`. |

`HotChocolate.Language` was pinned in `DreamTravel.Sql.csproj` as a transitive-CVE override
(NU1904 / CVE-2026-40324, via `EntityGraphQL.AspNet` → `EntityGraphQL` → vulnerable `13.9.11`).
**Removed**: `EntityGraphQL` `5.7.2` now declares `HotChocolate.Language >= 13.9.16` (the patched
version) directly in its own `.nuspec` — confirmed by inspection — so the override had become
dead weight (and the comment was already stale: it said "pin 13.9.16" while the actual
`PackageReference` had drifted to `16.2.1`). Also confirmed zero `.cs` usage of `HotChocolate`
anywhere in the repo — it was never used directly, only as an override. `dotnet list ...
--vulnerable` returns empty after removal. Do not re-add unless a new transitive CVE is found
and confirmed via the same `.nuspec`-inspection method.

## Stubbing / fake utilities

`SolTechnology.Core.Faker` no longer exists as a project (no `.csproj`, only stale `bin`/`obj`
artifacts on disk) — its fake-API DSL lives under `src/SolTechnology.Core.HTTP.Testing/Faker/`
now, covered by the `WireMock.Net` row above.

| Package | Version | Used by | Notes |
|---|---|---|---|
| `RichardSzalay.MockHttp` | `7.0.0` | `tests/SolTechnology.Core.HTTP.Tests` | Alternative HTTP stub for unit-level tests. |
| `Bogus` | `35.6.5` | Test data generation | — |

## Test stack

All test projects use **NUnit**. Canonical versions are centralized in `tests/Directory.Build.props`.

| Package | Version | Notes |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | `18.6.0` | Required by every test project. |
| `NUnit` | `4.6.1` | All `tests/*`. |
| `NUnit3TestAdapter` | `6.2.0` | All `tests/*`. |
| `NUnit.Analyzers` | `4.14.0` | All `tests/*`. |
| `AutoFixture.NUnit4` | `4.19.0` | Some `tests/*`. Slightly ahead of the `AutoFixture` core family (4.18.1) — that's the actual latest 4.19.x NUnit4 adapter; not drift to fix. |
| `FluentAssertions` | `7.2.2` | All `tests/*`. **Pinned, do not bump past 7.x** — 8.x switched to a commercial license; 7.2.2 is the last free/Apache-2.0 release. |
| `FluentValidation` | `12.1.1` | All `tests/*`. Matches the runtime row above. |
| `NSubstitute` | `5.3.0` | All `tests/*`. |
| `AutoFixture` | `4.18.1` | All `tests/*`. Core 4.x family caps at 4.18.1. |
| `AutoFixture.AutoNSubstitute` | `4.18.1` | All `tests/*`. NSubstitute auto-faking (not `AutoFixture.AutoMoq` — Moq is anti-stack). |
| `coverlet.collector` | `10.0.1` | All `tests/*`. Coverage collector — wire as a `PrivateAssets="all"` developer dependency. |
| `Verify.NUnit` | `31.20.0` | Snapshot/contract tests (e.g. `DreamTravel.Component.Tests`). |
| `bunit` | `2.7.2` | `DreamTravel.Ui.UnitTests` | The `Bunit.Web` package was merged into the main `bunit` package in 2.x — do NOT add `bunit.web` alongside it (causes a `TestContext` CS0433 ambiguity). `RenderComponent<T>` is `[Obsolete]` in 2.x; use `Render<T>` instead. |
| `Serilog.Sinks.InMemory` | `2.0.0` | `SolTechnology.Core.Testing` | In-memory log assertions. |
| `System.Text.RegularExpressions` | `4.3.1` | `SolTechnology.Core.Testing` | CVE override (CVE-2019-0820 / GHSA-cmhx-cq75-c4mj, HIGH). `Docker.DotNet` drags the vulnerable 4.3.0; this prunes it. net10 carries it in-framework, so consumers with package pruning see NU1510 (demoted repo-wide). |

## DreamTravel sample app — additional packages

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Aspire.Hosting.AppHost` | `13.4.6` | `DreamTravel.AppHost` | — |
| `Aspire.Hosting.SqlServer` | `13.4.6` | `DreamTravel.AppHost` | — |
| `MudBlazor` | `9.5.0` | `DreamTravel.Ui` | — |
| `Microsoft.AspNetCore.Components.WebAssembly` / `.DevServer` | `10.0.9` | `DreamTravel.Ui` | Match target framework minor. |
| `Microsoft.Playwright.NUnit` | `1.60.0` | DreamTravel E2E tests | — |
| `EntityGraphQL.AspNet` | `5.7.2` | `DreamTravel` GraphQL endpoints | — |
| `GraphQL.Server.Ui.Playground` / `.Voyager` | `8.3.3` | Same | — |
| `OpenTelemetry.Extensions.Hosting` / `.Exporter.OpenTelemetryProtocol` | `1.16.0` | `DreamTravel.ServiceDefaults` | — |
| `OpenTelemetry.Instrumentation.AspNetCore` | `1.15.2` | Same | Lags the `1.16.0` OTel core family — latest available at bump time for this instrumentation package. |
| `OpenTelemetry.Instrumentation.Http` / `.Runtime` | `1.15.1` | Same | Same OTel instrumentation cadence as above. |
| `JetBrains.Annotations` | `2026.2.0` | DreamTravel (Verify-plugin contract test annotations) | — |

## Anti-stack — never add to this repo

| Package | Reason |
|---|---|
| `Microsoft.Azure.ServiceBus` | Deprecated. Use `Azure.Messaging.ServiceBus`. |
| `Moq` | Use `NSubstitute`. |
| `Polly` v7.x | Use Polly v8. |
| `AutoMapper` | Hand-rolled mappers per `ClaudeCodingGuide §5`. |
| `Newtonsoft.Json` in new code | Use `System.Text.Json`. Only exceptions: `MessageBus`, Hangfire-integrated paths — both listed above. |
| `xUnit` in new test projects | NUnit-only convention. `tests/SolTechnology.Core.Guards.Tests` uses `xunit`/`xunit.runner.visualstudio` (`2.9.2`/`2.8.2`) but is a **legacy project not referenced by `SolTechnology.Core.slnx`** — excluded from the active build/test pipeline, not a pattern to copy. |
| `FluentAssertions` 8.x+ | Commercial license. Stay on `7.2.2`. |

## Maintenance

- Bumping a row: also bump every project that uses it (or document the laggard in the "Known
  drift" column).
- New row: cite the project that introduced it.
- Removing a package: delete the row when no `.csproj` references it.
- This file is the source of truth; `.csproj` files are downstream. Disagreements are bugs in
  this file or the `.csproj` — fix both.
