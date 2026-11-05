# Infrastructure modules

The infrastructure packages wrap one external system each behind a small contract that a
`ModuleInstaller` registers. This page describes how they work now and why. Consumer-facing usage
lives in each module's page under [`docs/`](../).

## SQL

`SolTechnology.Core.SQL` opens SQL Server connections, groups work in ambient transactions,
translates known SQL errors into `Result` failures, and deploys `.dacpac` packages. Usage is in
[`docs/SQL.md`](../SQL.md); registration lives in
[`ModuleInstaller.cs`](../../src/SolTechnology.Core.SQL/ModuleInstaller.cs).

### Public surface

Consumers depend on `ISQLConnectionFactory`, `IUnitOfWork`, `SQLConfiguration`,
`SqlErrorTranslator`, `SQLProjectDeployer`, and `AddSolSqlHealthCheck`. `SQLConnectionFactory` and
`UnitOfWork` are internal, so the implementations can change without a major version and tests
reach them through `InternalsVisibleTo`.

### Configuration validation

`AddSolSQL` throws `ArgumentNullException` for a null configuration and registers
`SQLConfiguration` with `ValidateOnStart`. The connection string must be non-empty and parsable by
`SqlConnectionStringBuilder`, so an unknown keyword stops the host at start instead of failing on
the first connection or health probe. The validation message never echoes the value, because the
value usually carries a password.

### Connections and retry

`CreateConnection` opens a `SqlConnection` synchronously and retries a transient `SqlException`
three times after 3, 9, and 27 seconds plus up to one second of jitter. Any other failure, such as
login failure `18456`, is thrown at once, and the connection is disposed whenever opening fails.

The transient set in `SQLTransientErrors` is the list of EF Core 10's
`SqlServerTransientExceptionDetector`, plus four numbers that matter only when opening a connection:
`-2` (timeout), `53` (network path not found), `258` (connect timeout), and `10061` (connection
refused). Every `SqlError` of the exception is checked, as EF Core does. Following EF Core keeps one
widely used definition of "transient" instead of a hand-picked list, and the extra numbers keep a
server restart, failover, or slow container start retried, as it was in 1.x. EF Core leaves `-2` out
because a command may have run; an open has no side effect, so retrying it is safe.

The retry blocks the calling thread for about 40 seconds of delays plus four connect attempts and
takes no `CancellationToken`.
The health check therefore opens its own connection without the retry, with a 5-second default
timeout, and rethrows the caller's cancellation instead of reporting `Unhealthy`.

### Unit of work

`IUnitOfWork.Begin` opens a `TransactionScope` with `TransactionScopeAsyncFlowOption.Enabled`, so
connections opened later in the same async flow enlist, even after an `await` that resumes on
another thread. `Begin` returns the unit of work and the interface extends `IDisposable`, so
`using var transaction = unitOfWork.Begin();` rolls back anything that was not completed.
`Complete` commits and closes the scope; the scope reference is cleared before the scope is
disposed, so a commit that throws (for example `TransactionAbortedException`) still leaves the unit
of work ready for the next `Begin`. `IUnitOfWork` is transient, so each consumer holds its own
scope.

`Begin` lives on the interface because the DI-resolved `IUnitOfWork` was otherwise unusable. An
`IAsyncDisposable` transaction object would be cleaner but would force every caller to restructure
its `using` blocks, and opening the scope in a scoped constructor would tie the transaction to the
DI scope instead of the caller's code.

Two connections open at once inside one scope promote the transaction to a distributed one, which
needs MSDTC on Windows; repositories keep one connection open at a time.

### Error translation

`SqlErrorTranslator.Execute` translates only the SQL errors it names: `2627` and `2601` become
`ConflictError`, `1205` becomes a recoverable `DeadlockError`, and `-2` a recoverable
`TimeoutError`. Each `Description` is a fixed text, so constraint, table, and value names never
leave the data layer. Every other exception, including a `SqlException` with another number,
propagates, so the API exception filter logs it with its stack trace and returns a sanitized
response. Translating unknown numbers would hide the only diagnostic a production failure has.

### DACPAC deployment

`SQLProjectDeployer.DeployDacpac` connects to `master`, creates the database when it is missing,
and otherwise upgrades it in place. Without caller options it uses DacFx defaults plus
`BlockOnPossibleDataLoss = true`: the database and objects missing from the package are never
dropped, and a change that could lose data fails with `DacServicesException`. It logs the start and
the end at `Information`, DacFx messages at their own level, and progress at `Debug`, all through
the caller's `ILogger`.

DacFx 170.x depends on `Microsoft.Data.SqlClient` 6.x and references
`Microsoft.Identity.Client`'s `MsalUiRequiredException` in its retry check. SqlClient 7 no longer
brings MSAL, so `SolTechnology.Core.SQL` and `SolTechnology.Core.SQL.Testing` reference
`Microsoft.Identity.Client` directly; without it a SQL error during deployment surfaces as
`FileNotFoundException`. `SQLPackageDependencyTests` guards the reference. Entra ID authentication
needs `Microsoft.Data.SqlClient.Extensions.Azure`, which the consumer installs; SQL logins need
nothing extra.

### Tests

`tests/SolTechnology.Core.SQL.Tests` starts one SQL Server container on first use through
`SolTechnology.Core.SQL.Testing`; tests that need no database start none. The DACPAC deployer tests
are in category `Dacpac`. The `mssql/server` image is amd64-only, and each deployment takes about
2.5 minutes under emulation on arm64 machines, where
`dotnet test tests/SolTechnology.Core.SQL.Tests --filter TestCategory!=Dacpac` skips them. CI on x64
runs them.
