# ADR-007: CQRS Production Hardening + In-House Mediator

> **Status:** Accepted
> **Decision Date:** 2026-05-26
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Anyone consuming `SolTechnology.Core.CQRS`, DreamTravel & TaleCode samples

---

## Context

`SolTechnology.Core.CQRS` v0.7.0 is the entrypoint to every handler in this repo and in
downstream apps. A read-through surfaced production-blocking issues:

1. **MediatR vendor risk.** MediatR v12 is the last permissively-licensed line; v13+ moved to a
   commercial model. We use a thin slice: `Send`, `Publish`, one open `IPipelineBehavior<,>`.
   Owning the slice is a few hundred lines.
2. **Double-registration bug.** Calling both `RegisterCommands` *and* `RegisterQueries` registers
   MediatR, both pipeline behaviors, and the validator scan twice — every request is logged
   twice and validated twice.
3. **Validation throws.** `FluentValidationPipelineBehavior` throws `ValidationException` instead
   of returning `Result.Fail(new ValidationError(...))` — contradicts the Result pattern.
4. **Docs lie.** `docs/CQRS.md` advertises `RegisterChain()`, `ChainHandler<>`, `IChainStep<>`,
   `ChainContext<>`. None exist. Multi-step orchestration is the job of `SolTechnology.Core.Story`.
5. **Result / Error model leaks.** `Result<T>.Data` is publicly settable; implicit
   `Exception → Result<T>` keeps only `Message`; `AggregateError.Message` setter throws
   `NotImplementedException`; `Error.From(Exception)` puts `StackTrace` into `Description`.
6. **Notification semantics unclear.** DreamTravel uses notifications as fire-and-forget side
   effects but the current mediator awaits them inline.
7. **No test project.** Every other module under `tests/` has one.
8. **Public surface is anaemic.** No `ICommand` / `IQuery` markers, no `Result` combinators.

## Decision

### 1. Replace MediatR with an in-house mediator

Drop the `MediatR` `PackageReference`. Implement the minimal surface with **CQRS uppercased**
per ADR-001:

```csharp
namespace SolTechnology.Core.CQRS;

// Markers
public interface ICommand : ICommand<Nothing> { }
public interface ICommand<TResult> { }
public interface IQuery<TResult> { }
public interface INotification { }

// Handlers
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
}
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

// Single mediator — overload resolution distinguishes command / query / notification.
public interface IMediator
{
    Task<Result>          Send(ICommand command,             CancellationToken ct = default);
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> c, CancellationToken ct = default);
    Task<Result<TResult>> Send<TResult>(IQuery<TResult> q,   CancellationToken ct = default);
    void Publish<TNotification>(TNotification notification) where TNotification : INotification;
}

// Behaviors
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request,
                           RequestHandlerDelegate<TResponse> next,
                           CancellationToken cancellationToken);
}
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
```

| Choice | Rationale |
|---|---|
| Three markers (`ICommand` / `IQuery` / `INotification`) instead of one `IRequest<T>` | Call-site clarity. A reviewer sees `IQuery<User>` and knows: no side effects, safe to retry. |
| `Result` / `Result<T>` implied by marker | Removes `IRequest<Result<...>>` line noise. Handlers can't accidentally return non-`Result`. |
| Single `IMediator` with overloaded `Send` + `Publish` | Overload resolution distinguishes at compile time. A second tier of segregated senders adds surface for no behavioral benefit. |
| `Publish` returns `void` | Notifications are fire-and-forget. A `Task` return invites `await` and reintroduces coupling. |

### 2. Validation on by default

```csharp
services.AddCQRS(); // logging + validation behaviors registered
services.AddCQRS(o => o.UseFluentValidation = false); // only logging
```

`FluentValidationPipelineBehavior` returns `Result.Fail(new ValidationError)` — never throws.

### 3. Notifications are fire-and-forget — all handlers run

`Publish<T>` returns `void`. For each handler: fresh scope, `Task.Run`, isolated `try/catch`
that logs failures. A failing handler never crashes the producer and never stops sibling handlers.

### 4. Drop Chain

Removed from docs. Multi-step orchestration is `SolTechnology.Core.Story` (ADR-002).

### 5. Tighten Result / Error model

- `Result<T>.Data` → `init`-only.
- Drop implicit `Exception → Result<T>`.
- `Error` subtypes → `record`s with `[JsonDerivedType]`.
- `AggregateError` fixed.
- `ResultExtensions` (`Bind`/`Map`/`Tap`/`Match`/`Ensure`).

### 6. Versioning

**0.8.0** — pre-prod; 1.0 ships after production bake.

### 7. Test project

`tests/SolTechnology.Core.CQRS.Tests` — NUnit + NSubstitute + FluentAssertions + AutoFixture.

## Consequences

**Positive**
- Vendor risk gone.
- Compile-time clarity at call site.
- Validation failures travel via `Result`.
- Fire-and-forget notifications match DreamTravel usage.

**Negative**
- `Publish` is fire-and-forget by contract. Callers that want to await completion don't get a
  way to do it. By design — the prior `await mediator.Publish(...)` pattern coupled producers
  to background work.

## Alternatives considered

1. **Pin MediatR to `[12.*, 13.0.0)` and just fix the bugs.** Leaves a commercial dependency
   one `dotnet add package` away. Rejected.
2. **Segregated senders (`ICommandSender` / `IQuerySender`).** Adds surface for no behavioral
   benefit — overload resolution already distinguishes at compile time. Rejected.
3. **Make `Publish` return `Task`.** Reintroduces the trap we just removed. Rejected.

## Related

- ADR-001 — Acronym capitalization (mandates `CQRS`, `AddCQRS`, `CQRSOptions`).
- ADR-002 — Story Framework (replaces Chain pattern).
- ADR-006 — Implementation Plan Workflow.

