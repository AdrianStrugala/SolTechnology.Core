# SolTechnology.Core.HTTP.Testing

Integration-testing fixtures for `SolTechnology.Core.HTTP` consumers: a [WireMock.Net](https://github.com/WireMock-Net/WireMock.Net)-backed
`WireMockFixture` and a fluent fake-API DSL keyed off your generated HTTP client interfaces.

> Reference from test projects only — not needed at runtime in production assemblies.
> **Migrated from `SolTechnology.Core.Faker`** (namespace `SolTechnology.Core.Faker` →
> `SolTechnology.Core.HTTP.Testing`) — an accepted breaking change. See ADR-008.

## What's in the box

| Type | Purpose |
|---|---|
| `WireMockFixture` | Boots a WireMock server (dynamic port), registers fake APIs, hands out the `Fake<T>` DSL, exposes `Url`/`Port`/`LogEntries`, resets/stops the server. |
| `FakeApiBase` | Base class for a fake API. Derive once and implement the client interface — `: FakeApiBase, IYourClient`. |
| `IFakeApi` | Marker/registration contract consumed by the fixture. |
| `IFakeApiBuilderWithRequest<T>` / `IFakeApiBuilderWithResponse` | The fluent request/response builder seam. |
| `JsonResponseBuilderDecorator` | `IResponseBuilder` decorator adding `WithBodyAsJson` ergonomics. |

## Usage

```csharp
// 1. One-time setup (assembly-level [OneTimeSetUp]).
var wireMock = new WireMockFixture();
wireMock.Initialize();                       // dynamic port (0) by default — never collides in parallel suites
wireMock.RegisterFakeApi(new GoogleFakeApi());

// Wire your HTTP client to the fake server's actual address (read it after Initialize):
configuration["HTTPClients:Google:BaseAddress"] = $"{wireMock.Url}/google/";

// 2. In a test — arrange via a DIRECT method call: full IntelliSense + compile-time argument checks.
wireMock.Fake<IGoogleHTTPClient>()
    .WithRequest(x => x.GetLocationOfCity(cityName))
    .WithResponse(r => r.WithSuccess().WithBodyAsJson(new { name = "Wrocław" }));

// Optional: assert the client actually called the fake.
Assert.That(wireMock.LogEntries, Is.Not.Empty);

// 3a. Between tests (per-test teardown) — clears mappings + recorded requests, keeps the server up.
wireMock.Reset();

// 3b. End of the fixture's life (one-time teardown) — stops the server and frees the port.
wireMock.Dispose();
```

A fake API derives from `FakeApiBase` and implements the client interface — the interface is named
**once** (`FakeApiBase` is not generic). `WithRequest` invokes the method directly (no reflection), so
the method body sets up the matcher:

```csharp
public class GoogleFakeApi : FakeApiBase, IGoogleHTTPClient
{
    protected override string BaseUrl => "google";   // relative path segment matched on the server

    public Task<City> GetLocationOfCity(string cityName)
    {
        var request = Request.Create().UsingGet()
            .WithPath(new WildcardMatcher($"/{BaseUrl}/maps/api/geocode/json"))
            .WithParam("address", cityName);
        Provider = BuildRequest(request);
        return default!;
    }
}
```

## Notes for large / parallel suites

- **Dynamic port by default.** `Initialize()` binds port `0`; read `Url`/`Port` to wire clients. Pass a
  fixed port only when a consumer hard-codes the address.
- **Admin interface off, no console logging** by default (quiet under thousands of requests). Override by
  passing your own `WireMockServerSettings` to `WireMockStartup.Run(settings)`.
- **`Reset()` vs `Dispose()`.** `Reset()` is the between-test cleanup (mappings + recorded requests);
  `Dispose()` stops the server and frees the port — call it once at the end.
- **One fixture per test assembly**, arranged sequentially. The arrangement DSL (`Fake<T>().WithRequest(...)`)
  is not designed for concurrent arrangement of the *same* fake; keep arrangement on the test thread.


