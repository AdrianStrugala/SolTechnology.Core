# Claude Coding Guide — SolTechnology.Core / DreamTravel

Convention layer for the agent. Defines **what** the agent writes: project structure,
CQRS, naming, logging, tests, documentation shape. Operational behaviour (pre-flight,
tool usage, forbidden actions, dependency management) lives in the root
[`CLAUDE.md`](../CLAUDE.md). One source of truth per topic — when in doubt, link,
don't copy.

Section numbers (§0–§N) are stable cite-targets. `CLAUDE.md`, ADRs and skills reference
them by number; never renumber an existing section. New rules append at the end.

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
- A handler longer than ~100 lines of business logic must be converted into a Story (chapters).
- If a handler talks to more than one external system, it must be a Story.

### Result pattern

```csharp
return Result<City>.Success(city);
return Result<City>.Fail("City not found");
return Result.Success();
return Result.Fail(new Error { Message = "..." });
```

Implicit conversion is allowed in handlers: `return city;` becomes `Result<City>.Success(city)` automatically. Use it.

> **Procedure:** the step-by-step for authoring a command/query/event/story lives in the
> [`command-query-event-story`](../.github/skills/command-query-event-story/SKILL.md) skill.

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

- **Story in `Queries/`** — a complex query is **always dedicated to its use case**. Never extract a query into a domain service; there is no reuse case that justifies it.
- **Story in `Commands/`** — a single write triggered by one entry point.
- **Story in `DomainServices/`** — the orchestration works **directly on domain models** (a save / update / mutation) and is **reused by multiple commands or event handlers** (e.g. `CityDomainService.Save` is reused by the `CitySearched` event handler and import flows). The domain service exposes a plain interface (`ICityDomainService.Save(...)`) and internally inherits `StoryHandler`. Domain services are a write/command-side concept — never a home for queries.

### `Workflows/` project

Reserved for **long-running, interactive, persisted** stories (require `RegisterStories(StoryOptions.WithSqlitePersistence(...))`). One folder per workflow, mirroring the CQRS use-case layout (`SampleOrderWorkflow/Chapters/...`).

> **Procedure:** authoring a Story (chapters, contexts, `DomainServices` vs `Workflows` hosting)
> is driven by the [`command-query-event-story`](../.github/skills/command-query-event-story/SKILL.md)
> skill.

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

- Frameworks: **NUnit** (all test projects). Assertion: **FluentAssertions**. Mocks: **NSubstitute**. Data: **AutoFixture** with `AutoNSubstituteCustomization`.
- Test class field: `_sut` for the system under test. Dependencies frozen via `fixture.Freeze<T>()`.
- Test name format: `Method_Scenario_ExpectedOutcome` (`Execute_ShouldPopulateContextWithRoadData`, `Resume_AfterPause_CompletesStory_AndPersistsTerminalState`).
- One arrange / one act / one assert *block* per test — but multiple related assertions inside the assert block are encouraged (denser tests > more tests).
- **Mark the three blocks with `// Arrange`, `// Act`, `// Assert` comments.** This is the one place where comments restating *what* is allowed (and required) — they delimit the test phases, which makes scanning failures and reviewing tests dramatically faster. Skip a phase only when it is genuinely empty (e.g. a parameterless `// Act` for a static call). Example:
  ```csharp
  [Test]
  public async Task Resume_AfterPause_CompletesStory_AndPersistsTerminalState()
  {
      // Arrange
      var input = _fixture.Create<OnboardingInput>();
      var start = await _sut.StartStory<UserOnboardingStory, ...>(input);

      // Act
      var resume = await _sut.ResumeStory<UserOnboardingStory, ...>(start.Data!.StoryId, _userInput);

      // Assert
      resume.IsSuccess.Should().BeTrue();
      resume.Data!.Status.Should().Be(StoryStatus.Completed);
      (await _repo.GetAsync(start.Data.StoryId))!.Status.Should().Be(StoryStatus.Completed);
  }
  ```
- Parameterize with `[TestCase]` / `[TestCaseSource]` instead of duplicating tests.
- A test earns its place only if removing it would let a real regression through. Do not write tests that mirror the implementation shape.

---

## 9. Class-level rules ("small classes, one job")

These apply to every class you write, regardless of layer.

1. **One reason to change.** If you can describe the class with the word "and", split it.
2. **Size budget:** target ≤ 100 lines, hard cap ~150. Above that, extract a collaborator.
3. **Method size:** target ≤ 20 lines. Methods longer than that almost always hide a missing abstraction.
4. **Constructor size:** ≤ 5 dependencies. More than five = the class does too much. Move work into a Story or split the class. (`ILogger<T>` does not count toward the budget.)
5. **Primary constructors** are mandatory for DI capture. Do not hand-write `private readonly` fields just to assign them.
6. **No statics with state.** Static methods are fine for pure helpers (`CityQueryBuilder`). Static *fields* with mutable state are forbidden outside `const` and `static readonly` lookup tables.
7. **`sealed` by default** for non-abstract classes. Open them up only when inheritance is the explicit design.
8. **`internal` by default.** A type is `public` only when it crosses an assembly boundary intentionally.
9. **No "Manager", "Helper", "Util" suffixes** unless the class genuinely is a generic helper (rare). Name by responsibility: `CityMapper`, `StreetTrafficUpdater`, `GoogleHTTPClient`.
10. **No `#region`.** Use partial classes (one method per file for HTTP clients) or extract a new class. The only exception is legacy test files explicitly listed in the root `CLAUDE.md`.
11. **Comments earn their place.** Tale Code reads like prose — let names carry the meaning. Write a comment **only** when a reader cannot infer the *why* from the code itself: a non-obvious framework constraint, a workaround for a specific bug/version, an ADR pointer. Hard rule: **one line — two as the absolute exception**. No multi-line narration, no incident retrospectives in `//`, no restating *what* the next line does. If the explanation needs a paragraph, it belongs in an **ADR** (link it: `// See ADR-005.`) or in an **XML `<summary>`** on the public type — not inline. Inline `//` is a *pointer*, not the storage.
    ```csharp
    // ❌ BAD — three-line essay restating what the call does and re-explaining
    //         framework internals everyone can google.
    // Pass the assembly explicitly: the default scanner uses
    // Assembly.GetCallingAssembly() which is unreliable under JIT
    // inlining (and under WebApplicationFactory the entry assembly
    // becomes the test host, not the API).
    services.RegisterStories(assemblies: typeof(SaveCityStory).Assembly);

    // ✅ GOOD — one line, why-not-what, points at the root cause.
    // Explicit: GetCallingAssembly() is unreliable under JIT inlining / WAF.
    services.RegisterStories(assemblies: typeof(SaveCityStory).Assembly);
    ```
    ```csharp
    // ❌ BAD — 6-line incident retrospective lives in code forever.
    // Two timeout systems were a confusing source of incidents:
    // Polly's per-attempt RequestTimeout and HttpClient.Timeout
    // could fight, with the latter killing a retry mid-flight.
    // When the resilience pipeline is active, Polly owns time;
    // we set HttpClient.Timeout to InfiniteTimeSpan so the only
    // deadline is the one configured on HttpPolicyConfiguration.
    if (policyCfg.UsePolly) { httpClient.Timeout = Timeout.InfiniteTimeSpan; /* ... */ }

    // ✅ GOOD — one sentence, the why; the war story goes to docs/HTTP-Production-Checklist.md.
    // Polly owns the deadline when active; HttpClient.Timeout would otherwise kill retries mid-flight.
    if (policyCfg.UsePolly) { httpClient.Timeout = Timeout.InfiniteTimeSpan; /* ... */ }
    ```
    **Decision rule when you feel the urge to write 3+ comment lines:**
    - Does it document the *type's contract*? → move to XML `<summary>`.
    - Does it record a design decision / incident? → move to an ADR, link from one-line `//`.
    - Does it explain *what* the code does? → delete it, rename the symbol instead.
    - Is it genuinely a single non-obvious *why*? → keep, one line.

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
  - Rule applies to **every** placeholder, including:
    - forwarded user/exception messages: `logger.LogInformation("[{Message}]", message);`
    - durations and numbers: `"Duration: [{ElapsedMs} ms]"`, not `"Duration: {ElapsedMs} ms"`.
    - HTTP fields: `"[{RequestMethod}] [{RequestPath}] -> [{StatusCode}]"`.
  - The only acceptable bare placeholder is when the entire message is a single quoted literal that already provides visual delimiters (rare — prefer `[]` even then).
- Use structured logging placeholders (`{Name}`), never string interpolation in log messages — interpolation breaks log aggregation.
- **Never** pass an exception's `Message` (or any user-supplied string) as the message *template*: `logger.LogError(ex, ex.Message)` will throw `FormatException` if the text contains `{` / `}`. Always use a placeholder: `logger.LogError(ex, "[{Message}]", ex.Message)`.
- Property names use **PascalCase** (`{OperationName}`, not `{operationName}`) — matches MEL/Serilog/App Insights convention and KQL queries that consumers write.
- Log at the boundaries (handler entry/exit, external call start/end, chapter transitions). Do not log inside tight loops.
- Errors: `logger.LogError(exception, "Message with [{Context}]", ctx);` — pass the exception as the first argument, never `ex.ToString()` inside the message.
- Reusable extension methods that emit common shapes (operation lifecycle, HTTP request lifecycle) live in `SolTechnology.Core.Logging` and are the preferred entry point — `_logger.OperationStarted(name)` over hand-rolled templates.

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
| Multi-line "essay" comment restating *what* the next line does | various | One line, *why* only. See §9.11. |
| Returning a persistence-layer entity (`*Entity` from `DbModels/`) past the DataLayer boundary — e.g. as a controller / handler / repository return type | DataLayer projects | Map to a domain type at the DataLayer boundary (`*Mapper.ToDomain`). Consumers see domain types only — see §5 and §6. Leaking an entity bypasses lazy-loading control, change-tracking lifetime, and JSON serialisation contracts. |
| Splitting a schema change across multiple commits (entity in one, `DbContext` registration in another, EF migration in a third) | DataLayer changes | Single PR: entity class + `DbContext` `DbSet<>` + `EntityTypeConfiguration` + EF migration land together. The reviewer sees the full schema delta in one diff; rollback is one revert. |

---

## 16. Workflow checklist (run for every task)

Before declaring a task done, you must:

0. ✅ **Before the first code edit in the session**, open this guide and the section(s)
     relevant to the change (e.g. §11 for any `logger.Log*`, §3 for handlers, §10 for
     renames). State the rules you will follow in your reply — one sentence, see root
     `CLAUDE.md` "Evidence-of-consumption rule".
1. ✅ Layer dependencies respected (run mental check from §1).
2. ✅ New services registered through a `ModuleInstaller`, not in `Program.cs`.
3. ✅ Class size, method size, ctor-arg budget within §9.
4. ✅ Public types have XML `<summary>` (English).
5. ✅ Logging uses placeholders + `[{Value}]` brackets — **every** placeholder, including
     forwarded user/exception messages and durations (§11).
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
        // ... orchestration, ≤ 100 lines; otherwise convert to a Story ...
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

## 18. Public module documentation (`docs/<Module>.md`)

Every module under `src/SolTechnology.Core.*` has a companion page in `docs/`. These are the
**user-facing docs** — what a developer reads on GitHub before deciding to pull the package
in. They are technical, dense, and example-driven; not marketing prose, not internal incident
retrospectives.

### Canonical structure (do not reorder, do not invent new top-level sections)

```
## SolTechnology.Core.<Module>
<one-paragraph lead — what the package gives you, in one sentence + one elaboration>

### Features
- bullet 1 — concrete user benefit
- bullet 2 — concrete user benefit
…  (5–9 bullets; each is one observable capability the consumer gets, not an internal detail)

### Registration
<one or two snippets: the one-call happy path, then the lower-level compose path if it exists>

### Configuration
<table of options (Name | Default | Purpose) + one binding snippet; "no config needed" if true>

### Usage
<subsections per capability, each a short prose lead-in (≤ 1 line) + a code example>
<tables for behaviour matrices / mappings>

### Testing
<the testing fixture / helpers this module ships, one snippet with Arrange/Act/Assert>

### Conventions
<bulleted DOs/DON'Ts for consumers of this module — short, imperative>

### What ships in DI   (optional — include when AddXxx registers more than 2 services)
<bulleted list of registered services so consumers can Replace/Decorate>

### Working with AI Agent   (optional — include when the module has a companion skill in `.github/skills/`)
<one-line lead + bullet links to the companion SKILL.md and the relevant ClaudeCodingGuide §, absolute GitHub URLs>
```

Anything that does not fit one of those headings is a sign the doc is drifting — either it
belongs in an **ADR** (`docs/adr/*.md`) or in **inline XML doc** on a public type.

### Hard rules

1. **No essays, no war stories, no incident retrospectives.** "Two timeout systems were a
   confusing source of incidents…" is for ADRs, not user docs. The user doc says what the
   knob does and what the default is.
2. **Features = user-observable benefits, not implementation details.** "RFC 7807 error
   pipeline" ✅ — "Uses `IExceptionStatusCodeMapper` internally with `TryAddSingleton`" ❌
   (that belongs under §What ships in DI or in XML doc).
3. **Lead-in lines under `###` / `####` headings are ≤ 1 sentence.** If you wrote "Configure
   X with automatic Y to do Z:" above a code block — delete it; the code block speaks.
4. **Code examples are runnable.** No `// ...` ellipsis in the middle of a snippet unless it
   replaces unrelated boilerplate. The snippet must compile in the consumer's project as-is.
5. **Tables over prose for matrices.** Status code mappings, default-value tables, option
   defaults — always tables.
6. **No `> Tip:` / `> Note:` blockquote noise.** Either the info matters (then it's a regular
   sentence in the body) or it doesn't (then it's deleted).
7. **One module = one page.** Don't split into sub-pages; don't merge two modules. If the page
   exceeds ~300 lines, the module is doing too much and probably needs splitting in code first.
8. **Cross-links instead of duplication.** When `Api.md` and `Log.md` both touch correlation,
   one is canonical and the other links; the description is not copy-pasted.
9. **Companion to ADRs, not a replacement.** A doc page never explains *why we built it this
   way*; that's `docs/adr/NNN-*.md`. The doc explains *how to use it*.
10. **Companion-skill section uses absolute URLs.** When a module ships an authoring skill under
    `.github/skills/`, link it from a `### Working with AI Agent` section with absolute
    `https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/…` URLs — nuget.org cannot
    resolve repo-relative `.github/` links. The skill is opt-in (the consumer points their agent
    at it), NEVER auto-installed. Delivery policy: [`.github/skills/README.md`](../.github/skills/README.md)
    → "Package-companion skills".

### When refactoring an existing doc

- Strip every "Overview" / "Introduction" / "About" heading — fold the content into the lead.
- Strip every paraphrasing intro under a heading ("Configure X to do Y:" above a code block).
- Pull every numbered "1. … 2. … 3. …" structure that wasn't a real sequence into either a
  table or flat `####` subsections. Numbered headings imply order; if the user can skip them,
  use flat headings.
- Move any "Manual setup", "Behind the scenes", "How it works internally" content into either
  XML doc on the type or an ADR; the user doc keeps only what the consumer touches.

### Reference implementation

`docs/Api.md` is the canonical example. When in doubt, compare structure against it. Other
module docs (`Log.md`, `Bus.md`, `HTTP-Production-Checklist.md`) are migrating toward this
shape — bring them in line when you touch the surrounding module.

---

## 19. AI-only documentation (`CLAUDE.md`, this guide, `SKILL.md`)

A third class of docs lives in this repo: files read **exclusively by AI agents**
(`CLAUDE.md` at root, this guide, every `.github/agents/*.agent.md`, every
`.github/skills/*/SKILL.md`). They are not user-facing; they are not narrative; they are the
agent's operational and convention memory. Optimise them for four things, in this order:

1. **Routing speed** — the agent must find the rule for the task in the first ~N tokens.
2. **Compliance verification** — both agent and reviewer must be able to point at "you
   broke §X" without ambiguity.
3. **Token efficiency** — every sentence competes with code for context window space.
4. **Self-update loop** — agents must be able to append rules without breaking sections
   that other docs / ADRs / skills cite.

Everything else (prose, motivation, history) is waste.

### Three-layer hierarchy (one role per file)

| File | Role | Size budget |
|---|---|---|
| `CLAUDE.md` (root) | Operational protocol: how the agent behaves (pre-flight, tool usage, forbidden actions, dependency management, self-improvement routing). | ≤ 300 lines |
| `docs/ClaudeCodingGuide.md` (this) | Conventions: what the agent writes (project structure, CQRS, naming, logging, anti-patterns, doc shape). | indexable; sections are stable cite-targets |
| `.github/skills/*/SKILL.md` | One task, end-to-end (code-review, premortem, planning). Loaded on demand. | as short as the task allows |

One rule = one place. Other files **link**, never copy.

### Hard rules

#### A. Form and tone

1. **Imperative, present tense.** "Use primary constructors." not "You should consider
   using primary constructors". "Never throw in `TellStory()`." not "Throwing here is
   usually a bad idea".
2. **One term per concept.** If you say "chapter", do not later say "step" / "stage" /
   "phase". LLMs split semantics on synonyms and hallucinate distinctions.
3. **`MUST` / `NEVER` / `PREFER` in caps** for critical rules; ordinary text for the
   rest. The caps build a force hierarchy without prose.
4. **No history.** "We used to have a `RegisterCommands` overload that…" → delete.
   State the current rule. War stories go to ADRs.
5. **No marketing / praise.** "Beautifully designed Tale Code framework…" → delete.
   The agent does not need motivation; it needs the rule.
6. **No `> Note:` / `> Tip:` / `> IMPORTANT:` blockquotes.** Either the info matters
   (regular sentence) or it doesn't (delete). Same rule as §18.6.

#### B. Structure

7. **Stable section numbering.** §0, §1, …, §N. Numbers are cite-targets — `CLAUDE.md`,
   ADRs and skills reference "§9.11", "§18", "§4". **Never renumber existing sections.**
   New rules append at the end; if a section grows beyond cohesion, split it but keep
   the old number with a "moved to §M" pointer for one release.
8. **Front-load.** Rules with the highest cost of violation (layer boundaries, secrets
   in config, missing primary ctor, forbidden actions) go to §0–§3. Edge cases at the
   end.
9. **Decision tree at the entry.** First section answers "what am I writing?" in 5–7
   points. The agent routes before the first edit.
10. **Tables over prose for matrices.** Layer → types, exception → status, topic →
    source-of-truth, anti-pattern → fix. Same rule as §18.5 — agents parse tables
    faster than paragraphs.
11. **Workflow / pre-yield checklist at the end.** `- [ ]` list the agent ticks before
    handing control back. Lets the agent self-verify.

#### C. Concrete over abstract

12. **BAD/GOOD code pairs for every non-trivial rule.** Few-shot is the strongest signal
    for an LLM — stronger than the prose rule above it. "Comments ≤ 1 line" + ❌/✅
    block beats an essay every time.
13. **Cite-able names.** Types, files, options, methods — always in `backticks`,
    exact-string-searchable. "Inject `ILogger<TSelf>`" not "inject a logger of the
    self type".
14. **Anti-patterns with locations.** "`CalculateBestPathController.CalculateBestPathV1`
    has `try/catch + JsonConvert`" — the agent knows where to fix it on the next pass.
    Mythical "somewhere in the codebase" → delete.
15. **Concrete numbers, not adjectives.** "≤ 100 lines, hard cap 150." not "classes
    should be small". LLMs have no intuition for "small".

#### D. Agent-specific mechanics

16. **Evidence-of-consumption rule.** Before the first code-writing tool call in a
    session, the agent **must** cite which sections it consulted (`CLAUDE.md §0`).
    Without it, the agent silently forgets the rule by turn 4.
17. **Forbidden-action list.** Explicit list of actions requiring user confirmation
    (rename public symbols, bump majors, edit ADRs, push to master, mask CVEs). See
    `CLAUDE.md §1`.
18. **Tool-usage hints next to the rule that needs the tool.** "After editing a file,
    call `get_errors`." — the agent knows it is part of the protocol, not a suggestion.
19. **Self-improvement clause with explicit triggers.** List the events that mandate
    an update (user correction, discovered constraint, repeated mistake, new ADR) →
    agent updates the guide *in the same turn* before yielding. See §20 below.
20. **Token-budget hint.** Single section ≤ ~150 lines. Above that the LLM starts
    losing earlier fragments under resampling. If a section grows — split it or move
    detail into a skill.

#### E. Cross-file discipline

21. **Cross-link, never copy-paste.** Logging convention lives in §11 of this guide.
    `CLAUDE.md` says "follow §11 for any `logger.Log*`". The `code-review` skill says
    "check §11". One source of truth.
22. **Skills are situational, not pre-loaded.** Not every rule lives in the guide.
    Skills (`code-review`, `premortem`, `implementation-planning`) load on demand —
    that saves tokens in sessions that don't need them. Read the `SKILL.md` before
    invoking; never infer from the skill's name.

#### F. What to avoid

23. **No emoji decoration** beyond `✅` / `❌` in BAD/GOOD pairs and `- [ ]` in
    checklists. LLMs handle `**bold**` and headings well; piktograms are noise.
24. **No "you can" / "usually" / "in most cases".** Either a rule or an exception.
    Exceptions are explicit: "Exception: legacy test files listed in `CLAUDE.md §X`."
25. **No `TODO` / "we should later".** Either it's an issue in the tracker or it
    doesn't exist. AI-only docs are not a todo board.

### When refactoring an AI-only doc

- Strip every blockquote (`> Note:`, `> IMPORTANT:`, `> 🚨`).
- Replace soft-language ("usually", "consider", "it's good to") with imperative.
- Pull every convention rule out of `CLAUDE.md` into this guide; replace with a
  cross-link in §7's table.
- Collapse repeated triggers (the same evidence-of-consumption rule stated in three
  places) into one canonical section.
- Verify section numbers in this guide and skills still match every cross-reference.

### Reference implementations

- `CLAUDE.md` — operational protocol (≤ 300 lines, §0 pre-flight, §1 forbidden,
  §7 cross-ref table to this guide).
- This guide — conventions (§0 decision tree, stable §0–§N, BAD/GOOD pairs in §9.11,
  §18, §19).
- `.github/skills/code-review/SKILL.md` — single-task skill (front-loaded routing,
  no convention duplication, links to this guide for every rule check).

---

## 20. Self-improvement — keep this guide alive

Whenever you (Claude / the agent) **learn something new** during a task that future iterations
should not have to rediscover, you must update your own instructions immediately, in the same
turn, before yielding back to the user.

Triggers — update the guide when:

- The user corrects you on a convention, naming, structure, or workflow.
- You discover a non-obvious constraint of the codebase (build quirks, framework rule, DI pitfall).
- A repeated mistake gets called out (e.g. forgetting `{}` after `if`, missing primary ctor, partial code dumps).
- A new pattern, helper, or framework addition becomes "the way" to do something here.
- An ADR is written or amended — reflect its rule here in one line + link, **and** update the
  ADR index at [`docs/adr/README.md`](adr/README.md) in the same change ([ADR-006](adr/006-implementation-plan-workflow.md)).

How to update:

1. Find the most relevant section (§0–§17). If none fits, add a new numbered section at the end.
2. Add the rule as a single, imperative bullet — short, concrete, copy-pasteable. No prose essays.
3. If the lesson is broad enough to affect *all* tasks, also add it to the §16 workflow checklist.
4. If it is repository-wide (not Story/CQRS/etc. specific), mirror a one-liner into root `CLAUDE.md`.
5. Mention the update in your reply to the user (one sentence: *"Added rule X to §N."*).

Do **not** wait to be told to update the guide — silent retention is forbidden. If a lesson is
worth remembering for next time, it is worth writing down now.

