---
status: completed
created: 2026-05-26
completed: 2026-05-29
---

# CQRS Production Hardening

> Historical delivery record. It may not describe the current system.

## Goal

Replace MediatR with an in-house mediator and harden validation, events, registration, and the
Result/Error model.

## Context

The module needed a smaller public contract, predictable result-shaped handlers, idempotent
registration, and a transport-replaceable event boundary. A production review identified:

- reliance on the last permissively licensed MediatR major version for only a small subset of its
  behavior;
- duplicate mediator, behavior, and validator registration when command and query registration
  were combined;
- validation exceptions that contradicted the repository's `Result` contract;
- stale Chain documentation for APIs that no longer existed;
- mutable or lossy `Result` and `Error` behavior;
- notifications whose awaited execution did not match DreamTravel's fire-and-forget use;
- no dedicated CQRS test project or command/query marker interfaces.

## Original decision

Replace MediatR with a focused in-house mediator whose public abstractions reveal intent:

- `ICommand` and `ICommand<TResult>` for state-changing requests;
- `IQuery<TResult>` for reads;
- `IEvent` for fire-and-forget notifications;
- matching handler interfaces that always return `Result` or `Result<T>`;
- one `IMediator` with overloaded `Send` and a non-awaitable `Publish`;
- `IPipelineBehavior<TRequest, TResponse>` for validation and logging.

Three separate request markers were selected instead of a general `IRequest<T>` so reviewers could
distinguish commands from retry-safe queries at the call site. Result-shaped responses were implied
by the marker interfaces to prevent handlers from returning arbitrary transport types.

Validation and logging were enabled by default. Validation failures were to return
`Result.Fail(ValidationError)` without calling the handler.

Events were defined as fire-and-forget. The initial plan described an isolated background task and
scope for every handler, with failures logged and prevented from stopping siblings. The delivered
implementation later differed in task/scope granularity, recorded below.

The plan also tightened `Result<T>.Data`, removed implicit exception conversion, introduced typed
error serialization, fixed aggregate errors, and proposed `Bind`, `Map`, `Tap`, `Match`, and
`Ensure` combinators.

## Alternatives considered

### Pin MediatR to its permissive major version

Rejected because the repository used only a small surface and would remain exposed to accidental
commercial-version upgrades.

### Add separate command and query sender services

Rejected because overload resolution already provided compile-time distinction and another service
tier added surface without different behavior.

### Return `Task` from event publication

Rejected because awaiting publication would couple producers to background side effects and
contradict the selected fire-and-forget contract.

## Scope

- Add the in-house mediator and command/query/event marker interfaces.
- Enable logging and validation pipeline behaviors by default.
- Add replaceable event publisher and dispatcher boundaries.
- Remove Chain and MediatR dependencies.
- Harden errors, serialization, registration, and tests.

## Implementation plan

The work was delivered in nine steps covering the mediator core, validation, events, Result/Error,
registration, migration, tests, documentation, and package cleanup. A dedicated NUnit project used
NSubstitute, FluentAssertions, and AutoFixture.

## Acceptance criteria

- CQRS has no MediatR dependency.
- Validation failures return `Result.Fail` without invoking handlers.
- Event handler failures do not prevent later handlers from running.
- Registration is idempotent and test-covered.
- Commands and queries always return result-shaped responses.
- Event publication returns immediately and isolates handler failures.
- Error subtypes serialize without exposing exception stack traces as public descriptions.
- CQRS no longer documents or depends on the removed Chain abstraction.

## Expected consequences

### Positive

- The module owns a small, license-independent mediator contract.
- Request intent and result semantics are visible at compile time.
- Validation failures remain in the ordinary `Result` flow.
- Event transport can be replaced without changing producers or handlers.

### Negative

- Consumers cannot await event completion through `Publish`.
- Replacing MediatR requires downstream type and registration migration.
- The repository owns maintenance and compatibility for mediator dispatch and behaviors.

## Completion summary

The in-house mediator, command/query/event contracts, default logging and validation, idempotent
registration, replaceable publisher/dispatcher event seams, typed error model, migration, and
dedicated tests shipped. The MediatR dependency and stale Chain documentation were removed.

Current behavior lives in [`../architecture/cqrs.md`](../architecture/cqrs.md).

## Deviations

- Planned `Bind`, `Map`, `Tap`, `Match`, and `Ensure` Result combinators did not ship.
- The in-memory publisher creates one task and scope per event; handlers run sequentially within
  that scope, not one task and scope per handler.

## Follow-ups

- Reconsider Result combinators only as a new feature with demonstrated consumer need.
- Keep durable event transport outside the in-memory mediator and replace it through the publisher
  boundary.
