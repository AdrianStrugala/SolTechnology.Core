# Tale Framework

`SolTechnology.Core.Tale` provides readable, resumable orchestration through
`TaleHandler<TInput, TContext, TOutput>`, `Tale`, `Chapter<TContext>`,
`InteractiveChapter<TContext, TInput>`, and `Context<TInput, TOutput>`.

`Tell()` is the table of contents for a workflow. Chapters contain individual operations, while
the typed context carries state. This keeps orchestration visible and avoids competing chain or
flow abstractions.

## Registration and execution

`AddSolTale()` scans supplied assemblies, or the entry and calling assemblies by default. It
registers chapters and handlers as transient, `TaleManager` as scoped, and the default
`InMemoryTaleRepository` as singleton.

`TaleManager` creates a fresh dependency-injection scope for every start or resume operation. It
supports persisted pause/resume, cancellation, state retrieval, and start deduplication through
an idempotency key.

## Persistence

`ITaleRepository` is the application-owned persistence boundary. `UseTaleRepository<T>()`
replaces the in-memory default; the application owns that repository's dependencies and chosen
lifetime. `ListAsync` is optional and throws `NotSupportedException` unless implemented.

The core package does not ship a database provider. DreamTravel contains
`SQLiteTaleRepository` as a sample-side implementation. Process-restart durability therefore
exists only when the application registers a durable repository.

## HTTP surface

Applications expose Tale endpoints by inheriting `TaleController` and adding their authorization
policy. The base route is `/api/tale`:

| Operation | Endpoint |
|---|---|
| Start | `POST /{handler}/start` |
| Resume | `POST /{id}` |
| State | `GET /{id}` |
| Result | `GET /{id}/result` |
| Cancel | `DELETE /{id}` |

Waiting for input returns `202`; ordinary success returns `200`. Only handlers discovered by
`AddSolTale()` can be resolved by the controller.
