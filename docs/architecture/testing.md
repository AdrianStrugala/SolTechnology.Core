# Testing

Testing infrastructure is split into a foundation package and focused companion packages so
production assemblies do not carry test-only dependencies:

- `SolTechnology.Core.Testing`
- `SolTechnology.Core.API.Testing`
- `SolTechnology.Core.SQL.Testing`
- `SolTechnology.Core.HTTP.Testing`
- `SolTechnology.Core.Redis.Testing`
- `SolTechnology.Core.Blob.Testing`
- `SolTechnology.Core.ServiceBus.Testing`

The foundation standardizes NUnit, AutoFixture with NSubstitute, optional Bogus, Result
assertions, cancellation helpers, retry polling, log assertions, and shared Testcontainers
lifecycle. Each companion owns the fixture and protocol details for one integration boundary.
Fixture methods carry no `Async` suffix (`Initialize`, `Reset`, `Flush`, `Clear`), because none has a
synchronous overload; only the framework-imposed `DisposeAsync` keeps it.

## Container lifetime

Every fixture keeps its state per instance: two fixtures with the same container name never share a
connection string, and disposing one never breaks the other. Within a run, consumers start a fixture
once from an assembly-level `[SetUpFixture]`.

Container reuse across runs is opt-in through `TESTCONTAINERS_REUSE`. Reused containers use stable
names and networks, restart when stopped, and are not disposed at the end of a run. Without reuse,
disposing a fixture removes what it started.

Ryuk follows the Testcontainers settings and is on by default, so a run aborted before disposal does
not leave containers behind. The kits never switch it off in code; Docker Desktop Enhanced Container
Isolation users set `TESTCONTAINERS_RYUK_DISABLED=true`. With Ryuk off, a resource built with
`WithCleanUp(true)` loses its reaper session id, and Testcontainers then only stops it on dispose: the SQL,
Redis and Azurite fixtures leave stopped containers, while `ServiceBusFixture` (auto-remove plus an
explicit network delete) and `TestContainersContext` (explicit network delete) still remove theirs.

The Service Bus emulator needs an MSSQL sidecar on a shared network. `Testcontainers.ServiceBus`
creates both when it is not given them but never disposes them, so `ServiceBusFixture` builds them
itself through `WithMsSqlContainer` (internal `OwnedEmulator`) and removes the emulator, the sidecar and
the network together, attempting each even when another removal fails. With reuse, the three get the
names `{name}`, `{name}-mssql` and `{name}-network` and are found and restarted by name, because the
Testcontainers reuse hash is unstable for a container that references a sidecar.

Fixtures use protocol-level readiness checks instead of treating an open TCP port as readiness (the
Service Bus emulator is ready only once it echoes the AMQP SASL header).

## Boundaries

SQL testing supports MSSQL and Postgres with dacpac, delegate, or script provisioning. Azurite is
pinned to `azurite:3.37.0`, the first image that accepts the service version of `Azure.Storage.Blobs`
12.29.0; the pin only moves forward.

HTTP testing uses WireMock with strict matching: a request must match every matcher of a stub, or it
gets 404, so a client bug cannot pass by hitting the closest stub. `WireMockFixture.Initialize(settings)`
and `WireMockStartup.Run(settings)` opt back into partial matching. WireMock splits query values on
commas, so a comma-separated value is stubbed with its parts.

`APIFixture` owns its `WebApplicationFactory` and disposes it, which stops the in-memory host and its
hosted services. Test-host creation is contained to approved fixture files by an architecture test.

## DreamTravel

DreamTravel's unit tests and its component tests (SQL Server through `SQLFixture`, WireMock for the
upstream APIs) run in the GitHub Actions job `dreamtravel-tests` on every trigger, and the tag
publish waits for it ([`docs/CICD.md`](../CICD.md#dreamtravel-tests-job)). The paid Google and
Michelin integration tests stay `[Ignore]`d, `DreamTravel.E2E.Tests` is a placeholder and stays
out, and the Azure DevOps pipelines under `sample-tale-code-apps/DreamTravel/devOps/` are kept as a
memento without CI coverage.

The component tests run several hosts in one process: the shared API and Worker, and a short-lived
Production API for the error-handling tests. Hangfire's configuration is process-wide (`AddHangfire`
sets the static log provider and the global filter collections), so the extra host reuses the
shared `JobStorage.Current` instead of configuring its own; otherwise its tear-down leaves Hangfire
logging to a disposed logger factory for every later test.

## Coverage

Every module that registers services has a `ModuleResolutionTests` class that builds its provider
with `ValidateOnBuild` and `ValidateScopes` on and resolves each public service, so a registration
that only fails at runtime fails a test first. Blob is the exception: it has no test project
(accepted risk AR-1 of the 2.0 hardening), and its fixes rest on a Release build, source checks and
manual Azurite runs. The deprecated Guards and Scheduler projects have no resolution test.

CI runs the library tests through `.github/runTests.ps1` after the build. It runs every test project
listed in `SolTechnology.Core.slnx` with `--no-build`, keeps going after a failure, and exits non-zero
when any one failed, so a failure in an early project cannot be hidden by a later green one.

The `*.Testing` packages have no test project of their own (Q6 of the 2.0 hardening). They are
exercised by the module test projects that use them and by the DreamTravel component tests; a few
kit behaviours (strict matching, `APIFixture` disposal, Service Bus fixture lifecycle, network removal)
are pinned in the HTTP, API and MessageBus test projects.

This split keeps test setup composable, centralizes lifecycle behavior, and prevents repeated
hand-written fixtures across modules.
