# Claude Coding Guide — SolTechnology.Core / DreamTravel

> **Audience:** Claude Code (autonomous agent). This file is mandatory reading whenever
> you write or modify C# code in this repository. It is referenced from the root
> `CLAUDE.md` and supersedes any older convention you find in legacy code.
>
> **Goal:** every file you produce must be small, single-purpose, and read like prose.
> When in doubt, choose the option that makes the *next* developer reading the code
> understand it without scrolling.

---

## 0. Decision Tree — "What am I writing?"

Before touching any file, answer these in order:

1. **Is it user-driven I/O (HTTP, message, cron)?** → goes in `Presentation/` (Api, Worker, Ui).
2. **Is it business logic / orchestration?** → `LogicLayer/` (Commands, Queries, DomainServices, Workflows).
3. **Does it talk to a database / HTTP API / blob / bus?** → `DataLayer/` (one project per external system).
4. **Is it a pure domain concept (entity, value object, domain event)?** → `*.Domain/`.
5. **Is it cross-cutting plumbing (events publisher, email, generic infra)?** → `Infrastructure/`.

If a file would not fit cleanly into exactly one of those buckets, your design is wrong —
split the responsibility before writing the file.

---

## 1. Project / Folder Structure (DreamTravel as the canonical layout)

```
src/
  Presentation/
    DreamTravel.Api/              ← controllers, Program.cs, filters
    DreamTravel.Worker/           ← message/cron entry points
    DreamTravel.Ui/               ← Blazor / front-end host
    DreamTravel.ServiceDefaults/  ← Aspire defaults
  LogicLayer/
    DreamTravel.Commands/         ← write side (CQRS)
    DreamTravel.Queries/          ← read side (CQRS)
    DreamTravel.DomainServices/   ← reusable domain operations + Stories spanning multiple commands
    DreamTravel.Workflows/        ← long-running interactive Stories
    DreamTravel.TravelingSalesmanProblem/ ← isolated algorithmic engine
  DataLayer/
    DreamTravel.Sql/              ← EF Core DbContext + entities + QueryBuilders
    DreamTravel.GraphDatabase/    ← Neo4j repositories
    DreamTravel.GeolocationDataClients/ ← Google / Michelin / GeoDb HTTP clients
  Infrastructure/
    DreamTravel.Infrastructure/   ← email, event publishing, generic plumbing
    DreamTravelDatabase/          ← migrations / DACPAC
  DreamTravel.Domain/             ← pure domain (records, value objects, domain events)
  DreamTravel.Aspire/             ← orchestration host
tests/
  Unit/                ← only for pure algorithms / domain invariants
  Component/           ← WebApplicationFactory + Testcontainers (preferred)
  EndToEnd/            ← real environment smoke tests
```

### Dependency direction (enforced — never violate)

```
Presentation ──► LogicLayer ──► DataLayer ──► Infrastructure
                                   │
                                   ▼
                                 Domain  (depended on by everything, depends on nothing)
```

Concrete rules:

- `Domain` references **nothing** (no EF, no MediatR, no Story, no logging).
- `DataLayer` may reference `Domain` and `SolTechnology.Core.*` packages — never `LogicLayer` or `Presentation`.
- `LogicLayer` may reference `DataLayer`, `Domain`, `Infrastructure`, and core packages — never `Presentation`.
- `Presentation` references everything below it but contains no business rules.
- `Infrastructure` is referenced by `LogicLayer` / `Presentation` for plumbing only; it never references them back.

If you need `LogicLayer` from `DataLayer`, the abstraction is in the wrong layer — move it.

### When to create a new project

Create a new `.csproj` only if **all** apply:

- It represents a distinct external system, bounded context, or deployable unit.
- It has its own `ModuleInstaller.cs`.
- It would otherwise force two unrelated concerns into one assembly.

Otherwise add a folder inside an existing project. Never split on technical grounds
(e.g. "Models project" / "Helpers project") — split on responsibility.

---

## 2. The `ModuleInstaller` Pattern (mandatory)

Every project that registers services exposes exactly one `ModuleInstaller.cs`:

```csharp
namespace DreamTravel.Queries;

public static class ModuleInstaller
{
    public static IServiceCollection InstallTripsQueries(this IServiceCollection services)
    {
        services.RegisterQueries();   // SolTechnology.Core.CQRS scans the calling assembly
        services.RegisterStories();   // SolTechnology.Core.Story scans chapters & handlers
        services.AddTransient<ITSP, AntColony>();
        return services;
    }
}
```

Rules:

- One `Install<ProjectName>(this IServiceCollection)` extension method per project. Name it after the project domain, not the type — `InstallTripsQueries`, not `AddQueryHandlers`.
- `ModuleInstaller` lives at the project root, never nested.
- `Program.cs` calls only these extensions. It must not register individual services itself (with the narrow exception of framework wiring: CORS, Swagger, Auth, MVC filters, MediatR root, configuration binding).
- Do not call `RegisterCommands` / `RegisterQueries` / `RegisterStories` from `Program.cs` — they use `Assembly.GetCallingAssembly()` and must be invoked from inside the assembly that owns the handlers.
- Decorators go in the installer right after the registration they decorate (`services.Decorate(typeof(IGoogleHTTPClient), typeof(GoogleHTTPClientCachingDecorator));`).

---

## 3. CQRS — Commands and Queries

### File layout per use case

One folder per use case. Folder name = use case name. Inside the folder:

```
LogicLayer/DreamTravel.Queries/CalculateBestPath/
  CalculateBestPathQuery.cs        ← input + validator (one file)
  CalculateBestPathResult.cs       ← output DTO
  CalculateBestPathContext.cs      ← Story context (only if it's a Story)
  CalculateBestPathStory.cs        ← StoryHandler implementation
  Chapters/
    0.InitiateContext.cs
    1.DownloadRoadData.cs
    2.FindProfitablePath.cs
    3.SolveTsp.cs
    4.FormCalculateBestPathResult.cs
```

For a simple (non-Story) handler the folder shrinks to:

```
FetchTraffic/
  FetchTrafficCommand.cs
  FetchTrafficHandler.cs
  FetchTrafficResult.cs    (only if there is output)
```

### Rules

- **Query / Command class** holds *only* the input DTO + its `AbstractValidator<>` in the same file. No logic.
- **Result class** is a plain DTO — no behavior, no nullable mystery.
- **Handler** implements `IQueryHandler<,>` or `ICommandHandler<>` from `SolTechnology.Core.CQRS`. Always returns `Result` / `Result<T>`. Never throws for business failures.
- **Validators** are `AbstractValidator<TInput>` and live in the same file as the input. They are auto-discovered by `RegisterCommands()` / `RegisterQueries()`.
- A handler longer than ~40 lines of business logic must be converted into a Story (chapters).
- If a handler talks to more than one external system, it must be a Story.

### Result pattern

```csharp
return Result<City>.Success(city);
return Result<City>.Fail("City not found");
return Result.Success();
return Result.Fail(new Error { Message = "..." });
```

Implicit conversion is allowed in handlers: `return city;` becomes `Result<City>.Success(city)` automatically. Use it.

---

## 4. Story Framework (preferred for any multi-step orchestration)

Use a Story whenever the operation has ≥ 2 logical steps, or any single step is non-trivial.

### Anatomy

```csharp
public sealed class CalculateBestPathContext : Context<CalculateBestPathQuery, CalculateBestPathResult>
{
    public List<City> Cities { get; set; } = null!;
    // ...accumulator state used across chapters...
}

public class CalculateBestPathStory(IServiceProvider sp, ILogger<CalculateBestPathStory> logger)
    : StoryHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>(sp, logger),
      IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    protected override async Task TellStory()
    {
        await ReadChapter<InitiateContext>();
        await ReadChapter<DownloadRoadData>();
        await ReadChapter<FindProfitablePath>();
        await ReadChapter<SolveTsp>();
        await ReadChapter<FormCalculateBestPathResult>();
    }
}
```

### Rules

- `TellStory()` is the table of contents — it must read top-to-bottom as plain English and contain **no logic**, no `if`, no `try`, no loops. If you feel the urge to branch, split the branches into separate chapters or move the decision into a chapter that returns `Result.Fail` to short-circuit.
- One chapter = one verb = one file. File names are prefixed with their order: `0.InitiateContext.cs`, `1.DownloadRoadData.cs`. Class names omit the prefix (`InitiateContext`, `DownloadRoadData`).
- Chapter classes inherit `Chapter<TContext>` (automated) or `InteractiveChapter<TContext, TInput>` (user input pause).
- All cross-chapter state lives on the `Context`. Chapters do not share fields, do not call each other.
- Mark chapters with `[UsedImplicitly]` if the IDE flags them — they are resolved via DI.
- Chapters return `Result.Success()` / `Result.Fail(...)`. Throwing is reserved for true exceptions (network, IO) — the framework converts them to errors.
- The `StoryHandler` may also implement `IQueryHandler<,>` / `ICommandHandler<>` so MediatR resolves it directly. This is the standard wiring.

### When to choose `DomainServices` vs Story-in-Queries/Commands

- **Story in `Queries/` or `Commands/`** — the operation is a single use case triggered by one entry point.
- **Story in `DomainServices/`** — the operation is reusable across multiple commands/queries (e.g. `CityDomainService.Save` is reused by several import flows). The domain service exposes the plain interface (`ICityDomainService.Save(...)`) and internally inherits `StoryHandler` to implement the orchestration.

### `Workflows/` project

Reserved for **long-running, interactive, persisted** stories (require `RegisterStories(StoryOptions.WithSqlitePersistence(...))`). One folder per workflow, mirroring the CQRS use-case layout (`SampleOrderWorkflow/Chapters/...`).

---

## 5. DataLayer

### SQL (`DreamTravel.Sql`)

```
DreamTripsDbContext.cs
DbModels/                ← EF entities, suffix `Entity`
  CityEntity.cs
EntityConfigurations/    ← IEntityTypeConfiguration<T>
QueryBuilders/           ← static extension classes on IQueryable<T>
  CityQueryBuilder.cs
ModuleInstaller.cs
```

Rules:

- **Entities are persistence shape, not domain.** They live in `DbModels/`, suffixed `Entity`. Domain models live in `*.Domain/` and are mapped via dedicated `*Mapper` classes (e.g. `CityMapper`).
- **Never expose `IQueryable<TEntity>` outside the SQL project.** Consumers receive domain objects.
- Reusable query composition goes into `QueryBuilders/` as `IQueryable<T>` extension methods (`WhereName`, `WhereCoordinates`, `ApplyReadOptions`). Keep them small, single-predicate, and named after what they filter.
- Configuration binding: `services.AddSQL(sqlConfiguration)` from `SolTechnology.Core.SQL` first, then `AddDbContext<>` for project-specific context.
- Migrations live in `Infrastructure/DreamTravelDatabase/`. Never put migrations next to the DbContext.

### HTTP clients (`DreamTravel.GeolocationDataClients`)

One folder per upstream system: `GoogleApi/`, `MichelinApi/`, `GeoDb/`. Inside:

```
GoogleApi/
  IGoogleHTTPClient.cs
  GoogleHTTPClient.GetLocationOfCity.cs   ← partial class, one method per file
  GoogleHTTPClient.GetNameOfCity.cs
  GoogleHTTPClient.GetDurationMatrixByTollRoad.cs
  GoogleHTTPClientCachingDecorator.cs
  GoogleHTTPOptions.cs
```

Rules:

- Use `services.AddHTTPClient<IXxx, Xxx, XxxOptions>("Xxx")` from `SolTechnology.Core.HTTP`.
- The client class is `partial`. **Each public method gets its own file.** Filename: `<Class>.<MethodName>.cs`. This is the canonical pattern (it replaces `#region`).
- Cross-cutting concerns (caching, retries, logging) are decorators in their own files: `XxxClient<Suffix>Decorator.cs`. Wire with `services.Decorate(...)`.
- Options classes hold configuration (`Key`, `BaseUrl`, etc.) and bind from `appsettings.json`.
- Do not leak transport types (`HttpResponseMessage`, `JObject`) past the interface — return domain models or DTOs.

### Repositories (Graph, etc.)

- One repository per aggregate (`IIntersectionRepository`, `IStreetRepository`).
- Methods are intent-named (`GetAllAsync`, `UpdateTrafficRegularTime`), not CRUD-named (`Update`, `Save`).
- Repositories return domain types, not driver types.

---

## 6. Domain Layer (`*.Domain`)

- C# `record` for value objects and immutable entities; `class` for aggregates with mutable invariants.
- No attributes from EF, MediatR, JSON, validation, or logging libraries. The domain knows nothing about how it is persisted, transported, or validated.
- Domain events live in `Events/`. They are records with past-tense names (`CityImported`, `TrafficRecalculated`).
- Validation that is a true domain invariant lives in the constructor / factory method. Input validation lives in FluentValidation validators in the LogicLayer.

---

## 7. Presentation — API

### `Program.cs`

`Program.cs` is wiring, not logic. Order:

1. `builder.AddServiceDefaults();` (Aspire).
2. Culture, CORS, configuration binding.
3. **Module installers** (one line per project): `InstallTripsSql`, `InstallGeolocationDataClients`, `InstallInfrastructure`, `InstallDomainServices`, `InstallTripsQueries`, `InstallGraphDatabase`, `AddFlows`, etc.
4. Cross-cutting: `AddCache`, `AddMediatR`, authentication, versioning, Swagger.
5. Filters: `ExceptionFilter`, `ResponseEnvelopeFilter` (always wired globally).
6. Build, configure pipeline, `MapControllers`, `Run`.

CORS policy names, scheme names, and other constants must be `const` with a meaningful name — never placeholder strings like `"dupa"`. If you encounter such a placeholder in legacy code, fix it as part of your task.

### Controllers

```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/[controller]")]
public class CalculateBestPathController(
    IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult> handler,
    ILogger<CalculateBestPathController> logger)
    : ControllerBase
{
    [HttpPost]
    [MapToApiVersion("2.0")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Result<CalculateBestPathResult>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> CalculateBestPath([FromBody] CalculateBestPathQuery query)
    {
        return Ok(await handler.Handle(query, CancellationToken.None));
    }
}
```

Rules:

- Controllers are **thin**. Body of an action ≤ 3 lines: log (optional), invoke handler, return.
- Inject the specific `IQueryHandler<,>` / `ICommandHandler<>` rather than `IMediator` whenever a single use case is involved. `IMediator` is acceptable when the controller fans out to multiple handlers.
- Never `try/catch` in a controller — `ExceptionFilter` handles it. Never serialize errors manually.
- One controller per resource/route; one folder per bounded context (`Trips/`, `RoadPlanner/`, `Statistics/`).
- API versioning: place version-specific controllers under `Trips/v1/`, `Trips/v2/`. Use `[ApiVersion]` + `[MapToApiVersion]`. Mark deprecated versions `Deprecated = true`.
- Document every action with XML `<summary>` (in English) and `[ProducesResponseType]` for every status code returned.

### Filters / middleware

- Use `ExceptionFilter` and `ResponseEnvelopeFilter` from `SolTechnology.Core.API` — register globally, never per-controller.
- Authentication wiring goes through `AddAuthenticationAndBuildFilter(...)` from `SolTechnology.Core.Authentication`.
- Custom middleware lives in `Middlewares/` in the API project.

---

## 8. Tests

### Where do tests go?

- **Unit (`tests/Unit/`)** — only for pure algorithms (e.g. `TravelingSalesmanProblem`), domain invariants, and individual chapters with non-trivial logic. Never write a unit test that mocks `IMediator`, `HttpClient`, `DbContext`, or `IRepository` to assert "the handler called X".
- **Component (`tests/Component/`)** — preferred. `WebApplicationFactory<Program>` + Testcontainers for SQL/Bus/Blob. This is the default home for new tests in DreamTravel.
- **EndToEnd (`tests/EndToEnd/`)** — real environment smoke tests, manual or pipeline-triggered.

### Unit test layout

Mirrors the production folder structure exactly:

```
tests/Unit/DreamTravel.Queries.UnitTests/
  CalculateBestPath/
    DownloadRoadDataTests.cs       ← one test class per chapter / handler
```

### Test conventions

- Frameworks: **NUnit** (core libs use xUnit historically; new tests use NUnit per repo convention). Assertion: **FluentAssertions**. Mocks: **NSubstitute**. Data: **AutoFixture** with `AutoNSubstituteCustomization`.
- Test class field: `_sut` for the system under test. Dependencies frozen via `fixture.Freeze<T>()`.
- Test name format: `Method_Scenario_ExpectedOutcome` (`Execute_ShouldPopulateContextWithRoadData`, `Resume_AfterPause_CompletesStory_AndPersistsTerminalState`).
- One arrange / one act / one assert *block* per test — but multiple related assertions inside the assert block are encouraged (denser tests > more tests).
- Parameterize with `[TestCase]` / `[TestCaseSource]` instead of duplicating tests.
- A test earns its place only if removing it would let a real regression through. Do not write tests that mirror the implementation shape.

---

## 9. Class-level rules ("small classes, one job")

These apply to every class you write, regardless of layer.

1. **One reason to change.** If you can describe the class with the word "and", split it.
2. **Size budget:** target ≤ 100 lines, hard cap ~150. Above that, extract a collaborator.
3. **Method size:** target ≤ 20 lines. Methods longer than that almost always hide a missing abstraction.
4. **Constructor size:** ≤ 5 dependencies. More than five = the class does too much. Move work into a Story or split the class. (`ILogger<T>` does not count toward the budget.)
5. **Primary constructors** are mandatory for DI capture (see root `CLAUDE.md` §14). Do not hand-write `private readonly` fields just to assign them.
6. **No statics with state.** Static methods are fine for pure helpers (`CityQueryBuilder`). Static *fields* with mutable state are forbidden outside `const` and `static readonly` lookup tables.
7. **`sealed` by default** for non-abstract classes. Open them up only when inheritance is the explicit design.
8. **`internal` by default.** A type is `public` only when it crosses an assembly boundary intentionally.
9. **No "Manager", "Helper", "Util" suffixes** unless the class genuinely is a generic helper (rare). Name by responsibility: `CityMapper`, `StreetTrafficUpdater`, `GoogleHTTPClient`.
10. **No `#region`.** Use partial classes (one method per file for HTTP clients) or extract a new class. The only exception is legacy test files explicitly listed in the root `CLAUDE.md`.

---

## 10. Naming conventions

- **Acronyms: ALL CAPS** (PEP 8 style) — `APIClient`, `SQLConfiguration`, `XMLDocument`, `CQRSHandler`, `AUID`, `HTTP`, `UI`, `IO`, `DB`. (See ADR-001.)
  - Existing published package names (`SolTechnology.Core.CQRS`, `SolTechnology.Core.AUID`) are grandfathered. New types follow the rule.
- **Files mirror their primary type name.** Exception: HTTP-client partials (`GoogleHTTPClient.<MethodName>.cs`) and ordered chapters (`0.InitiateContext.cs`).
- **Folders are nouns in PascalCase.** Use case folders are verb-noun (`CalculateBestPath`, `FetchTraffic`).
- **Async methods end with `Async` only when there is a sync overload to disambiguate.** Otherwise omit the suffix (most repository methods in this codebase do).
- **Boolean members start with `Is`/`Has`/`Should`/`Can`** (`IsNew`, `HasStatistics`).
- **Domain events are past tense** (`CityImported`).
- **Commands are imperative** (`FetchTraffic`, `RecalculateTraffic`). **Queries are interrogative or noun-phrased** (`CalculateBestPath`, `FindCityByName`, `GetSearchStatistics`).

---

## 11. Logging

- Inject `ILogger<TSelf>` via the primary constructor.
- **Always wrap variable values in square brackets:** `logger.LogInformation("Processing city [{CityName}]", name);`. Empty values become visible (`[]`) instead of invisible.
- Use structured logging placeholders (`{Name}`), never string interpolation in log messages — interpolation breaks log aggregation.
- Log at the boundaries (handler entry/exit, external call start/end, chapter transitions). Do not log inside tight loops.
- Errors: `logger.LogError(exception, "Message with [{Context}]", ctx);` — pass the exception as the first argument, never `ex.ToString()` inside the message.

---

## 12. Validation

- Input validation = **FluentValidation** in the same file as the Command/Query.
- Business invariants = inside the domain or chapter, returning `Result.Fail(...)`.
- Defensive parameter checks at module/library boundaries = **`SolTechnology.Core.Guards`**.
- Never validate in the controller. Never validate in the handler if a validator exists.

---

## 13. Error handling

- Default to `Result` / `Result<T>`. Throw only for genuinely exceptional conditions (network, IO, programmer error).
- A chapter that fails returns `Result.Fail(...)` — the Story stops. Do not throw to abort a Story.
- `try/catch` is allowed at the boundary of an external call inside a chapter or HTTP client method (to translate driver exceptions into domain errors). It is **not** allowed in controllers, handlers, or `TellStory()`.
- Never swallow exceptions silently. If a `catch` block has no `throw` and no `logger.LogError`, it is a bug.
- For aggregated failures across chapters, use `AggregateError` from `SolTechnology.Core.CQRS`.

---

## 14. Configuration

- Bind config sections to options classes (`SQLConfiguration`, `Neo4jSettings`, `GoogleHTTPOptions`).
- Bind in `Program.cs` and pass the *configuration object* into `Install...` methods — installers must not call `IConfiguration` directly.
- Secrets never live in `appsettings.json`. Use environment variables / Aspire / Key Vault.

---

## 15. Anti-patterns observed in this codebase — do not propagate

These are real examples spotted in DreamTravel. Fix them when you touch the surrounding file; never copy them.

| Anti-pattern | Where seen | Correct form |
| --- | --- | --- |
| Placeholder constant `private static readonly string CorsPolicy = "dupa";` | `DreamTravel.Api/Program.cs` | `private const string CorsPolicyName = "DreamTravelDefault";` |
| `try/catch` + `JsonConvert.SerializeObject(ex.Message)` in a controller | `CalculateBestPathController.CalculateBestPathV1` | Let `ExceptionFilter` handle it; return `Ok(result)`. |
| `logger.LogInformation("Skipped " + s.Name);` | `FetchTrafficHandler` | `logger.LogInformation("Skipped [{Street}]", s.Name);` |
| Multiple `+`-concatenated log strings | various | Structured placeholders, single interpolated raw string. |
| Naked `Newtonsoft.Json` usage in new code | `CalculateBestPathController` | `System.Text.Json` (and the Story/AUID converters) is the default. Newtonsoft only where Hangfire / legacy serialization requires it. |
| Mocking `IMediator` / `DbContext` in unit tests | hypothetical | Write a Component test instead. |
| Hand-written `private readonly` ctor capture | hypothetical | C# 12 primary constructor. |
| `#region` to organize a class | forbidden | Split into partial files or new classes. |

---

## 16. Workflow checklist (run for every task)

Before declaring a task done, you must:

1. ✅ Layer dependencies respected (run mental check from §1).
2. ✅ New services registered through a `ModuleInstaller`, not in `Program.cs`.
3. ✅ Class size, method size, ctor-arg budget within §9.
4. ✅ Public types have XML `<summary>` (English).
5. ✅ Logging uses placeholders + `[{value}]` brackets.
6. ✅ No `#region`, no placeholder strings, no swallowed exceptions, no `try/catch` in controllers/handlers.
7. ✅ Tests added/updated — Component test preferred, Unit test only for pure logic.
8. ✅ `dotnet build SolTechnology.Core.slnx` is green. For DreamTravel changes, `cd sample-tale-code-apps/DreamTravel && dotnet build` is green.
9. ✅ Relevant tests run green.
10. ✅ No new NU1902 / NU1903 / NU1603 warnings (see root `CLAUDE.md` §"Dependency Management").

If any item fails, fix it before yielding back to the user.

---

## 17. Quick reference — file templates

### Command

```csharp
// FetchTrafficCommand.cs
using FluentValidation;
using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Commands.FetchTraffic;

public class FetchTrafficCommand : IRequest<Result>
{
    public DateTime DepartureTime { get; set; }
}

public class FetchTrafficCommandValidator : AbstractValidator<FetchTrafficCommand>
{
    public FetchTrafficCommandValidator()
    {
        RuleFor(x => x.DepartureTime).NotEmpty();
    }
}
```

### Command handler (simple)

```csharp
public class FetchTrafficHandler(
    IGoogleHTTPClient googleClient,
    IStreetRepository streetRepo,
    ILogger<FetchTrafficHandler> logger)
    : ICommandHandler<FetchTrafficCommand>
{
    public async Task<Result> Handle(FetchTrafficCommand request, CancellationToken ct)
    {
        logger.LogInformation("Fetching traffic at [{Time}]", request.DepartureTime);
        // ... orchestration, ≤ 40 lines; otherwise convert to a Story ...
        return Result.Success();
    }
}
```

### Story query

```csharp
// CalculateBestPathStory.cs
public class CalculateBestPathStory(IServiceProvider sp, ILogger<CalculateBestPathStory> logger)
    : StoryHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>(sp, logger),
      IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    protected override async Task TellStory()
    {
        await ReadChapter<InitiateContext>();
        await ReadChapter<DownloadRoadData>();
        await ReadChapter<FindProfitablePath>();
        await ReadChapter<SolveTsp>();
        await ReadChapter<FormCalculateBestPathResult>();
    }
}
```

### Chapter

```csharp
[UsedImplicitly]
public class DownloadRoadData(IGoogleHTTPClient google, IMichelinHTTPClient michelin)
    : Chapter<CalculateBestPathContext>
{
    public override async Task<Result> Read(CalculateBestPathContext context)
    {
        // single responsibility; no branching across chapters
        return Result.Success();
    }
}
```

### Controller

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class FindCityByNameController(
    IQueryHandler<FindCityByNameQuery, FindCityByNameResult> handler)
    : ControllerBase
{
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(Result<FindCityByNameResult>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(string name, CancellationToken ct)
        => Ok(await handler.Handle(new FindCityByNameQuery { Name = name }, ct));
}
```

### Module installer

```csharp
public static class ModuleInstaller
{
    public static IServiceCollection InstallTripsQueries(this IServiceCollection services)
    {
        services.RegisterQueries();
        services.RegisterStories();
        services.AddTransient<ITSP, AntColony>();
        return services;
    }
}
```

---

---

## 18. Self-improvement — keep this guide alive

Whenever you (Claude / the agent) **learn something new** during a task that future iterations
should not have to rediscover, you must update your own instructions immediately, in the same
turn, before yielding back to the user.

Triggers — update the guide when:

- The user corrects you on a convention, naming, structure, or workflow.
- You discover a non-obvious constraint of the codebase (build quirks, framework rule, DI pitfall).
- A repeated mistake gets called out (e.g. forgetting `{}` after `if`, missing primary ctor, partial code dumps).
- A new pattern, helper, or framework addition becomes "the way" to do something here.
- An ADR is written or amended — reflect its rule here in one line + link.

How to update:

1. Find the most relevant section (§0–§17). If none fits, add a new numbered section at the end.
2. Add the rule as a single, imperative bullet — short, concrete, copy-pasteable. No prose essays.
3. If the lesson is broad enough to affect *all* tasks, also add it to the §16 workflow checklist.
4. If it is repository-wide (not Story/CQRS/etc. specific), mirror a one-liner into root `CLAUDE.md`.
5. Mention the update in your reply to the user (one sentence: *"Added rule X to §N."*).

Do **not** wait to be told to update the guide — silent retention is forbidden. If a lesson is
worth remembering for next time, it is worth writing down now.

---

**Last word.** Code in this repository is written for the next reader, not the next compiler.
If a chapter, handler, or controller is hard to read aloud, it is wrong — split it,
rename it, narrate it. That is the Tale Code philosophy, and it is enforced.
