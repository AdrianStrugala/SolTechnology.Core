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
takes no `CancellationToken`. Opening a connection is not a SqlClient command, so this wait shows
as a gap in the parent [Chronicle](logging.md#chronicle) step rather than as a `SQL` step.

SQL itself has no Chronicle code and no Logging dependency: Logging observes SqlClient's
`SqlClientDiagnosticListener` and records each command inside a root as `SQL {keyword}` (leading
comments and `SET …;` statements skipped, stored procedures by name), from sending the command to the
first result. Commands through Dapper, EF Core and raw `SqlConnection` are covered alike, and a
connection wrapper with an SQL → Logging edge was rejected because it would miss EF Core and break
provider-specific casts. Statement text and SqlClient error messages are recorded only with
`CaptureSQLCommandText`, because they can carry literal values.
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

## Blob

`SolTechnology.Core.Blob` stores typed objects as Azure blobs in JSON or Avro and records the
format in the blob's metadata. Usage is in [`docs/Blob.md`](../Blob.md); registration lives in
[`ModuleInstaller.cs`](../../src/SolTechnology.Core.Blob/ModuleInstaller.cs).

### Public surface

Consumers depend on `IBlobConnectionFactory`, `BlobConfiguration`, `SerializationFormat`, and the
`ReadFromBlob<T>` and `WriteToBlob` extensions on `BlobContainerClient`. Everything lives in the
`SolTechnology.Core.Blob` namespace: the extensions used to sit in the SDK's `Azure.Storage.Blobs`
namespace, where they looked like part of the SDK and appeared in every file that imports it.
`BlobConnectionFactory` is internal, so the cache can change without a major version.
Every read and write takes a `CancellationToken`. `ReadFromBlob<T>` stays a classic `this` method
because an `extension` block loses `T`'s nullable annotation (Coding Guide §9.7).

### Configuration validation

`AddSolBlob` throws `ArgumentException` for a null configuration and registers
`BlobConfiguration` with `ValidateOnStart`. The connection string must be non-empty and parsable:
the validator constructs a `BlobServiceClient`, which parses the string without sending a request,
and treats `FormatException` as invalid. A wrong value therefore stops the host at start instead of
failing at the first `GetConnection` (Coding Guide §14). The messages never echo the value, because
it usually carries an account key or a SAS token.

### Container cache

The singleton factory keeps a `ConcurrentDictionary` of `Lazy<Task<BlobContainerClient>>` per
container name, so concurrent first calls create a container once, with
`CreateIfNotExistsAsync`, and share one client. A failed creation is evicted, so a transient
failure is retried on the next call instead of being cached. `GetConnection` stays synchronous and
blocks on the first call per name, because an async accessor would change `IBlobConnectionFactory`.

A container can disappear after it was cached, for example through `AzuriteFixture.Clear` or
an operator. `WriteToBlob` catches `ContainerNotFound`, creates the container through the same
client, and retries the upload once; the cached client is a stateless handle, so the factory does
not need to know. Reads do not recreate: a blob in a deleted container cannot exist, so they fail
with 404.

### Writes, reads, and metadata

The format travels as the `SerializationFormat` metadata key (`json` or `avro`) in the
`BlobUploadOptions` of the single upload, so no reader sees content without its format marker and a
crash cannot leave one behind. An overwriting upload replaces all metadata, so `WriteToBlob` first
reads the blob's metadata with one `GetPropertiesAsync`, starts from empty metadata on a 404 (blob
or container missing), and uploads the merged keys. Two writers of the same blob still race: the
last upload wins, and a key another writer added between the read and the upload is lost. An
undefined `SerializationFormat` throws `ArgumentOutOfRangeException` before any request.

`ReadFromBlob<T>` downloads content and metadata in one buffered `DownloadContentAsync` response and
reads a blob without the key as JSON. The key name and values are those of 1.x, so blobs written by
1.x, which set the metadata in a second call, still read. Avro uses AvroConvert with the Brotli
codec.

### Tests

No test project references Blob (accepted risk AR-1): no automated test covers the container cache,
the metadata merge, the start-time validation, or the DI registration. The behaviour was verified
manually against Azurite through `SolTechnology.Core.Blob.Testing`. `AzuriteFixture` pins
`mcr.microsoft.com/azure-storage/azurite:3.37.0`, the first image that accepts service version
`2026-06-06` of `Azure.Storage.Blobs` 12.29.0; older images answer every request with 400
`InvalidHeaderValue`, so the pin moves only forward with the SDK.

## MessageBus

`SolTechnology.Core.MessageBus` publishes typed messages to Azure Service Bus queues and topics and
hands received messages to scoped `IMessageHandler<T>` implementations. Usage is in
[`docs/Bus.md`](../Bus.md); registration lives in
[`ModuleInstaller.cs`](../../src/SolTechnology.Core.MessageBus/ModuleInstaller.cs).

### Public surface

Consumers depend on `MessageBusConfiguration`, `QueueConfiguration`, `IMessage`,
`IMessagePublisher`, `IMessageHandler<T>`, the `AddSolMessageBus` and `With*` extensions, and
`AddSolServiceBusHealthCheck`. The broker, publisher, receiver, settler, and provisioning types are
internal, so their design can change without a major version. The generic `With*` methods live in an
`extension` block; ApiCompat reports CP0021 "notnull" on them, which the `IMessage` constraint
already implies (Coding Guide §9.7).

### Registration and the shared client

`AddSolMessageBus` registers `MessageBusConfiguration` with `ValidateOnStart`: an empty connection
string or a `MaxDeliveryCount` of zero or less stops the host. The module owns one
`ServiceBusClient`, registered under its own key, so an unkeyed client that the application
registers for something else is neither reused nor disposed by the module; the health check and
the broker resolve the keyed client. The broker is registered with a factory lambda, which keeps
its constructor internal and adds no public surface that would have to be removed again. The
correlation service from `SolTechnology.Core.Logging` is registered with the module, because the
publisher and receiver carry the correlation id in the `CorrelationId` application property.

`With*` calls only record endpoints in a registry. The broker turns them into senders and
processors on the first `EnsureInitializedAsync`, called by the receiver at host start and by the
publisher before the first send. It provisions every entity first, then builds all senders and
processors into local collections and keeps them only when every one was created, so a failed
initialisation leaves nothing behind and the next call retries cleanly instead of adding a second
sender. Queue lookup by message type uses the class name without namespace, so renaming a message
namespace does not move its queue.

### Provisioning

With `CreateResources` on, the broker creates missing queues, topics, and subscriptions through
`ServiceBusAdministrationClient`, which needs the *Manage* claim. Created queues and subscriptions
get `MaxDeliveryCount`, so the broker and the receiver agree on the last attempt. Provisioning is
check-then-create, and an entity that another instance creates in between (`MessagingEntityAlreadyExists`)
counts as created and is logged at Debug, so instances that start at once do not fail. Existing
entities are never changed. With `CreateResources` off, nothing is provisioned, and the entities
must exist.

### Receiving and settlement

`MessageBusReceiver` is a hosted service that starts one processor per receiver with
`AutoCompleteMessages = false` in peek-lock mode, so the module owns every settlement decision.
For each message it restores the correlation id, deserializes the JSON body, and calls the handler in
a new DI scope with the processor's cancellation token:

- an empty, invalid, or `null` body is dead-lettered at once (`EmptyBody`, `DeserializationFailed`,
  `NullPayload`), because retrying cannot fix it;
- a handler that returns completes the message;
- a handler that throws abandons the message, or dead-letters it with reason
  `HandlerFailedMaxDelivery` and the exception's message once the delivery count reaches
  `MaxDeliveryCount`. The value is configured rather than read from the entity because reading it
  needs the *Manage* claim;
- completion runs outside the handler's error path, so a completion failure, such as a lost lock,
  is logged at Warning and never turns a handled message into an abandon or dead-letter.

Settlement calls never use the processor's cancellation token. The SDK cancels that token as soon
as `StopProcessingAsync` starts, and a handler that still returns during shutdown must be able to
complete its message; otherwise every rolling deploy would hand in-flight messages to another
instance a second time. A handler that throws `OperationCanceledException` during shutdown is not
settled, and its message is delivered again. A failed complete, or a failed abandon or dead-letter
after a handler failure, is logged at Warning and left to the lock expiry.

Each message is its own [Chronicle](logging.md#chronicle) root, `Message {MessageType}`, opened with
`BeginRoot` after the correlation id is restored and inside the receiver's log scope, so its 2154
entry carries `CorrelationId` and `MessageId`. `MessageSettler` returns the internal
`MessageSettlement` it chose, and the root takes its status from it: completed is `Ok` (with
`Settlement=CompleteFailed` when the broker refused the completion), an unreadable or failing
message is `Failed` with `Settlement` and the dead-letter reason code (never the description, which
can hold exception text), and a shutdown cancellation is `Cancelled`. The body and application
properties are never recorded.

### Several instances

Every instance that registers a receiver competes for the same queue or subscription, so each
message is handled by one instance; fan-out to every instance needs a subscription per instance,
which the module does not create. Delivery is at least once: a lock expiry, a crash, or a failed
settlement delivers the message again, possibly to another instance, so handlers must be
idempotent. There is no ordering across instances or with `MaxConcurrentCalls` above `1`.

### Tests

`tests/SolTechnology.Core.MessageBus.Tests` runs the publish, receive, settlement, shutdown, and log
template tests against the Service Bus emulator through `SolTechnology.Core.ServiceBus.Testing`,
started once on first use with the queues in `servicebus-emulator-config.json`. The emulator has no
administration API, so provisioning, concurrent creation, and initialisation retry are tested with a
substituted `ServiceBusAdministrationClient`, and the completion-failure path with a processor whose
messages the test delivers itself.
