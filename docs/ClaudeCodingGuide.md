# Claude Coding Guide — SolTechnology.Core / DreamTravel

Convention layer for the agent. Defines **what** the agent writes: project structure,
CQRS, naming, logging, tests, documentation shape. Operational behaviour (pre-flight,
behavioral core, tool usage, forbidden actions, dependency management) lives in the
root [`CLAUDE.md`](../CLAUDE.md). AI-doc authoring rules live in
[`AIDocsGuide.md`](AIDocsGuide.md). One source of truth per topic — when in doubt,
link, don't copy.

Section numbers (§0–§N) are stable cite-targets. `CLAUDE.md`, ADRs and skills reference
them by number; NEVER renumber an existing section. New rules append at the end.

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
    DreamTravel.DomainServices/   ← reusable domain operations + Tales spanning multiple commands
    DreamTravel.Workflows/        ← long-running interactive Tales
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

### Layer references (enforced — NEVER violate)

| Layer | May reference |
|---|---|
| `Presentation` | `LogicLayer`, `DataLayer`, `Infrastructure`, `Domain`, `SolTechnology.Core.*` |
| `LogicLayer` | `DataLayer`, `Infrastructure`, `Domain`, `SolTechnology.Core.*` |
| `DataLayer` | `Infrastructure`, `Domain`, `SolTechnology.Core.*` |
| `Infrastructure` | `Domain`, `SolTechnology.Core.*` |
| `Domain` | nothing (no EF, no MediatR, no Tale, no logging) |

References MUST point only downward in this table. If you need `LogicLayer` from
`DataLayer`, the abstraction is in the wrong layer — move it.

### When to create a new project

Create a new `.csproj` only if **all** apply:

- It represents a distinct external system, bounded context, or deployable unit.
- It has its own `ModuleInstaller.cs`.
- It would otherwise force two unrelated concerns into one assembly.

Otherwise add a folder inside an existing project. NEVER split on technical grounds
(e.g. "Models project" / "Helpers project") — split on responsibility.

### Interface + implementation co-location

When a class is ≤ 80 lines total (including the interface), put the interface and its
single implementation in the **same file**. Name the file after the implementation
(e.g. `RedisCache.cs` contains both `IRedisCache` and `RedisCache`). Split into
separate files only when the implementation exceeds 80 lines or there are multiple
implementations.

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
        services.AddSolTale();        // SolTechnology.Core.Tale scans chapters & handlers
        services.AddTransient<ITSP, AntColony>();
        return services;
    }
}
```

Rules:

- One `Install<ProjectName>(this IServiceCollection)` extension method per project. Name it after the project domain, not the type — `InstallTripsQueries`, not `AddQueryHandlers`.
- `ModuleInstaller` lives at the project root, never nested.
- `Program.cs` calls **installer-level extension methods only** — module installers
  (`Install*`) and framework-level installers (`AddCache`, `AddMediatR`, CORS, Swagger,
  Auth, versioning, MVC filters, configuration binding). It MUST NOT register
  individual services (`services.AddTransient<IFoo, Foo>()` belongs in a `ModuleInstaller`).
- NEVER call `RegisterCommands` / `RegisterQueries` / `AddSolTale` from `Program.cs` — they use `Assembly.GetCallingAssembly()` and must be invoked from inside the assembly that owns the handlers.
- Decorators go in the installer right after the registration they decorate (`services.Decorate(typeof(IGoogleHTTPClient), typeof(GoogleHTTPClientCachingDecorator));`).

---

## 3. CQRS — Commands and Queries

### File layout per use case

One folder per use case. Folder name = use case name. Inside the folder:

```
LogicLayer/DreamTravel.Queries/CalculateBestPath/
  CalculateBestPathQuery.cs        ← input + validator (one file)
  CalculateBestPathResult.cs       ← output DTO
  CalculateBestPathContext.cs      ← Tale context (only if it's a Tale)
  CalculateBestPathTale.cs         ← TaleHandler implementation
  Chapters/
    0.InitiateContext.cs
    1.DownloadRoadData.cs
    2.FindProfitablePath.cs
    3.SolveTsp.cs
    4.FormCalculateBestPathResult.cs
```

For a simple (non-Tale) handler the folder shrinks to:

```
FetchTraffic/
  FetchTrafficCommand.cs
  FetchTrafficHandler.cs
  FetchTrafficResult.cs    (only if there is output)
```

### Rules

- **Query / Command class** holds *only* the input DTO + its `AbstractValidator<>` in the same file. No logic.
- **Result class** is a plain DTO — no behavior, no nullable mystery.
- **Handler** implements `IQueryHandler<,>` or `ICommandHandler<>` from `SolTechnology.Core.CQRS`. Always returns `Result` / `Result<T>`. NEVER throws for business failures.
- **Validators** are `AbstractValidator<TInput>` and live in the same file as the input. They are auto-discovered by `RegisterCommands()` / `RegisterQueries()`.
- Tale threshold: the single criterion lives in §4 (MUST when > 100 lines of business
  logic OR > 1 external system).

### Result pattern

```csharp
return Result<City>.Success(city);
return Result<City>.Fail("City not found");
return Result.Success();
return Result.Fail(new Error { Message = "..." });
```

Implicit conversion is allowed in handlers: `return city;` becomes `Result<City>.Success(city)` automatically. Use it.

**Procedure:** the step-by-step for authoring a command/query/event/tale lives in the
[`command-query-event-tale`](../.github/skills/command-query-event-tale/SKILL.md) skill.

---

## 4. Tale Framework

Threshold (single source of truth):

- **MUST** be a Tale when the handler exceeds **100 lines of business logic** OR talks
  to **more than one external system**.
- **PREFER** a Tale for any multi-step orchestration below that threshold.

### Anatomy

```csharp
public sealed class CalculateBestPathContext : Context<CalculateBestPathQuery, CalculateBestPathResult>
{
    public List<City> Cities { get; set; } = null!;
    // ...accumulator state used across chapters...
}
```

```csharp
protected override Tale<CalculateBestPathResult> Tell() =>
    Open<InitiateContext>()
        .Read<DownloadRoadData>()
        .Read<FindProfitablePath>()
        .Otherwise<JustOrderCities>()
        .Read<SolveTsp>()
        .Read<FormCalculateBestPathResult>()
        .Finale(ctx => ctx.Output);
```

Full `TaleHandler` template: §17.

### Rules

- `Tell()` is the table of contents — it returns a `Tale` that reads top-to-bottom as plain English and contains **no logic**, no `if`, no `try`, no loops. Open the tale with `Open<FirstChapter>()`, chain with `.Read<Chapter>()`, guard with `.Expect(...)`, recover with `.Otherwise<Fallback>()`, and conclude with `.Finale(ctx => ctx.Output)`. If you feel the urge to branch, split the branches into separate chapters or use `.Expect`/`.Otherwise`.
- One chapter = one verb = one file. File names are prefixed with their order: `0.InitiateContext.cs`, `1.DownloadRoadData.cs`. Class names omit the prefix (`InitiateContext`, `DownloadRoadData`).
- Chapter classes inherit `Chapter<TContext>` (automated) or `InteractiveChapter<TContext, TInput>` (user input pause).
- All cross-chapter state lives on the `Context`. Chapters do not share fields, do not call each other.
- **Always** mark chapter classes with `[UsedImplicitly]` — they are resolved via DI.
- Chapters return `Result.Success()` / `Result.Fail(...)`. Chapter code NEVER throws —
  exceptions originate only from DataLayer / Infrastructure / external libraries and
  the framework converts them to errors. Layer rules for throw/catch: §13.
- The `TaleHandler` may also implement `IQueryHandler<,>` / `ICommandHandler<>` so MediatR resolves it directly. This is the standard wiring.

### When to choose `DomainServices` vs Tale-in-Queries/Commands

- **Tale in `Queries/`** — a complex query is **always dedicated to its use case**. NEVER extract a query into a domain service; there is no reuse case that justifies it.
- **Tale in `Commands/`** — a single write triggered by one entry point.
- **Tale in `DomainServices/`** — the orchestration works **directly on domain models** (a save / update / mutation) and is **reused by multiple commands or event handlers** (e.g. `CityDomainService.Save` is reused by the `CitySearched` event handler and import flows). The domain service exposes a plain interface (`ICityDomainService.Save(...)`) and internally inherits `TaleHandler`. Domain services are a write/command-side concept — never a home for queries.

### `Workflows/` project

Reserved for **long-running, interactive, persisted** tales (require a durable `ITaleRepository` — e.g. the DreamTravel sample's `UseTaleRepository<SQLiteTaleRepository>()`, or any `UseTaleRepository<T>()` backend). One folder per workflow, mirroring the CQRS use-case layout (`SampleOrderWorkflow/Chapters/...`).

**Procedure:** authoring a Tale (chapters, contexts, `DomainServices` vs `Workflows` hosting)
is driven by the [`command-query-event-tale`](../.github/skills/command-query-event-tale/SKILL.md)
skill.

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
- **NEVER expose `IQueryable<TEntity>` outside the SQL project.** Consumers receive domain objects.
- Reusable query composition goes into `QueryBuilders/` as `IQueryable<T>` extension methods (`WhereName`, `WhereCoordinates`, `ApplyReadOptions`). Keep them small, single-predicate, and named after what they filter.
- Configuration binding: `services.AddSQL(sqlConfiguration)` from `SolTechnology.Core.SQL` first, then `AddDbContext<>` for project-specific context.
- Migrations live in `Infrastructure/DreamTravelDatabase/`. NEVER put migrations next to the DbContext.

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

`Program.cs` is wiring, not logic. It calls installer-level extensions only (§2). Order:

1. `builder.AddServiceDefaults();` (Aspire).
2. Culture, CORS, configuration binding.
3. **Module installers** (one line per project): `InstallTripsSql`, `InstallGeolocationDataClients`, `InstallInfrastructure`, `InstallDomainServices`, `InstallTripsQueries`, `InstallGraphDatabase`, `AddFlows`, etc.
4. Framework installers: `AddCache`, `AddMediatR`, authentication, versioning, Swagger.
5. Filters: `ExceptionFilter`, `ResponseEnvelopeFilter` (always wired globally).
6. Build, configure pipeline, `MapControllers`, `Run`.

CORS policy names, scheme names, and other constants MUST be `const` with a meaningful name — never placeholder strings like `"dupa"` (§15).

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
- NEVER `try/catch` in a controller — `ExceptionFilter` handles it (§13). NEVER serialize errors manually.
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

- **Unit (`tests/Unit/`)** — only for pure algorithms (e.g. `TravelingSalesmanProblem`), domain invariants, and individual chapters with non-trivial logic. NEVER write a unit test that mocks `IMediator`, `HttpClient`, `DbContext`, or `IRepository` to assert "the handler called X".
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
- Test name format: `Method_Scenario_ExpectedOutcome` (`Execute_ShouldPopulateContextWithRoadData`, `Resume_AfterPause_CompletesTale_AndPersistsTerminalState`).
- One arrange / one act / one assert *block* per test — but multiple related assertions inside the assert block are encouraged (denser tests > more tests).
- **Mark the three blocks with `// Arrange`, `// Act`, `// Assert` comments.** This is the one place where comments restating *what* is allowed (and required) — they delimit the test phases, which makes scanning failures and reviewing tests dramatically faster. Skip a phase only when it is genuinely empty (e.g. a parameterless `// Act` for a static call). Example (`StartStory` / `ResumeStory` are the framework's API names):
  ```csharp
  [Test]
  public async Task Resume_AfterPause_CompletesTale_AndPersistsTerminalState()
  {
      // Arrange
      var input = _fixture.Create<OnboardingInput>();
      var start = await _sut.StartStory<UserOnboardingTale, ...>(input);

      // Act
      var resume = await _sut.ResumeStory<UserOnboardingTale, ...>(start.Data!.TaleId, _userInput);

      // Assert
      resume.IsSuccess.Should().BeTrue();
      resume.Data!.Status.Should().Be(TaleStatus.Completed);
      (await _repo.GetAsync(start.Data.TaleId))!.Status.Should().Be(TaleStatus.Completed);
  }
  ```
- Parameterize with `[TestCase]` / `[TestCaseSource]` instead of duplicating tests.
- A test earns its place only if removing it would let a real regression through. Do not write tests that mirror the implementation shape.

---

## 9. Class-level rules ("small classes, one job")

These apply to every class you write, regardless of layer.

1. **One reason to change.** If you can describe the class with the word "and", split it.
2. **Size budget:** target ≤ 100 lines, hard cap 150. Above that, extract a collaborator.
3. **Method size:** target ≤ 20 lines. Methods longer than that almost always hide a missing abstraction.
4. **Constructor size:** ≤ 5 dependencies. More than five = the class does too much. Move work into a Tale or split the class. (`ILogger<T>` does not count toward the budget.)
5. **Primary constructors** are mandatory for DI capture. NEVER hand-write `private readonly` fields just to assign them.
6. **No statics with state.** Static methods are fine for pure helpers (`CityQueryBuilder`). Static *fields* with mutable state are forbidden outside `const` and `static readonly` lookup tables.
7. **Use .NET 10 `extension` blocks** for extension methods. NEVER write `static class` + `this` parameter — use the C# 14 `extension` syntax instead.
8. **No "Manager", "Helper", "Util" suffixes** unless the class genuinely is a generic helper (rare). Name by responsibility: `CityMapper`, `StreetTrafficUpdater`, `GoogleHTTPClient`.
9. **No `#region`.** Use partial classes (one method per file for HTTP clients) or extract a new class. The only exception is legacy test files explicitly listed in the root `CLAUDE.md`.
10. **Comments earn their place.** Tale Code reads like prose — let names carry the meaning. Write a comment **only** when a reader cannot infer the *why* from the code itself: a non-obvious framework constraint, a workaround for a specific bug/version, an ADR pointer. Hard rule: **one line — two as the absolute exception**. No multi-line narration, no incident retrospectives in `//`, no restating *what* the next line does. If the explanation needs a paragraph, it belongs in an **ADR** (link it: `// See ADR-005.`) or in an **XML `<summary>`** on the public type — not inline. Inline `//` is a *pointer*, not the storage.
    ```csharp
    // ❌ BAD — three-line essay restating what the call does and re-explaining
    //         framework internals everyone can google.
    // Pass the assembly explicitly: the default scanner uses
    // Assembly.GetCallingAssembly() which is unreliable under JIT
    // inlining (and under WebApplicationFactory the entry assembly
    // becomes the test host, not the API).
    services.AddSolTale(assemblies: typeof(SaveCityTale).Assembly);

    // ✅ GOOD — one line, why-not-what, points at the root cause.
    // Explicit: GetCallingAssembly() is unreliable under JIT inlining / WAF.
    services.AddSolTale(assemblies: typeof(SaveCityTale).Assembly);
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

- **Acronyms: ALL CAPS** — `APIClient`, `SQLConfiguration`, `XMLDocument`, `CQRSHandler`, `AUID`, `HTTP`, `UI`, `IO`, `DB`. (See ADR-001.)
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
  - The only acceptable bare placeholder is when the entire message is a single quoted literal that already provides visual delimiters (rare — PREFER `[]` even then).
- Use structured logging placeholders (`{Name}`). NEVER use string interpolation or `+`-concatenation in log messages — both break log aggregation.
- **NEVER** pass an exception's `Message` (or any user-supplied string) as the message *template*: `logger.LogError(ex, ex.Message)` will throw `FormatException` if the text contains `{` / `}`. Always use a placeholder: `logger.LogError(ex, "[{Message}]", ex.Message)`.
- Property names use **PascalCase** (`{OperationName}`, not `{operationName}`) — matches MEL/Serilog/App Insights convention and KQL queries that consumers write.
- Log at the boundaries (handler entry/exit, external call start/end, chapter transitions). Do not log inside tight loops.
- Errors: `logger.LogError(exception, "Message with [{Context}]", ctx);` — pass the exception as the first argument, never `ex.ToString()` inside the message.
- Reusable extension methods that emit common shapes (operation lifecycle, HTTP request lifecycle) live in `SolTechnology.Core.Logging` and are the PREFERred entry point — `_logger.OperationStarted(name)` over hand-rolled templates.

---

## 12. Validation

- Input validation = **FluentValidation** in the same file as the Command/Query.
- Business invariants = inside the domain or chapter, returning `Result.Fail(...)`.
- Defensive parameter checks at module/library boundaries = **`SolTechnology.Core.Guards`**.
- NEVER validate in the controller. NEVER validate in the handler if a validator exists.

---

## 13. Error handling

Layer rule — who may throw, who may catch:

| Layer | Throw | Catch |
|---|---|---|
| Controllers, handlers, chapters, `Tell()`, Domain | NEVER (business failures = `Result.Fail`) | NEVER (`ExceptionFilter` / the Tale framework handle it) |
| DataLayer / Infrastructure (HTTP client methods, repositories) | only for genuinely exceptional conditions (network, IO) | allowed at the external-call boundary, to translate driver exceptions into domain errors |
| External libraries | may throw — out of your control | — |

- Default to `Result` / `Result<T>` everywhere.
- A chapter that fails returns `Result.Fail(...)` — the Tale stops. NEVER throw to abort a Tale; exceptions escaping from DataLayer / external libraries are converted to errors by the framework (§4).
- NEVER swallow exceptions silently. If a `catch` block has no `throw` and no `logger.LogError`, it is a bug.
- For aggregated failures across chapters, use `AggregateError` from `SolTechnology.Core.CQRS`.

---

## 14. Configuration

- Bind config sections to options classes (`SQLConfiguration`, `Neo4jSettings`, `GoogleHTTPOptions`).
- Bind in `Program.cs` and pass the *configuration object* into `Install...` methods — installers MUST NOT call `IConfiguration` directly.
- **Every `AddOptions<T>()` chain MUST end with `.ValidateOnStart()`.** Bad config = host refuses to start. NEVER let a misconfiguration slip through to the first production request.
- Secrets NEVER live in `appsettings.json`. Use environment variables / Aspire / Key Vault.

---

## 15. Anti-patterns observed in this codebase — do not propagate

Real examples spotted in DreamTravel. NEVER copy them. Policy column drives the
surgical-change exception in root `CLAUDE.md` §1.3: **fix on touch** = fix when the
entry sits in code you already edit (separate `chore:` commit); **report only** =
the fix changes an observable contract (log templates, serialization, public types) —
report it, never auto-fix.

| Anti-pattern | Where seen | Correct form | Policy |
| --- | --- | --- | --- |
| Placeholder constant `private static readonly string CorsPolicy = "dupa";` | `DreamTravel.Api/Program.cs` | Meaningful `const` (§7) | fix on touch |
| `try/catch` + `JsonConvert.SerializeObject(ex.Message)` in a controller | `CalculateBestPathController.CalculateBestPathV1` | `ExceptionFilter` handles it (§13) | fix on touch |
| `logger.LogInformation("Skipped " + s.Name);` | `FetchTrafficHandler` | Structured placeholder + `[]` (§11) | report only — changes the log template consumers query |
| Multiple `+`-concatenated log strings | various | Structured placeholders (§11) | report only — changes the log template consumers query |
| Naked `Newtonsoft.Json` usage in new code | `CalculateBestPathController` | `System.Text.Json` + Tale/AUID converters; Newtonsoft only where Hangfire / legacy serialization requires it | report only — changes serialization behaviour |
| Mocking `IMediator` / `DbContext` in unit tests | hypothetical | Component test (§8) | fix on touch |
| Hand-written `private readonly` ctor capture | hypothetical | Primary constructor (§9.5) | fix on touch |
| `static class` + `this` extension methods | various | `extension` block (§9.7) | fix on touch |
| `#region` to organize a class | forbidden | Partial files or new class (§9.9) | fix on touch |
| Multi-line "essay" comment restating *what* the next line does | various | One line, *why* only (§9.10) | fix on touch |
| Returning a persistence-layer entity (`*Entity` from `DbModels/`) past the DataLayer boundary | DataLayer projects | Map at the boundary via `*Mapper.ToDomain` (§5, §6) | report only — changes a public contract |
| Splitting a schema change across multiple commits | DataLayer changes | Single PR: entity + `DbSet<>` + `EntityTypeConfiguration` + EF migration together | fix on touch (process rule) |

---

## 16. Convention checklist (run for every task)

Operational checks (build, tests, `get_errors`, NU warnings, forbidden actions) live
in root `CLAUDE.md` §10 — run both lists. Before declaring a task done:

- [ ] Relevant guide section(s) read and cited in the reply (root `CLAUDE.md` §0).
- [ ] Layer references match the §1 table.
- [ ] New services registered through a `ModuleInstaller`, not in `Program.cs` (§2).
- [ ] Tale threshold respected — MUST-Tale when > 100 lines or > 1 external system (§4).
- [ ] Class size, method size, ctor-arg budget within §9.
- [ ] Public types have XML `<summary>` (English).
- [ ] Logging uses placeholders + `[{Value}]` brackets — **every** placeholder (§11).
- [ ] No `#region`, no placeholder strings, no swallowed exceptions; throw/catch respects the §13 layer table.
- [ ] Comments are one-line *why-not-what* (§9.10).
- [ ] Tests added/updated — Component test preferred, Unit test only for pure logic (§8).

If any item fails, fix it before yielding.

---

## 17. Quick reference — file templates

### Command

Commands implement `IRequest<Result>` so they flow through the MediatR pipeline;
the handler implements `ICommandHandler<>` (which the framework maps onto MediatR).

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
        // ... orchestration; above the §4 threshold convert to a Tale ...
        return Result.Success();
    }
}
```

### Tale query (canonical full template — §4 links here)

```csharp
// CalculateBestPathTale.cs
public class CalculateBestPathTale(IServiceProvider sp, ILogger<CalculateBestPathTale> logger)
    : TaleHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>(sp, logger),
      IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    protected override Tale<CalculateBestPathResult> Tell() =>
        Open<InitiateContext>()
            .Read<DownloadRoadData>()
            .Read<FindProfitablePath>()
            .Otherwise<JustOrderCities>()
            .Read<SolveTsp>()
            .Read<FormCalculateBestPathResult>()
            .Finale(ctx => ctx.Output);
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
        services.AddSolTale();
        services.AddTransient<ITSP, AntColony>();
        return services;
    }
}
```

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

## 19. AI-only documentation — moved

Content lives in [`docs/AIDocsGuide.md`](AIDocsGuide.md). Section number retained as a
cite-target for one release; update your references, then this stub disappears.

---

## 20. Self-improvement — how to append to this guide

Triggers and routing live in root `CLAUDE.md` §9 (single source of truth). When a
lesson routes here:

1. Find the most relevant section (§0–§18). If none fits, add a new numbered section at the end — NEVER renumber existing ones.
2. Add the rule as a single, imperative bullet — short, concrete, copy-pasteable. No prose essays.
3. If the lesson affects *all* tasks, also add it to the §16 checklist.
4. If it is repository-wide (not convention-specific), mirror a one-liner into root `CLAUDE.md` instead.
5. Mention the update in your reply (one sentence: *"Added rule X to §N."*).

---

## 21. Markdown / Mermaid hygiene (all docs)

- Links with spaces in the path: `[Text](<path/file.md>)`.
- Verify every link resolves on disk before printing it.
- Mermaid node labels with spaces use `<br>`: `Node[Name<br>With<br>Spaces]`.
- No issue-tracker IDs (Jira, etc.) unless the user supplies them.