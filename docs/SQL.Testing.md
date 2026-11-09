# SolTechnology.Core.SQL.Testing

Integration-testing fixtures for `SolTechnology.Core.SQL` consumers. **Reference from test projects
only** — never from production assemblies.

> Part of the testing framework defined in
> [testing architecture](architecture/testing.md). For the overall testing strategy see
> [theQuality.md](theQuality.md); for the shared foundation helpers see [Testing.md](Testing.md).

## What's inside

| Type | Purpose |
|---|---|
| `SQLFixture` | A Testcontainers-backed database fixture for **MSSQL** (dacpac) and **PostgreSQL**. |
| Schema provisioning | Pluggable strategies — dacpac, raw scripts, or a delegate (e.g. EF migrations). |
| Respawn reset | Between-test data reset via [Respawn](https://github.com/jbogard/Respawn). |

ORM-agnostic by design: serves **Dapper** and **Entity Framework** alike.

## Usage

Spin up `SQLFixture` once per test assembly, choose a schema-provisioning strategy, and let Respawn reset state
between tests:

```csharp
// Assembly-level [OneTimeSetUp]
SqlFixture = new SQLFixture("OrdersDb").WithScripts("schema.sql");   // or WithSQLProject / WithEfMigrations / UsePostgres
await SqlFixture.Initialize();
var connectionString = SqlFixture.DatabaseConnectionString;

// Per-test teardown — deletes the data, keeps the schema
await SqlFixture.Reset();

// Assembly-level [OneTimeTearDown]
await SqlFixture.DisposeAsync();   // no-op when TESTCONTAINERS_REUSE=true
```

[SQL.md → Testing](SQL.md#testing) lists every provisioning option. See the companion foundation package
[`SolTechnology.Core.Testing`](Testing.md) for the shared Testcontainers lifetime/reuse model and Ryuk settings these
fixtures build on.
