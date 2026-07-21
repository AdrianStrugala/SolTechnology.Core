---
name: command-query-event-tale
description: Author a use case in any app built on the `SolTechnology.Core.CQRS` / `SolTechnology.Core.Tale` NuGet packages — a command, a query, a fire-and-forget event, or a Tale (chapters). Covers simple handlers, Tales hosted in `Commands`/`Queries`, domain-model Tales in `DomainServices`, and long-running interactive Tales in `Workflows`. Use whenever adding a new command/query/event, promoting a handler past the §3 size budget into a Tale, adding a chapter, publishing or handling an `IEvent`, wiring a handler into a `ModuleInstaller`, or adding `[LogScope]` logging. Encodes the layout and rules from `ClaudeCodingGuide.md` §0/§3/§4/§11 and the DreamTravel reference app. Routes tests to `test-writing`, review to `code-review`, behaviour-preserving cleanup to `refactor`.
---

# Command-Query-Event-Tale

Procedure for writing a single use case the Tale Code way — in the DreamTravel sample **or in any
app that consumes the `SolTechnology.Core.CQRS` / `SolTechnology.Core.Tale` NuGet packages**. Where
[`ClaudeCodingGuide.md`](../../../docs/ClaudeCodingGuide.md) §3/§4 define **what** the artifact must
look like, this skill is the **how**: classify the artifact, place the files, fill them in the
canonical shape, wire registration, and log it — using DreamTravel as the reference implementation.

The CQRS/Tale **contracts** (`IQueryHandler<,>`, `ICommandHandler<>`, `IEvent`, `TaleHandler<,,>`,
`[LogScope]`, `Result`) ship in the packages and are identical for every consumer. The §0/§1
**folder layout** (`LogicLayer/`, `*.Domain/Events/`, `Presentation/`) is DreamTravel's canonical
structure — recommended, but a consumer app may use its own names. When in doubt, copy DreamTravel.

> One source of truth per topic. The binding rules live in the Coding Guide §0/§3/§4/§11 and are
> cited by number here, never copied. If a rule below disagrees with the guide, the guide wins —
> fix this skill in the same PR per §19.

## When to use

- **Always** when adding a new command, query, or event — in a sample app (`DreamTravel`,
  `TaleCode`) or in a consumer app built on the `SolTechnology.Core.CQRS` / `.Tale` packages.
- A handler grew past **~100 lines of business logic** or now talks to **> 1 external system** —
  §3 requires promotion to a Tale (chapters).
- Adding a chapter to an existing Tale, or adding an `InteractiveChapter` pause point.
- Publishing a domain event (`IMediator.Publish`) or writing its `IEventHandler<>`.
- Deciding where a Tale lives: dedicated to a `Queries`/`Commands` use case vs a write-side
  `DomainServices` Tale vs a long-running interactive `Workflows` Tale.

## When NOT to use

- Adding or changing a **public/protected symbol** in `src/SolTechnology.Core.*` (the CQRS/Tale
  *framework* itself). That is `CLAUDE.md §2` forbidden without confirmation — route to
  [`implementation-planning`](../../agents/implementation-planning.agent.md) and gate on
  [`premortem`](../premortem/SKILL.md).
- Writing tests for the use case → [`test-writing`](../test-writing/SKILL.md).
- Renaming internals / splitting an oversized chapter with no behaviour change →
  [`refactor`](../refactor/SKILL.md).
- Reviewing an already-written use case → [`code-review`](../code-review/SKILL.md).
- A multi-module or breaking change → [`implementation-planning`](../../agents/implementation-planning.agent.md).

## Documentation references

- [`ClaudeCodingGuide.md` §0](../../../docs/ClaudeCodingGuide.md) — decision tree (which layer).
- [`ClaudeCodingGuide.md` §3](../../../docs/ClaudeCodingGuide.md) — CQRS file layout, Result, validators, the ~100-line / >1-system promotion rule.
- [`ClaudeCodingGuide.md` §4](../../../docs/ClaudeCodingGuide.md) — Tale anatomy, `Tell()` no-logic rule, chapters, `DomainServices` vs `Workflows`.
- [`ClaudeCodingGuide.md` §11](../../../docs/ClaudeCodingGuide.md) — logging rules (`[Brackets]` around values, structured placeholders, operation-lifecycle extensions).
- [`CQRS.md`](../../../docs/CQRS.md) / [`Tale.md`](../../../docs/Tale.md) — public API of the two libraries.
- [`Hangfire.md`](../../../docs/Hangfire.md) — durable (persisted) event dispatch.
- DreamTravel reference implementations:
  - Query-as-Tale — `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/CalculateBestPath/`
  - Simple command — `.../DreamTravel.Commands/FetchTraffic/`
  - Event publish + handler — `.../DreamTravel.Queries/FindCityByName/` + `.../DreamTravel.Domain/Events/CitySearched.cs` + `.../DreamTravel.Worker/EventHandlers/OnCitySearched/`
  - Tale in DomainServices — `.../DreamTravel.DomainServices/CityDomain/SaveCityTale/`
  - Interactive persisted Workflow — `.../DreamTravel.Workflows/SampleOrderWorkflow/`

## Critical rules

- **One folder per use case.** Folder name = use case name (§3). Never split a use case across
  technical "Models" / "Handlers" folders.
- **Handlers never throw for business failures.** Return `Result` / `Result<T>`. Throwing is
  reserved for true exceptions (network, IO) — the Tale engine converts those to errors (§4).
- **`Tell()` is a table of contents.** No `if`, no `try`, no loops, no logic — only
  `await ReadChapter<…>()` lines plus final output mapping. A branch means a new chapter (§4).
- **One chapter = one verb = one file.** File prefixed with order (`1.DownloadRoadData.cs`);
  class omits the prefix (`DownloadRoadData`) (§4).
- **All cross-chapter state lives on the `Context`.** Chapters never share fields or call each
  other (§4).
- **Reference the guide, don't restate it.** Cite §3/§4 in PR descriptions, not paraphrases.

## Procedure

### 1. Classify the artifact

| Question | Artifact | Interface (from `SolTechnology.Core.CQRS` / `.Tale`) | Return | DreamTravel example |
|---|---|---|---|---|
| Read-only, returns data? | **Query** | `IQueryHandler<TQuery, TResult>` | `Result<TResult>` | `FindCityByName` |
| Mutates state, no return? | **Command** | `ICommandHandler<TCommand>` | `Result` | `FetchTraffic` |
| Mutates state, returns data? | **Command + result** | `ICommandHandler<TCommand, TResult>` | `Result<TResult>` | — |
| ≥ 2 steps / > 1 external system / > ~100 lines? | **Tale** | `TaleHandler<TInput, TContext, TOutput>` (+ optionally `IQueryHandler<,>` / `ICommandHandler<,>`) | `Result<TOutput>` | `CalculateBestPath` |
| Something happened, others may react? | **Event** | `IEvent` + `IEventHandler<TEvent>` | `void` publish / `Task` handle | `CitySearched` |

Hosting a Tale (§4):

- **`Queries/`** — a complex query Tale is **always dedicated to its use case**. Never extract a
  query into a domain service.
- **`Commands/`** — a single command triggered by one entry point. Default for writes.
- **`DomainServices/`** — the Tale works **directly on domain models** (save / update / mutate) and
  is **reused by multiple commands or event handlers** (e.g. `SaveCityTale` behind
  `ICityDomainService.Save`, reused by the `CitySearched` event handler). Write-side only — expose a
  plain interface, inherit `TaleHandler` internally, and keep chapters under the service folder.
- **`Workflows/`** — long-running, **interactive** (`InteractiveChapter`), **persisted**. Requires
  a durable tale repository via `AddSolTale(...).UseTaleRepository<TRepository>()`.

### 2. Place the files (§3 layout)

Simple handler:

```
<UseCase>/
  <UseCase>Command.cs      ← input DTO + AbstractValidator (one file)
  <UseCase>Handler.cs
  <UseCase>Result.cs       ← only if there is output
```

Tale:

```
<UseCase>/
  <UseCase>Query.cs        ← input + validator
  <UseCase>Result.cs       ← output DTO
  <UseCase>Context.cs      ← inherits Context<TInput, TResult>; accumulator state
  <UseCase>Tale.cs         ← TaleHandler implementation
  Chapters/
    0.<FirstVerb>.cs
    1.<SecondVerb>.cs
```

Name the context file **and** class `<UseCase>Context` — keep them in sync (an older sample uses
a `…Narration.cs` filename; do not copy that drift).

### 3. Author the input + validator (same file)

The query/command class holds **only** the input DTO and its `AbstractValidator<>` — no logic.
Validators are auto-discovered; a validation failure short-circuits as `ValidationError` and the
handler never runs (§3, §12).

### 4. Author the Result DTO

A plain DTO — no behaviour, no nullable mystery (§3). Omit it entirely for a `Result`-only
command.

### 5. Author the handler — or decide it is a Tale

Write `IQueryHandler<,>` / `ICommandHandler<,>` with a primary constructor for dependencies and
`ILogger<TSelf>` last. Return via implicit conversion (`return city;` → `Result<City>.Success`).
The moment the handler crosses **~100 lines of business logic** or a **second external system**,
stop and convert to a Tale (step 6) — this is a §3 hard rule, not a preference.

### 6. Author the Tale

- `Context` inherits `Context<TInput, TResult>` and carries every cross-chapter field.
- The `TaleHandler<TInput, TContext, TResult>` passes `(serviceProvider, logger)` to the base
  ctor and may also implement `IQueryHandler<,>` / `ICommandHandler<,>` so the mediator resolves
  it directly (see `CalculateBestPathTale`).
- `Tell()` lists chapters top-to-bottom — prose only, zero logic.
- Each chapter inherits `Chapter<TContext>`, overrides `Read(TContext context)`, returns
  `Result.Success()` / `Result.Fail(...)` (or the `…AsTask()` variants). Mark `[UsedImplicitly]`
  if the IDE flags it (resolved via DI).

### 7. Interactive / persisted variant (`Workflows/`)

For a pause/resume step, inherit `InteractiveChapter<TContext, TInput>`, override
`ReadWithInput(context, userInput)`, and optionally override `ChapterId` for a stable resume
token (see `CustomerDetailsChapter`). The workflow needs a durable tale repository registered
(step 10) via `.UseTaleRepository<TRepository>()` — the default in-memory store cannot resume
across a restart (Tale.md).

### 8. Events

- The event is a pure record implementing `IEvent`, living in `*.Domain/Events/` (§0 — Domain
  references nothing).
- Publish fire-and-forget via the injected `IMediator.Publish(@event)` — it returns immediately;
  handler failures are isolated and logged, never propagate to the caller (CQRS.md).
- One background task and DI scope are created per event. Handlers run sequentially in that scope.
  Place handlers in the consumer (`Worker`/`Presentation`), one folder per event
  (`OnCitySearched/`).
- Need **durable** dispatch (survives a restart)? Use the Hangfire-backed publisher — see
  [`Hangfire.md`](../../../docs/Hangfire.md).

### 9. Logging

Every command/query is logged **automatically**. `LoggingPipelineBehavior` (registered by
`AddSolCQRS`) emits a **START** on entry and a **SUCCESS** / **FAIL** with duration on exit, keyed by
the request type name (`FindCityByNameQuery`). Do **not** hand-write begin/end logs for the
top-level handler — that is the §11 operation-lifecycle, already wired.

- **Enrich the scope with `[LogScope]`.** Mark a request property with `[LogScope]`
  (`SolTechnology.Core.Logging`) to project its value into the per-operation log scope; it also
  rides the tracing `Activity` tag. `[LogScope("Origin")]` renames the scope key. **PII is off by
  default** — only attributed properties enter the scope, so never mark a card / token / email /
  password field.

  ```csharp
  public sealed class FindCityByNameQuery : IQuery<City>
  {
      [LogScope]                 // -> scope["Name"] = value
      public string Name { get; set; } = null!;
  }
  ```

- **Event handlers and chapters are NOT on the pipeline.** Events dispatch through the
  fire-and-forget publisher and chapters run inside the Tale engine, so neither gets the automatic
  START/SUCCESS/FAIL. Log their boundaries yourself with the operation-lifecycle extensions from
  `SolTechnology.Core.Logging` — `_logger.OperationStarted(name)`,
  `_logger.OperationSucceeded(name, ms)`, `_logger.OperationFailed(name, ms, ex)` — over
  hand-rolled templates (§11).
- All §11 rules apply: inject `ILogger<TSelf>` via the primary ctor, wrap every value in `[]`,
  structured placeholders only (never interpolation), never pass a raw `ex.Message` as the template.

### 10. Register

Handlers, validators, chapters, and event handlers are discovered by **assembly scan** — no
per-type DI line:

- CQRS module: `services.AddSolCQRS(o => o.RegisterFromAssemblies(typeof(ModuleInstaller).Assembly));`
- Tale module: `services.AddSolTale(assemblies: typeof(SomeTale).Assembly);`
- Persisted workflow: `services.AddSolTale(assemblies: …).UseTaleRepository<SomeTaleRepository>();`.

Only hand-rolled collaborators (a `*Step`, a TSP engine) need an explicit `AddScoped`. If you add
or change a `ModuleInstaller.cs` contract, that is premortem-gated (`CLAUDE.md §4`).

### 11. Verify and hand off

- `get_errors` after every file edit (`CLAUDE.md §3`).
- Build the sample: `cd sample-tale-code-apps/DreamTravel && dotnet build`.
- Tests are **not** optional — hand off to [`test-writing`](../test-writing/SKILL.md). Every new
  `Result.Fail(...)` branch needs a negative test asserting the exact `Error.Code`.

## Pre-yield checklist

- [ ] Artifact classified (command / query / event / tale) and placed in the correct layer (§0)
      and host project (§4).
- [ ] One folder per use case; files named `<UseCase>{Query|Command|Result|Context|Tale}.cs`.
- [ ] Input DTO + `AbstractValidator<>` share one file; no logic in them.
- [ ] Handler returns `Result` / `Result<T>`; never throws for business failures.
- [ ] Handler ≤ ~100 lines of business logic **and** touches ≤ 1 external system — else it is a Tale.
- [ ] `Tell()` contains only `ReadChapter<…>()` calls plus output mapping — no `if`/`try`/loop.
- [ ] Chapters: one verb, numbered file prefix, class without prefix, state on `Context`, `[UsedImplicitly]`.
- [ ] Events implement `IEvent` in `*.Domain/Events/`; published via `IMediator.Publish`; handlers in the consumer.
- [ ] Logging: request properties needing visibility marked `[LogScope]` (no PII); event handlers / chapters log their own begin/end via `OperationStarted` / `OperationSucceeded` / `OperationFailed`.
- [ ] Registration is assembly-scan (`AddSolCQRS` / `AddSolTale`); only true collaborators added explicitly.
- [ ] `get_errors` clean; `dotnet build` green for the sample app.
- [ ] Tests handed to `test-writing`; every new failure branch has a negative test.

## Constraints

- DO NOT copy §3/§4 rule text into code comments or PRs — cite the section number.
- DO NOT put logic, branching, or loops in `Tell()`. Split into chapters instead.
- DO NOT let chapters share fields or call each other — all state flows through the `Context`.
- DO NOT throw from a handler/chapter for a business failure — return `Result.Fail(...)`.
- DO NOT register handlers/validators/chapters one-by-one — they are assembly-scanned.
- DO NOT hand-write START / END logs for a command/query handler — `LoggingPipelineBehavior`
  already emits them. Enrich with `[LogScope]` instead.
- DO NOT mark a PII property (card, token, email, password) with `[LogScope]` — only
  non-sensitive identifiers enter the scope.
- DO NOT add or change a public/protected symbol in `src/SolTechnology.Core.*` here. That is the
  framework surface — route to `implementation-planning`, apply `CLAUDE.md §2`, and use
  `premortem` only when `CLAUDE.md §4` or the risk warrants it.
- DO NOT exceed the §3 promotion threshold without converting to a Tale.
- DO NOT write the tests inline here — hand off to `test-writing`.
- DO NOT improvise a freehand use-case layout when this skill is unavailable. STOP and tell the
  user `command-query-event-tale` is required (`CLAUDE.md §3`). Freehand handlers are how
  100-line god-handlers and logic-in-`Tell()` re-enter the codebase.

