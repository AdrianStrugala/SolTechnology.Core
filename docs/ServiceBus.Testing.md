# SolTechnology.Core.ServiceBus.Testing

Integration-testing fixture for apps backed by Azure Service Bus (e.g. `SolTechnology.Core.MessageBus`
consumers): a [Testcontainers](https://dotnet.testcontainers.org/)-backed `ServiceBusFixture` that runs
the official **Azure Service Bus emulator** and hands back its connection string.

> Reference from test projects only — not needed at runtime in production assemblies.

## Why it needs SQL

The Azure Service Bus emulator is the real broker engine packaged for local use; it **persists entity
metadata and message state in SQL Server** — there is no in-memory mode (that's a Microsoft constraint,
not ours). `ServiceBusFixture` creates the emulator's MSSQL sidecar and the network they share, hands them
to `Testcontainers.ServiceBus` through `WithMsSqlContainer`, and removes all three on dispose; disposing
the emulator container alone would leave the sidecar running. Attaching any other network or
`DependsOn` makes the 4.x emulator's `UnsafeCreateAsync` throw *"Sequence contains more than one element"*.
The package does **not** depend on `SolTechnology.Core.SQL.Testing` or share its SQL container, because the
emulator needs a concrete `MsSqlContainer` — see [Container lifetime & reuse](#container-lifetime--reuse).

## What's in the box

| Member | Purpose |
|---|---|
| `ServiceBusFixture` | Runs the emulator and its MSSQL sidecar, gated on an AMQP readiness probe, with stable-name reuse; removes both and their network on dispose. |
| `ConnectionString` | Emulator connection string (whether created fresh or reused by name). |
| `ServiceBusInstanceBuilder` | The lower-level `ServiceBusBuilder` factory (custom image / topology config). |
| `servicebus-emulator-config.json` | Bundled default topology (one queue + one topic/subscription on `sbemulatorns`). Override via the `configFilePath` ctor arg. |

## Usage

```csharp
// Assembly-level [OneTimeSetUp] — the fixture creates the MSSQL sidecar itself, so there is nothing to wire:
ServiceBusFixture = new ServiceBusFixture();
await ServiceBusFixture.InitializeAsync();          // boots emulator + sidecar, gated on the AMQP probe

var configuration = new TestConfigurationBuilder()
    .AddJsonFile("appsettings.tests.json")
    .Override("ServiceBus:ConnectionString", ServiceBusFixture.ConnectionString)
    .Build();

// Assembly-level [OneTimeTearDown]
await ServiceBusFixture.DisposeAsync();             // removes emulator, sidecar, network; no-op when TESTCONTAINERS_REUSE=true
```

### Custom topology

```csharp
new ServiceBusFixture(configFilePath: "servicebus-emulator-config.json");   // your own queues/topics
```

### Multiple emulators

```csharp
new ServiceBusFixture(instanceName: "orders");
new ServiceBusFixture(instanceName: "payments");
```

## Container lifetime & reuse

The most lifetime-sensitive fixture of the set:

- **Within a run** — boot once in `[OneTimeSetUp]`; every test reuses the same emulator. Each fixture
  instance owns its emulator: two instances with the same name do not share a connection string, and
  disposing one leaves the other working.
- **Across runs** — set `TESTCONTAINERS_REUSE=true`. Reuse is managed **manually via Docker.DotNet and
  stable names**, not Testcontainers' reuse hash (which is unstable here because the emulator holds a
  reference to its MSSQL sidecar). On the first run the emulator `{name}`, its sidecar `{name}-mssql` and
  their network `{name}-network` are created; on later runs they are detected by name, started if stopped,
  and the mapped port is re-read. `DisposeAsync()` is a no-op when reuse is on.
- **Clean-up** — `DisposeAsync()` removes the emulator, the sidecar and the network, with or without Ryuk
  (see `SolTechnology.Core.Testing`'s Ryuk section).
- **Readiness** — every start (fresh or reused) is gated on an **AMQP SASL-echo probe**
  (`ContainerLifecycleHelper.WaitForAmqpReadyAsync`). TCP-accept alone is insufficient — it surfaces as a
  `NullReferenceException` in `AmqpTransportInitiator`.

The emulator persists to its **own** MSSQL sidecar, fully isolated from any `SolTechnology.Core.SQL.Testing`
container in your suite — a between-test `SQLReset` runs against your application database and can never
touch the emulator's tables.

