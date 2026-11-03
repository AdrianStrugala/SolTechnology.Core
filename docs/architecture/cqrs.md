# CQRS

`AddSolCQRS()` registers the in-house CQRS mediator and discovers command, query, and event
handlers from explicitly configured assembly sets. The scoped `CQRSMediator` supports
`ICommand`, `ICommand<T>`, and `IQuery<T>` through transient pipeline behaviors. The module has no
MediatR dependency. Consumer usage lives in [`docs/CQRS.md`](../CQRS.md).

Logging and FluentValidation behaviors are enabled independently and default to enabled.
Validation failures return `Result.Fail(ValidationError)` without invoking the handler or
throwing a business-validation exception.

## Dispatch

Each `Send` overload names the handler interface it dispatches to: `ICommandHandler<TCommand>`,
`ICommandHandler<TCommand, TResult>`, or `IQueryHandler<TQuery, TResult>`. The mediator does not
infer intent from the request's interfaces, so one type that implements both `IQuery<T>` and
`ICommand<T>` reaches the query handler or the command handler depending on the overload used.
`RequestDispatcherCache` compiles one handler-invocation delegate per (request type, handler
interface) pair.

Discovery scans exported types only, so an internal handler is never registered. A request without
a registered handler fails with an `InvalidOperationException` that names the request type and the
`RegisterCommandsFromAssembly` or `RegisterQueriesFromAssembly` call to add, instead of a
`NullReferenceException` deep in the pipeline.

Pipeline behaviors and event handlers are called through their interfaces
(`IPipelineBehavior<,>.Handle`, `IEventHandler<>.Handle`) by delegates created once per closed type
from a generic method. Explicit interface implementations therefore work, and a synchronous throw
surfaces as the original exception rather than a `TargetInvocationException`. Behaviors fold
around the handler in registration order, the first registered outermost; `AddSolCQRS` registers
logging before validation, so validation failures are logged as failed operations. The hot path
still builds the closed behavior and handler interface types per call and resolves behaviors from
the container; the caches remove the per-call method lookup and invocation reflection.

## Operation logging

`LoggingPipelineBehavior` treats each request as one operation named after the request type. It
writes `OperationStarted` and exactly one outcome entry:

| Outcome | Entry | Activity status |
|---|---|---|
| Handler returned a successful result or a non-`Result` response | `OperationSucceeded` (2138, Information) | `Ok` |
| Handler returned a failed `Result` | `OperationFailedWithResult` (2149, Warning) with `ErrorType` and `ErrorMessage` | `Error` |
| An exception escaped the pipeline | `OperationFailed` (2139, Error) with the exception | `Error`, exception recorded |

A failed `Result` carries only the `Error` runtime type name and `Error.Message`. It never logs
`ValidationError.Errors`, `AggregateError.InnerErrors`, other error properties, or `Result<T>.Data`,
because those can hold user input and land in long-retention sinks; `Error.Message` is the part of
an error designed for display, and its content is the consumer's responsibility (an
`AggregateError` composes its inner messages into it). A failed `Result` is an expected business
outcome, hence Warning rather than Error. The `[LoggerMessage]` emitter for 2149 is internal to
CQRS, so Logging exposes no extra public member; event ids 2137–2140 belong to Logging's
`LoggerExtensions` (see [Logging](logging.md)).

`[LogScope]` request properties are read through compiled getters cached per request type, masked
when marked `[Masked]`, pushed as one log scope, and set as activity tags. Activities start on the
CQRS-owned `ActivitySource` named `CoreLoggingActivitySources.OperationsName`.

## Events

`Publish()` delegates through replaceable `IEventPublisher` and `IEventDispatcher` boundaries.
The in-memory publisher creates one `Task.Run` and one fresh dependency-injection scope per
published event. Event handlers then execute sequentially in registration order inside that
scope. Each handler exception is logged with the original exception and swallowed so later
handlers still run.

The event abstraction is intentionally transport-replaceable. This fire-and-forget behavior is
not a durability guarantee; use a persistent publisher when delivery must survive process loss.

## Result model

Handlers communicate expected outcomes through `Result` and typed `Error` records. Commands and
queries remain explicitly separated because they have different consistency and latency goals.
The library enforces result-shaped responses and validation behavior; query side-effect freedom
remains an application convention rather than a runtime constraint.

The in-house mediator avoids external licensing exposure, keeps the public contract small, and
prevents duplicate behavior registration. The package ships its XML documentation, so the public
contract is described in IntelliSense.
