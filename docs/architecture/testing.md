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

Container reuse is opt-in through `TESTCONTAINERS_REUSE`. Reused containers use stable names and
networks, restart when stopped, and are not disposed at the end of a run. Ryuk is disabled to
support Docker Desktop Enhanced Container Isolation. Fixtures use protocol-level readiness
checks instead of treating an open TCP port as readiness.

SQL testing supports MSSQL and Postgres with dacpac, delegate, or script provisioning. HTTP
testing uses WireMock. Redis, Azurite, and Service Bus use focused fixtures.

This split keeps test setup composable, centralizes lifecycle behavior, and prevents repeated
hand-written fixtures across modules.
