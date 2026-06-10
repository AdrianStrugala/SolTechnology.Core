---
adr: 009-hangfire-persistent-events-and-jobs
step: 02 of 10
status: done
---

# Step 02: CQRS dispatch seam — `IEventPublisher` / `IEventDispatcher`

## Summary
Extract the event-dispatch mechanism behind two public seams so a plugin can replace *how an event
leaves `Publish`* without CQRS knowing about Hangfire. `IEventPublisher` decides what happens when an
event is published (default: dispatch in-process now). `IEventDispatcher` fans an event out to its
registered handlers (the body currently inlined in `CQRSMediator.Publish<T>`). The in-memory defaults
preserve today's exact fire-and-forget behaviour, so every existing consumer stays green. This is
**plumbing** — no new application logic — and is kept separate from the rename (step 01) and from the
Hangfire publisher (step 04).

## Behaviour parity — pin the ACTUAL semantics (review correction)
The cross-check found that `CQRSMediator.Publish<T>` does **not** match its own XML doc. The doc
claims "every handler runs on its own background task with a fresh DI scope"; the real code
(`Internal/Mediator.cs:43-60`) does:

1. one `_ = Task.Run(...)` per `Publish` call (fire-and-forget, returns immediately);
2. **one** `IServiceScopeFactory.CreateAsyncScope()` for the whole call (NOT one scope per handler);
3. resolves all handlers from that single scope and runs them in a **sequential `foreach`**;
4. a **per-handler** `try/catch` that logs and swallows, so a throwing handler never stops its
   siblings and never propagates to the caller.

**Preserve items 1–4 exactly.** Do not introduce per-handler scopes or parallelism. While the code is
moving anyway, **correct the carried-over XML docs** on `IEventHandler<T>` and `IMediator.Publish` (and
the new `IEventDispatcher`) to describe the real single-scope/sequential semantics.

## Affected components
- `src/SolTechnology.Core.CQRS/IEventPublisher.cs` — **new** public interface:
  `void Publish<TEvent>(TEvent @event) where TEvent : IEvent;` and the non-generic
  `void Publish(IEvent @event);`. XML doc: "Decides what happens when an event is published — the
  replaceable seam (`AddPersistentEvents` swaps this)."
- `src/SolTechnology.Core.CQRS/IEventDispatcher.cs` — **new** public interface:
  `Task Dispatch(IEvent @event, CancellationToken cancellationToken);`. XML doc: "Fans an event out to
  every registered `IEventHandler<T>` resolved from the **ambient DI scope**; failures isolated +
  logged; handlers run sequentially."
- `src/SolTechnology.Core.CQRS/Internal/EventDispatcher.cs` — **new** `internal sealed` default,
  registered **scoped**. Injects the **scoped** `IServiceProvider` + `ILogger`. `Dispatch` resolves
  `IEventHandler<TEvent>` by the event's **runtime type** (the existing reflection path) from the
  injected provider, then runs the sequential `foreach` + per-handler try/catch. It does **not** create
  its own scope — the caller (publisher) owns scope creation (see lifetime decision below). This keeps
  the non-generic dispatch working and makes `Dispatch` the single unit a Hangfire job will later call.
- `src/SolTechnology.Core.CQRS/Internal/InMemoryEventPublisher.cs` — **new** `internal sealed`
  default `IEventPublisher`, registered **singleton**. Injects `IServiceScopeFactory` + `ILogger`
  (**not** `IEventDispatcher` directly — see lifetime decision). `Publish` reproduces today's
  fire-and-forget: `_ = Task.Run(async () => { await using var scope = scopeFactory.CreateAsyncScope();
  var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>(); await
  dispatcher.Dispatch(@event, CancellationToken.None); });`. One scope per publish, dispatcher +
  handlers resolved inside it — byte-for-byte today's behaviour.
- `src/SolTechnology.Core.CQRS/Internal/Mediator.cs` — `CQRSMediator` now depends on
  `IEventPublisher`; `Publish<TEvent>` / `Publish(IEvent)` delegate to it. The fan-out body is **gone**
  from the mediator. `ArgumentNullException.ThrowIfNull` guards stay. (`CQRSMediator` itself remains
  registered **scoped** — see ModuleInstaller line 30 — unchanged by this step.)
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — `AddCQRS` `TryAdd`s the defaults:
  `services.TryAddScoped<IEventDispatcher, EventDispatcher>();`
  `services.TryAddSingleton<IEventPublisher, InMemoryEventPublisher>();`.

## Lifetime decision — RESOLVED (encodes maintainer decision B2)
- **Publisher = singleton + `IServiceScopeFactory`** (matches B2 and today's `CQRSMediator`, which
  *also* injects `IServiceScopeFactory` and creates the per-dispatch scope itself).
  > Review correction: an earlier draft justified "singleton" as "matching today's singleton
  > `CQRSMediator`". That is **factually wrong** — `CQRSMediator` is registered **scoped**
  > (`TryAddScoped<IMediator, CQRSMediator>()`). The real reason singleton is safe is that the
  > publisher injects **only** `IServiceScopeFactory` + `ILogger` and creates a fresh scope per
  > dispatch — it never captures a scoped service.
- **A singleton publisher must NOT constructor-inject the scoped `IEventDispatcher`** — that is a
  captive-dependency bug. The publisher resolves `IEventDispatcher` *from the scope it creates*.
- **Dispatcher = scoped**, resolved from the per-dispatch scope. It owns no scope itself.

## Override mechanism — RESOLVED (was an open question; pin it here)
`AddPersistentEvents()` (step 04) replaces the publisher with:
`services.RemoveAll<IEventPublisher>(); services.AddSingleton<IEventPublisher, HangfireEventPublisher>();`
This is **order-independent**: if `AddCQRS` ran first, `RemoveAll` + `Add` swaps the default; if
`AddPersistentEvents` ran first, `AddCQRS`'s `TryAddSingleton` sees the existing registration and skips.
Do **not** rely on `TryAdd` + registration order alone. Step 04 honours this exact mechanism.

## Details
- **Behaviour parity is the whole point of this step** — see the "Behaviour parity" section above.
- `IEventDispatcher.Dispatch` is the unit a Hangfire job will later call (step 04), so its signature
  must be Hangfire-serialisable-friendly: a single `IEvent` argument + `CancellationToken`. Do **not**
  make it generic — Hangfire enqueues by `MethodInfo` and the runtime type must drive handler lookup.
  (Note for step 04: an `IEvent` **interface** parameter only round-trips through Hangfire if the
  app's serializer emits type metadata — see step 04's serialisation requirement.)

## Acceptance criteria
- `CQRSMediator` contains no `GetServices`/`Task.Run` fan-out — it only calls `IEventPublisher`.
- With no plugin, `dotnet test tests/SolTechnology.Core.CQRS.Tests` passes unchanged — specifically
  `NotificationDispatcherTests` (`Publish_ReturnsImmediately`, `Publish_AllHandlersAreInvoked`,
  `Publish_ThrowingHandler_DoesNotPreventSiblingHandlers`, `Publish_ThrowingHandler_DoesNotThrowToCaller`)
  and `AddCQRSTests` stay green.
- `IEventPublisher` and `IEventDispatcher` are `public`; `EventDispatcher` and
  `InMemoryEventPublisher` are `internal`.
- The XML docs on `IMediator.Publish` / `IEventHandler<T>` / `IEventDispatcher` describe the real
  single-scope/sequential semantics (no "per-handler scope" claim).
- `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none — publisher/dispatcher lifetimes and the override mechanism are resolved above (B2). If the
  implementer finds a concrete reason the singleton-publisher-owns-scope model cannot reproduce a
  specific existing test, record the deviation rather than silently changing lifetimes.

