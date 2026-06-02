# Canonical NuGet versions

Authoritative version pins for SolTechnology.Core. Read by the
[`package-management`](../SKILL.md) skill before any `PackageReference` is added or edited.

When you add a new package or bump an existing one, **update the matching row here in the same
change**. When rows drift between projects, the row records the highest version in use; the
"Known drift" column flags the laggards.

## Build foundation (`src/Directory.Build.props`)

| Package | Version | Why pinned here | Notes |
|---|---|---|---|
| `Microsoft.Extensions.Configuration` | `10.0.1` | All `src/SolTechnology.Core.*` inherit. | Shared-framework family — keep all `Microsoft.Extensions.*` on the same minor. |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.1` | Same. | — |
| `Microsoft.Extensions.DependencyInjection` | `10.0.1` | Same. | — |
| `Microsoft.Extensions.Options` | `10.0.1` | Same. | — |

## `Microsoft.Extensions.*` per-module references

| Package | Version | Used by | Known drift |
|---|---|---|---|
| `Microsoft.Extensions.Hosting.Abstractions` | `10.0.1` | `SolTechnology.Core.MessageBus` | `SolTechnology.Core.Scheduler` still on `10.0.0`. Align on next touch. |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.0` | `SolTechnology.Core.Scheduler` | `SolTechnology.Core.CQRS` pinned `8.0.3` — investigate before bumping (intentional floor for older consumers?). |
| `Microsoft.Extensions.Caching.Memory` | `8.0.1` | `SolTechnology.Core.Cache` | Sits on 8.x while rest of MEL is 10.x. Confirm 10.x has no API churn before bumping. |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.0` | `SolTechnology.Core.Api.Testing` | Mirrors target framework. |
| `Microsoft.AspNetCore.TestHost` | `10.0.0` | `SolTechnology.Core.Api.Testing` | — |

## Resilience / HTTP

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Polly` | `8.5.0` | `SolTechnology.Core.SQL`, indirectly via `Microsoft.Extensions.Http.Resilience` in `SolTechnology.Core.HTTP` | Polly v8 family. v7 is forbidden in new code. |

## CQRS / validation

| Package | Version | Used by | Notes |
|---|---|---|---|
| `MediatR` | `12.3.0` | `SolTechnology.Core.CQRS` | — |
| `FluentValidation` | `11.11.0` | `SolTechnology.Core.CQRS` | `tests/SolTechnology.Core.Api.Tests` pinned `11.10.0` — align on next touch. |
| `FluentValidation.DependencyInjectionExtensions` | `11.11.0` | `SolTechnology.Core.CQRS` | Must match `FluentValidation`. |

## Azure SDKs

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Azure.Messaging.ServiceBus` | `7.20.1` | `SolTechnology.Core.MessageBus` | Modern SDK. NEVER add `Microsoft.Azure.ServiceBus` (deprecated). |
| `Azure.Storage.Blobs` | `12.23.0` | `SolTechnology.Core.BlobStorage` | — |

## SQL / data

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Microsoft.SqlServer.DacFx` | `170.1.61` | `SolTechnology.Core.SQL` (runtime, `SQLProjectDeployer`), `SolTechnology.Core.SQL.Testing` (dacpac deploy) | Stays in `SQL` even though testing moved out. |
| `Testcontainers` | `3.9.0` | `SolTechnology.Core.Testing`, `SolTechnology.Core.SQL.Testing` | Container fixtures. Removed from `Sql` runtime in v0.6.0. |
| `Testcontainers.PostgreSql` | `3.9.0` | `SolTechnology.Core.SQL.Testing` | Postgres engine. Match `Testcontainers`. (MSSQL uses the generic builder — no `Testcontainers.MsSql`.) |
| `Microsoft.Data.SqlClient` | `5.2.2` | `SolTechnology.Core.SQL.Testing` | MSSQL ADO provider + login probe. |
| `Npgsql` | `8.0.5` | `SolTechnology.Core.SQL.Testing` | Postgres ADO provider. |
| `Respawn` | `6.2.1` | `SolTechnology.Core.SQL.Testing` | Between-test database reset (SqlServer + Postgres adapters). |

## Serialisation

| Package | Version | Used by | Notes |
|---|---|---|---|
| `System.Text.Json` | built-in (`net10.0`) | All modules (default) | DEFAULT serialiser. No `PackageReference` needed. |
| `Newtonsoft.Json` | `13.0.4` | `SolTechnology.Core.MessageBus` | Reserved for Service Bus payload compatibility + Hangfire integration. NEVER add to new code without an ADR. |
| `AvroConvert` | `3.4.10` | `SolTechnology.Core.BlobStorage` | Avro support in `DataType.Avro`. |

## Stubbing / fake utilities

| Package | Version | Used by | Notes |
|---|---|---|---|
| `WireMock.Net` | `1.6.8` | `SolTechnology.Core.Faker` | HTTP stubbing for tests. |
| `WireMock.Net.StandAlone` | `1.6.8` | `SolTechnology.Core.Faker` | Must match `WireMock.Net`. |
| `System.Linq.Dynamic.Core` | `1.6.0` | `SolTechnology.Core.Faker` | — |
| `RichardSzalay.MockHttp` | `7.0.0` | `tests/SolTechnology.Core.HTTP.Tests` | Alternative HTTP stub for unit-level tests. |

## Test stack

The current repo uses **xUnit** for the existing tests; `ClaudeCodingGuide §8` mandates **NUnit**
for new tests. Both stacks are listed; pick NUnit for new projects.

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Microsoft.NET.Test.Sdk` | `17.12.0` | All `tests/*` | Required by every test project. |
| `xunit` | `2.9.2` | All `tests/*` (legacy) | Keep for existing tests. Do not add to a new test project. |
| `xunit.runner.visualstudio` | `2.8.2` | All `tests/*` (legacy) | Must match `xunit` major. |
| `Microsoft.NET.Test.Sdk` (NUnit) | `17.12.0` | New NUnit test projects | Same SDK as xUnit projects. |
| `NUnit` | `4.2.2` | `SolTechnology.Core.Testing` (first new NUnit project); DreamTravel sample app | Recorded per `ClaudeCodingGuide §8`. |
| `FluentAssertions` | `6.12.2` | Most `tests/*` | `tests/SolTechnology.Core.Logging.Tests` on `6.12.1` — align on next touch. |
| `NSubstitute` | `5.3.0` | `tests/SolTechnology.Core.Api.Tests`, `SolTechnology.Core.Testing` | House mock — `Moq` is anti-stack. |
| `AutoFixture` | `4.18.1` | `SolTechnology.Core.Testing` | Core 4.x family caps at 4.18.1. |
| `AutoFixture.AutoNSubstitute` | `4.18.1` | `SolTechnology.Core.Testing` | NSubstitute auto-faking (not `AutoFixture.AutoMoq` — Moq is anti-stack). |
| `AutoFixture.NUnit4` | `4.19.0` | `SolTechnology.Core.Testing` | NUnit4 adapter ships a 4.19.0; depends on AutoFixture ≥ 4.18.1. |
| `Bogus` | `35.6.1` | `SolTechnology.Core.Testing` (opt-in customization) | Realistic data generator; complements AutoFixture, does not replace it. |
| `Docker.DotNet` | `3.125.15` | `SolTechnology.Core.Testing` | Container restart-if-stopped + health/AMQP probes. |
| `Serilog.Sinks.InMemory` | `0.11.0` | `SolTechnology.Core.Testing` | In-memory log assertions. |
| `System.Text.RegularExpressions` | `4.3.1` | `SolTechnology.Core.Testing` | CVE override (CVE-2019-0820 / GHSA-cmhx-cq75-c4mj, HIGH). `Docker.DotNet` drags the vulnerable 4.3.0; this prunes it. net10 carries it in-framework, so consumers with package pruning see NU1510 (demoted repo-wide). |
| `coverlet.collector` | `6.0.4` | All `tests/*` | Coverage collector — wire as a `PrivateAssets="all"` developer dependency. |

## Anti-stack — never add to this repo

| Package | Reason |
|---|---|
| `Microsoft.Azure.ServiceBus` | Deprecated. Use `Azure.Messaging.ServiceBus`. |
| `Moq` | Use `NSubstitute`. |
| `Polly` v7.x | Use Polly v8. |
| `AutoMapper` | Hand-rolled mappers per `ClaudeCodingGuide §5`. |
| `Newtonsoft.Json` in new code | Use `System.Text.Json`. Only exceptions: `MessageBus`, Hangfire-integrated paths — both listed above. |

## Maintenance

- Bumping a row: also bump every project that uses it (or document the laggard in the "Known
  drift" column).
- New row: cite the project that introduced it.
- Removing a package: delete the row when no `.csproj` references it.
- This file is the source of truth; `.csproj` files are downstream. Disagreements are bugs in
  this file or the `.csproj` — fix both.

