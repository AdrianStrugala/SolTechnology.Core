---
status: completed
created: 2026-05-30
completed: 2026-07-01
---

# Testing Framework Companion Packages

> Historical delivery record. It may not describe the current system.

## Goal

Extract reusable test infrastructure into a foundation package and focused `.Testing` companions.

## Context

Fixtures, polling helpers, container lifecycle code, and data-generation customizations were
duplicated across production and test assemblies. Multiple applications carried their own WireMock
and SQL fixtures, Redis and messaging containers, retry polling, and AutoFixture customizations.
Some test-only dependencies also lived in runtime packages.

Existing `API.Testing` and the SQL fixture demonstrated the intended convention: one focused
`.Testing` companion per production concern, referenced only by test projects.

The maintainer fixed several constraints before implementation:

- NUnit was the only supported test framework.
- SQL testing had to move out of the runtime package and support MSSQL and PostgreSQL.
- the WireMock DSL had to move from `SolTechnology.Core.Faker` to an HTTP testing companion;
- Azure Service Bus emulator support was in scope while RabbitMQ remained deferred;
- container reuse, restart, initialization, and real readiness checks were framework concerns.

## Original decision

Ship a shared foundation and six focused companions:

| Package planned | Responsibility |
|---|---|
| `SolTechnology.Core.Testing` | AutoFixture/NSubstitute data, polling, container lifecycle, and log assertions. |
| `SolTechnology.Core.API.Testing` | API fixture, authenticated/anonymous clients, and configuration overrides. |
| `SolTechnology.Core.SQL.Testing` | MSSQL/PostgreSQL engines, schema provisioning, and Respawn reset. |
| `SolTechnology.Core.HTTP.Testing` | WireMock fixture and typed fake-client DSL migrated from Faker. |
| `SolTechnology.Core.Redis.Testing` | Redis container, connection wiring, and reset. |
| `SolTechnology.Core.BlobStorage.Testing` | Azurite fixture for Azure Blob Storage. |
| `SolTechnology.Core.ServiceBus.Testing` | Azure Service Bus emulator and AMQP readiness. |

Applications would compose only the companions they needed in their own setup fixture.

### Data generation

AutoFixture remained the foundation because its NUnit parameter attributes and automatic mock
customization could not be replaced by Bogus. Bogus was accepted only as an optional source of
realistic values. During delivery the repository's NSubstitute convention replaced the originally
proposed Moq attributes.

### SQL fixture boundaries

The fixture contract was database-oriented, not ORM-oriented: start an engine, provision a schema,
return a connection string, and reset state. Engine choice and schema provisioning were independent
axes, allowing Dapper and EF consumers to use one `SQL.Testing` package.

### Container lifecycle

The planned cross-cutting lifecycle included:

- opt-in reuse with stable names and a shared Docker network;
- no disposal of reused containers;
- restart of externally stopped containers and refreshed mapped ports;
- semaphore-guarded one-time initialization and cached connection data;
- database login and AMQP protocol probes instead of TCP-only readiness;
- support for Docker Desktop Enhanced Container Isolation constraints.

## Alternatives considered

### Framework-agnostic fixtures with NUnit and xUnit adapters

Rejected because it doubled the adapter and CI surface without an in-repository xUnit consumer.

### Separate EF and PostgreSQL testing packages

Rejected because ORM choice is invisible to a container fixture and would fragment a small engine
difference across packages and release cadences.

### Keep SQL fixtures in the runtime package

Rejected because every production consumer would receive Testcontainers, Respawn, and schema tools.

### Replace AutoFixture with Bogus

Rejected because Bogus does not provide NUnit auto-data attributes or automatic mocking.

### Keep container lifecycle application-specific

Rejected because every application would continue to copy and independently debug reuse and
readiness logic.

## Scope

- Create foundation, API, SQL, HTTP, Redis, Blob, and Service Bus testing packages.
- Standardize NUnit, AutoFixture, NSubstitute, fixture lifecycle, and readiness checks.
- Remove test infrastructure from production packages.
- Publish all packable projects structurally from the solution.

## Implementation plan

The delivery used package-by-package extraction followed by a workflow update that packs every
eligible solution project. DreamTravel then dogfooded the packages before the publish workflow was
closed.

## Acceptance criteria

- Production packages do not carry test infrastructure.
- Each external boundary has a focused companion fixture.
- Container reuse and readiness behavior are shared.
- Every companion package is packed and published.
- SQL fixtures support both selected engines and multiple provisioning strategies.
- HTTP fakes preserve compile-time client method checks.
- Readiness waits for a usable service rather than only an open port.
- Warm container reuse materially reduces repeated component-test time.

## Expected consequences

### Positive

- Test-only dependencies leave runtime assemblies.
- New applications compose component-test infrastructure from NuGet rather than copying fixtures.
- Database engines and schema provisioning remain extensible without tying the fixture to an ORM.
- Reuse and readiness improvements become available to every consumer.

### Negative

- SQL fixture consumers must add a companion package reference.
- Moving Faker to HTTP.Testing requires package and namespace changes.
- Seven packages add versioning and publishing responsibilities.
- Container-backed behavior is expensive to verify in the ordinary core CI lane.

## Completion summary

All seven testing packages and the dynamic publish workflow shipped. The foundation delivered
AutoFixture/NSubstitute data attributes, optional Bogus customization, retry polling, container
lifecycle, and in-memory log assertions. SQL.Testing delivered two engines, dacpac/EF/script
provisioning, and reset; HTTP.Testing delivered the WireMock DSL; the API, Redis, Blob, and Service
Bus companions delivered their focused fixtures and helpers.

DreamTravel adopted the packages and its warm component suite was historically recorded as roughly
four times faster. The obsolete Faker project and duplicated local retry helper were removed.

Current architecture lives
in [`../architecture/testing.md`](../architecture/testing.md) and
[`../architecture/package-release.md`](../architecture/package-release.md).

## Deviations

- Planned `BlobStorage.Testing` shipped as `Blob.Testing`.
- The historical Testcontainers version and an unused shared SQL seam were superseded.
- The publish step remained marked open after the workflow had already shipped.
- `AutoNSubstituteData` shipped instead of the originally proposed `AutoMoqData`; Bogus remained an
	optional complement rather than a replacement.
- A dedicated container-reuse helper was dropped because assembly-level setup already provided
	within-run reuse.
- Faker was deleted rather than retained as an obsolete shim because the only tracked consumer was
	migrated before 1.0.
- `FakeApiBase` became non-generic and request matching used direct calls instead of reflection.
- Redis reset required administrative commands to be enabled.
- The Service Bus container managed its own SQL sidecar, so the planned shared-SQL seam was never
	used and was removed.
- TaleCode had no tracked source available for migration; DreamTravel was the only dogfood target.

## Follow-ups

- Add dedicated tests for companion packages when fixture behavior changes materially.
- Keep test-only package dependencies out of runtime projects.
- Add another broker companion only after a real consumer establishes its lifecycle requirements.
