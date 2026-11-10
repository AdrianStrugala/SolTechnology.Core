# Naming and Public API

Source identifiers capitalize acronyms: `API`, `HTTP`, `SQL`, `AUID`, and `CQRS`. Apply this
rule to namespaces, types, members, and local identifiers when the identifier is not constrained
by an existing compatibility contract.

Published package and assembly identities remain stable even when their casing differs from
source identifiers. For example, the package ID remains `SolTechnology.Core.Api` while its
namespace is `SolTechnology.Core.API`. Consumer compatibility takes precedence over cosmetic
uniformity.

Public registration and middleware extensions use the branded prefixes `AddSol*`, `UseSol*`,
and `MapSol*`. Examples include `AddSolCQRS`, `AddSolAuth`, `AddSolVersioning`,
`UseSolLogging`, and `MapSolHealthChecks`. Pre-1.0 names have no compatibility forwarders.

The convention makes domain abbreviations recognizable and reduces extension-method collisions
in IntelliSense. Public and protected symbols remain compatibility contracts: rename, move, or
delete them only after explicit maintainer confirmation and with the appropriate semver impact.

Project-owned asynchronous methods carry no `Async` suffix, because the `Task` return type already
says so; the suffix stays only where a synchronous overload needs disambiguating or the name is
imposed by a framework or external API (`DisposeAsync`, `InvokeAsync`, `StartAsync`, `StopAsync`,
`CheckHealthAsync`, `OnResultExecutionAsync`). 2.0 removed it from the HTTP request verbs, the Cache
lock and idempotency contracts, the Tale repository and the testing-kit fixtures. It stays on
persisted names, such as the Hangfire job method `RecurringJobRunner<TJob>.RunAsync`, and on the
`SolTechnology.Core.SQL.Testing` provisioner and engine contracts, which 2.0 did not rename. Async
contracts take a `CancellationToken`.

The public surface holds only what a consumer calls, implements, or configures. Implementation
types registered behind an interface are `internal` (for example the MessageBus broker, publisher
and receiver, the SQL connection factory and unit of work, the Blob connection factory, and the
Swagger options configurator), so they can change without a major version. New public types are
added only when a consumer needs them, such as `TaleJsonOptions.Default` for custom Tale
repositories.

Current exceptions are documented by the code rather than normalized speculatively. Do not infer
that a package ID or legacy public symbol should be renamed solely to satisfy acronym casing.
