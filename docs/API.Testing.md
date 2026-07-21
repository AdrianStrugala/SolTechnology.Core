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

Derive your component-test host from `APIFixture<TEntryPoint>`, point `TEntryPoint` at your API's
`Program`, and resolve an `HttpClient` through the auth-client helpers to drive requests end to end.

See the companion foundation package [`SolTechnology.Core.Testing`](Testing.md) for the AutoFixture
data attributes, Testcontainers lifetime model, and log assertions these fixtures compose with.
