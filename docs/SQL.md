## SolTechnology.Core.SQL

SQL Server connections, transactions, and schema deployment for Dapper and Entity Framework consumers.
`ISQLConnectionFactory` opens connections with a transient-error retry, `IUnitOfWork` groups work in one ambient
transaction, `SqlErrorTranslator` turns SQL errors into typed `Result` failures, and `SQLProjectDeployer` deploys a
`.dacpac` without silent data loss.

### Features

- **Connections with a transient retry** — a refused, unreachable, or timed-out server, deadlocks, and Azure SQL
  throttling are retried three times; any other error, such as a wrong password, fails at once.
- **Ambient transactions** — `IUnitOfWork.Begin()` opens a `TransactionScope` that flows across `await`, and
  connections opened inside it enlist.
- **Typed SQL errors** — duplicate keys, deadlocks, and timeouts become `ConflictError`, `DeadlockError`, and
  `TimeoutError` with fixed descriptions that never carry table, constraint, or value names.
- **Fail-fast configuration** — an empty `ConnectionString` stops the host at start.
- **Health check** — `AddSolSqlHealthCheck()` runs `SELECT 1` on a fresh connection, without the retry.
- **Safe DACPAC deployment** — `SQLProjectDeployer.DeployDacpac` creates or upgrades the database, fails instead of
  losing data, and logs through `ILogger`.
- **Test fixture** — `SolTechnology.Core.SQL.Testing` runs SQL Server or PostgreSQL in a container for component tests.

### Registration

Register the module and its health check (DreamTravel's API host):

```csharp
var sqlConfiguration = builder.Configuration.GetSection("Sql").Get<SQLConfiguration>()!;
builder.Services.AddSolSQL(sqlConfiguration);
builder.Services.AddHealthChecks().AddSolSqlHealthCheck();
```

### Configuration

| `SQLConfiguration` | Default | Purpose |
|---|---|---|
| `ConnectionString` | — (required) | SQL Server connection string for every connection the module opens; an empty or whitespace value fails at host start. |

Bind it from the `Sql` section, and keep the password in an environment variable (`Sql__ConnectionString`) or a
secret store rather than in `appsettings.json`:

```json
{
  "Sql": {
    "ConnectionString": "Data Source=localhost,1433;Initial Catalog=DreamTravelDatabase;User ID=app;TrustServerCertificate=True"
  }
}
```

SQL logins work without extra packages. Microsoft Entra ID authentication (`Authentication=Active Directory ...`) needs
`Microsoft.Data.SqlClient.Extensions.Azure`, which you install in your own project.

### Usage

#### Repositories with Dapper

Inject `ISQLConnectionFactory`, dispose every connection, and wrap the call in `SqlErrorTranslator.Execute`:

```csharp
public class CityRepository(ISQLConnectionFactory connectionFactory)
{
    public Task<Result> Add(string name) =>
        SqlErrorTranslator.Execute(async () =>
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("INSERT INTO dbo.City (Name) VALUES (@name)", new { name });
        });

    public Task<Result<int>> Count() =>
        SqlErrorTranslator.Execute(async () =>
        {
            using var connection = connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.City");
        });
}
```

`CreateConnection()` returns an open connection:

| Failure while opening | Behaviour |
|---|---|
| `SqlException` with a transient error number: the list of EF Core's `SqlServerTransientExceptionDetector` (for example `233`, `1205`, `4060`, `10053`, `10054`, `10060`, `11001`, `40501`, `40613`) plus `-2`, `53`, `258` (connect timeout), and `10061` (connection refused) | Retried three times after 3, 9, and 27 seconds plus up to 1 second of jitter; the calling thread waits. |
| Any other exception, for example login failure `18456` | Thrown at once; the connection is disposed. |

Entity Framework uses the same connection string through `GetConnectionString()` or the configuration object:

```csharp
services.AddDbContext<DreamTripsDbContext>(options => options.UseSqlServer(sqlConfiguration.ConnectionString));
```

#### Error translation

`SqlErrorTranslator.Execute` returns `Result.Success` or a typed failure for the SQL errors below:

| SQL error number | Error type | `Recoverable` | `Description` |
|---|---|---|---|
| `2627`, `2601` | `ConflictError` | No | A row with the same unique key already exists. |
| `1205` | `DeadlockError` | Yes | The transaction was chosen as a deadlock victim; retry the operation. |
| `-2` | `TimeoutError` | Yes | The SQL command did not finish within its timeout. |

Any other exception propagates, including a `SqlException` with another number (for example `208` invalid object or
`547` foreign key), so the API exception filter logs it with its stack trace. `SqlErrorTranslator.Translate(ex)` called
directly maps such a number to a plain `Error` with the description "SQL Server returned error number N."

#### Transactions

`Begin()` returns the unit of work, so `using` rolls back everything that was not completed:

```csharp
public class CityImporter(IUnitOfWork unitOfWork, CityRepository cities)
{
    public async Task<Result> Import(string first, string second)
    {
        using var transaction = unitOfWork.Begin();

        var added = await cities.Add(first);
        if (added.IsFailure)
        {
            return added;
        }

        added = await cities.Add(second);
        if (added.IsFailure)
        {
            return added;
        }

        transaction.Complete();
        return Result.Success();
    }
}
```

| Call | Behaviour |
|---|---|
| `Begin(isolationLevel)` | Opens the scope (default `ReadCommitted`); throws `InvalidOperationException` when one is already open. |
| `Complete()` | Commits and closes the scope; throws `InvalidOperationException` before `Begin()`. |
| `Rollback()` or `Dispose()` | Rolls back an open scope; does nothing when no scope is open. |

#### DACPAC deployment

`DeployDacpac` creates the database when it is missing and upgrades it in place otherwise (DreamTravel's Aspire host):

```csharp
var logger = loggerFactory.CreateLogger("DreamTravel.DacpacDeployment");
await SQLProjectDeployer.DeployDacpac(dacpacPath, connectionString, "DreamTravelDatabase", logger, ct: ct);
```

Pass `DacDeployOptions` to replace the safe defaults, for example to accept data loss in a throwaway environment:

```csharp
await SQLProjectDeployer.DeployDacpac(
    dacpacPath, connectionString, "DreamTravelDatabase", logger,
    new DacDeployOptions { BlockOnPossibleDataLoss = false }, ct);
```

| Situation | Without `deployOptions` |
|---|---|
| Database missing | Created from the package. |
| Database exists | Upgraded in place; it is never dropped and recreated. |
| Object missing from the package | Kept. |
| Change could lose data | `DacServicesException`; the data stays. |
| `.dacpac` file missing | `FileNotFoundException`. |

The deployer logs `Deploying dacpac [{DacpacPath}] to database [{DatabaseName}]` and `Database [{DatabaseName}] deployed`
at `Information`, DacFx messages at their own level, and progress at `Debug`.

#### Health check

`AddSolSqlHealthCheck()` opens its own connection, so a dead server is reported within the timeout instead of after the
retry delays:

| Aspect | Behaviour |
|---|---|
| Reachable | `Healthy` |
| Unreachable or timed out | `Unhealthy`, or the `failureStatus` you pass |
| Caller cancellation | Rethrown, not reported as `Unhealthy` |
| Timeout | 5 seconds per call, configurable with `timeout` |

### Testing

The companion package `SolTechnology.Core.SQL.Testing` provides `SQLFixture`, a Testcontainers database for component
tests that hands back a connection string, so Dapper and Entity Framework tests use it alike:

```csharp
var fixture = new SQLFixture("DreamTravelDatabase")
    .WithSQLProject("../../../src/Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj");
await fixture.InitializeAsync();

services.AddSolSQL(new SQLConfiguration { ConnectionString = fixture.DatabaseConnectionString });

// ... run tests ...

await fixture.ResetAsync();
await fixture.DisposeAsync();
```

| Method | Effect |
|---|---|
| `.UsePostgres()` | Use PostgreSQL instead of the default SQL Server. |
| `.WithSQLProject(path)` | Build a `.sqlproj` and deploy its dacpac (SQL Server only). |
| `.WithScripts(paths...)` | Run `.sql` script files in order. |
| `.WithEfMigrations(cs => ...)` | Run your Entity Framework migrations; the package does not reference Entity Framework. |
| `.WithSchema((cs, ct) => ...)` | Run any custom provisioning. |
| `.WithNetwork(network, alias)` | Attach the container to a shared Docker network. |
| `ResetAsync()` | Delete test data with Respawn and keep the schema. |

Reference the package from test projects only. Container lifetime and `TESTCONTAINERS_REUSE` follow
[theQuality.md → Container lifetime & reuse](theQuality.md#container-lifetime--reuse); see also
[SQL.Testing.md](SQL.Testing.md).

### Conventions

- Return `Result` or `Result<T>` from repositories, and wrap database calls in `SqlErrorTranslator.Execute`.
- Return domain types, not Entity Framework entities or driver types.
- Name methods by intent (`GetById`, `SaveTrip`), with one repository per aggregate.
- Dispose every connection from `CreateConnection()` with `using`.
- Keep one connection open at a time inside a unit of work; a second open connection promotes the transaction to a
  distributed one, which needs MSDTC on Windows.
- Inject `ISQLConnectionFactory` and `IUnitOfWork`; their implementations are internal.
- Opt out of the DACPAC data-loss block per deployment, never by default.

### What ships in DI

| Service | Lifetime | Notes |
|---|---|---|
| `IOptions<SQLConfiguration>` | Singleton | `ConnectionString` is validated at host start. |
| `ISQLConnectionFactory` | Transient | Opens connections with the transient retry. |
| `IUnitOfWork` | Transient | Each consumer gets its own transaction scope holder. |
