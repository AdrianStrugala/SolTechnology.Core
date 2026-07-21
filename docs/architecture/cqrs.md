# CQRS

`AddSolCQRS()` registers the in-house CQRS mediator and discovers command, query, and event
handlers from explicitly configured assembly sets. The scoped `CQRSMediator` supports
`ICommand`, `ICommand<T>`, and `IQuery<T>` through transient pipeline behaviors. The module has no
MediatR dependency.

Logging and FluentValidation behaviors are enabled independently and default to enabled.
Validation failures return `Result.Fail(ValidationError)` without invoking the handler or
throwing a business-validation exception.

## Events

`Publish()` delegates through replaceable `IEventPublisher` and `IEventDispatcher` boundaries.
The in-memory publisher creates one `Task.Run` and one fresh dependency-injection scope per
published event. Event handlers then execute sequentially in registration order inside that
scope. Each handler exception is logged and swallowed so later handlers still run.

The event abstraction is intentionally transport-replaceable. This fire-and-forget behavior is
not a durability guarantee; use a persistent publisher when delivery must survive process loss.

## Result model

Handlers communicate expected outcomes through `Result` and typed `Error` records. Commands and
queries remain explicitly separated because they have different consistency and latency goals.
The library enforces result-shaped responses and validation behavior; query side-effect freedom
remains an application convention rather than a runtime constraint.

The in-house mediator avoids external licensing exposure, keeps the public contract small, and
prevents duplicate behavior registration.
