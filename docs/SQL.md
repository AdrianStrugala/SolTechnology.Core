### Overview

The SolTechnology.Core.SQL library provides minimum functionality needed for SQL db connection. It handles needed services registration and configuration and a result provides IDbConnection.

### Registration

For installing the library, reference **SolTechnology.Core.SQL** nuget package and invoke **AddSQL()** service collection extension method:

```csharp
services.AddSQL();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "Sql": {
      "ConnectionString": "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var sqlConfiguration = new SQLConfiguration
{
    ConnectionString = "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
};
services.AddSQL(sqlConfiguration);
```


### Usage

1) Inject ISQLConnectionFactory

```csharp
     public MatchRepository(ISQLConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }
```

2) Create IDbConnection

```csharp
  using (var connection = _sqlConnectionFactory.CreateConnection())
```

3) Works well with raw SQL and Dapper ORM.

### Testing

The companion package **`SolTechnology.Core.SQL.Testing`** provides `SQLFixture` — a
[Testcontainers](https://dotnet.testcontainers.org/)-backed database fixture for component tests.
Reference it from test projects only.

It is **ORM-agnostic** (it only hands back a connection string, so Dapper and EF consumers are served
identically) and supports both **SQL Server** (default) and **PostgreSQL**, with pluggable schema
provisioning and Respawn-based reset:

```csharp
// SQL Server + dacpac (default, source-compatible with the original in-Sql fixture)
var fixture = new SQLFixture("TaleCodeDatabase")
    .WithSQLProject("../../../src/TaleCodeDatabase/TaleCodeDatabase.csproj");
await fixture.InitializeAsync();

var connectionString = fixture.DatabaseConnectionString;   // wire into "Sql:ConnectionString"

// ... run tests ...

await fixture.ResetAsync();      // between-test reset (Respawn; schema preserved)
await fixture.DisposeAsync();    // no-op when TESTCONTAINERS_REUSE=true
```

Engine + provisioning options (fluent, set before `InitializeAsync`):

| Method | Effect |
|---|---|
| `.UsePostgres()` | Use PostgreSQL instead of the default SQL Server. |
| `.WithSQLProject(path)` | Build + deploy a dacpac from a `.sqlproj` (SQL Server only). |
| `.WithScripts(paths...)` | Execute raw `.sql` script files in order. |
| `.WithEfMigrations(cs => ...)` | Run EF migrations via a delegate — the package stays **EF-free**; you bring your own `DbContext`. |
| `.WithSchema((cs, ct) => ...)` | General provisioning seam (any custom logic). |
| `.WithNetwork(network, alias)` | Attach the container to a shared docker network. |

Notes:

- **Type name is all-caps `SQLFixture`** and the namespace is `SolTechnology.Core.SQL.Testing`
  (per [ADR-001](adr/001-acronym-capitalization-refactoring.md)).
- **DacFx stays in `SolTechnology.Core.SQL` at runtime** (used by `SQLProjectDeployer`); only the
  test-only fixture moved to the companion package.
- Container lifetime / `TESTCONTAINERS_REUSE` reuse is the shared model from
  [theQuality.md → Container lifetime & reuse](theQuality.md#container-lifetime--reuse).

