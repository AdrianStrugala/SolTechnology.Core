# SolTechnology.Core.API.Testing

Integration-testing fixtures for `SolTechnology.Core.Api` consumers. **Reference from test projects
only** — never from production assemblies.

> Part of the testing framework defined in
> [testing architecture](architecture/testing.md). For the overall testing strategy see
> [theQuality.md](theQuality.md); for the shared foundation helpers see [Testing.md](Testing.md).

## What's inside

| Type | Purpose |
|---|---|
| `APIFixture<TEntryPoint>` | Wraps `Microsoft.AspNetCore.Mvc.Testing` / `TestHost` to spin up your API in-memory for component tests. |
| `CreateAuthorizedClient` / `CreateAnonymousClient` | Auth-client helpers for exercising secured and public endpoints. |
| `TestConfigurationBuilder` | The `appsettings` + in-memory override pattern for test configuration. |

## Usage

Create one `APIFixture<TEntryPoint>` per test assembly, point `TEntryPoint` at your API's `Program`, and drive
requests through `ServerClient` or the auth-client helpers:

```csharp
// Assembly-level [OneTimeSetUp]
var configuration = new TestConfigurationBuilder()
    .AddJsonFile("appsettings.tests.json")
    .Override("Sql:ConnectionString", sqlFixture.DatabaseConnectionString)
    .Build();
ApiFixture = new APIFixture<Program>(configuration, services => services.AddSingleton(TimeProvider.System));

// In a test
var response = await ApiFixture.ServerClient.GetAsync("/health");
using var admin = ApiFixture.CreateAuthorizedClient("Bearer", "test-token");
using var anonymous = ApiFixture.CreateAnonymousClient();

// Assembly-level [OneTimeTearDown] — stops the host; every IHostedService.StopAsync runs.
ApiFixture.Dispose();
```

`Dispose()` disposes the underlying `WebApplicationFactory`, so the in-memory host, its hosted services and
background workers stop with the fixture instead of running until the test process exits.

See the companion foundation package [`SolTechnology.Core.Testing`](Testing.md) for the AutoFixture
data attributes, Testcontainers lifetime model, and log assertions these fixtures compose with.
