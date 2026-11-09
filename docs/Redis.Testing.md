# SolTechnology.Core.Redis.Testing

Integration-testing fixture for apps backed by Redis (e.g. `SolTechnology.Core.Cache` Redis consumers):
a [Testcontainers](https://dotnet.testcontainers.org/)-backed `RedisFixture` that boots a Redis
container and hands back the connection details your app already wires.

> Reference from test projects only — not needed at runtime in production assemblies.

## What's in the box

| Member | Purpose |
|---|---|
| `RedisFixture` | Boots a `redis:7-alpine` container (random host port), exposes `HostName` / `ConnectionString`, `Flush()` and honours the shared reuse policy. |
| `HostName` | `host:port` — the value apps bind to `Redis:HostName`. |
| `ConnectionString` | Full StackExchange.Redis connection string. |
| `Flush()` | Clears all keys — the between-test reset when the container is reused. |
| `WithNetwork(network, alias)` | Attach to a docker network to share with other fixtures. |

## Usage

```csharp
// Assembly-level [OneTimeSetUp]
RedisFixture = new RedisFixture();
await RedisFixture.Initialize();

var configuration = new TestConfigurationBuilder()
    .AddJsonFile("appsettings.tests.json")
    .Override("Redis:HostName", RedisFixture.HostName)
    .Override("Redis:Enabled", "true")
    .Build();

// In a per-test teardown, when the container is reused:
await RedisFixture.Flush();

// Assembly-level [OneTimeTearDown]
await RedisFixture.DisposeAsync();   // no-op when TESTCONTAINERS_REUSE=true
```

## Container lifetime & reuse

The fixture defers to `SolTechnology.Core.Testing`'s `TestContainersContext`:

- **Within a run** — boot once in `[OneTimeSetUp]`; every test reuses the same container for free.
- **Across runs** — set `TESTCONTAINERS_REUSE=true` to keep the container alive between runs
  (stable name + `WithReuse(true)`); `DisposeAsync()` becomes a no-op. CI stays hermetic by default; Ryuk removes the containers of an
  aborted run (see [Testing.md](Testing.md#ryuk-resource-reaper)).
- **Between tests** — call `Flush()` to reset state without restarting the container.

