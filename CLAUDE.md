# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **🚨 MANDATORY READING BEFORE WRITING CODE 🚨**
>
> Whenever you write or modify C# code in this repository, you **must** first consult
> [`docs/ClaudeCodingGuide.md`](docs/ClaudeCodingGuide.md). It defines the canonical
> structure, dependencies, naming, file layout, class-size budget, and anti-patterns
> for this codebase (built on the DreamTravel sample). The rules in that file are
> binding and override any contradicting pattern found in legacy code.

> **🔁 SELF-IMPROVEMENT RULE 🔁**
>
> If you learn something new during a task — a correction from the user, a non-obvious
> codebase constraint, a repeated mistake, a new "the way" pattern, an ADR — **update
> your own instructions in the same turn before yielding back**. See
> [`docs/ClaudeCodingGuide.md` §18](docs/ClaudeCodingGuide.md). Silent retention is
> forbidden: if a lesson is worth remembering, write it down now.

## Repository Overview

SolTechnology.Core is a collection of NuGet packages that provide a foundation for building CQRS-driven applications using Azure technologies. The repository follows the "Tale Code" philosophy - making code readable like well-written prose. It includes both the core libraries (in `src/`) and a sample application called DreamTravel (in `sample-tale-code-apps/DreamTravel/`).

**Development Environment:**
- **Platform**: Windows (PowerShell is the primary shell)
- **IDE**: Works with Visual Studio, Rider, VS Code
- **Framework**: .NET 10.0
- **Package Manager**: NuGet
- **Total Test Suite**: 334+ tests across all core libraries

## Project Structure

The solution follows Clean Architecture with strict layer separation:

### Core Libraries (`src/`)

Each library is a separate NuGet package with its own `ModuleInstaller.cs` for dependency registration:

- **SolTechnology.Core.CQRS** - CQRS implementation with MediatR, Result pattern, and Chain handlers
- **SolTechnology.Core.SQL** - SQL database access with Dapper and Entity Framework
- **SolTechnology.Core.BlobStorage** - Azure Blob Storage wrapper
- **SolTechnology.Core.HTTP** - HTTP client abstractions and utilities
- **SolTechnology.Core.MessageBus** - Azure Service Bus messaging
- **SolTechnology.Core.Guards** - Input validation utilities
- **SolTechnology.Core.Authentication** - Basic and API key authentication
- **SolTechnology.Core.Logging** - Logging abstractions
- **SolTechnology.Core.Scheduler** - Cron-based task scheduling
- **SolTechnology.Core.API** - API utilities and filters
- **SolTechnology.Core.Cache** - Caching abstractions
- **SolTechnology.Core.Story** - Story Framework for workflow orchestration with interactive workflows, persistence, and Tale Code philosophy
- **SolTechnology.Core.AUID** - AUID (Application Unique ID) implementation
- **SolTechnology.Core.Faker** - Test data generation

### Sample Application (DreamTravel)

Located in `sample-tale-code-apps/DreamTravel/src/`, organized by layers:

- **Presentation/** - Entry points (API, Worker, UI)
- **LogicLayer/** - Business logic (Commands, Queries, Domain Services, Flows)
- **DataLayer/** - Data access (SQL, HTTP clients, Graph database)
- **Infrastructure/** - Shared infrastructure and service defaults
- **DreamTravel.Trips.Domain** - Domain models
- **DreamTravel.Aspire** - .NET Aspire orchestration

### Tests

- Core library tests: `tests/SolTechnology.Core.*.Tests`
- DreamTravel tests: `sample-tale-code-apps/DreamTravel/tests/`
  - `Unit/` - Unit tests for individual components (backend and UI)
  - `Component/` - Component integration tests
  - `EndToEnd/` - E2E tests

## Development Commands

### Building

```bash
# From repository root
dotnet restore SolTechnology.Core.slnx
dotnet build SolTechnology.Core.slnx
```

### Testing

```bash
# From repository root - runs all core library tests
.\.github\runTests.ps1

# Or manually run tests for each project
cd tests
dotnet test SolTechnology.Core.SQL.Tests --no-build
dotnet test SolTechnology.Core.HTTP.Tests --no-build
dotnet test SolTechnology.Core.Guards.Tests --no-build
```

### Packaging

```bash
# Pack individual library (example)
dotnet pack -c Release -o . ./src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj
```

### Dependency Management

**CRITICAL: Always update packages at source, never mask problems with overrides.**

When encountering NuGet security warnings (NU1902, NU1903, NU1603):

> **Note on `NU1900`** — `NU1900` is *not* a CVE finding. It means NuGet Audit
> could not download vulnerability data (typically because a configured feed is
> unreachable or requires auth, e.g. a corporate Azure DevOps feed behind VPN).
> It is intentionally demoted from error to warning in `src/Directory.Build.props`
> via `<WarningsNotAsErrors>NU1900</WarningsNotAsErrors>` so transient connectivity
> never breaks the build. Real CVE warnings (`NU1901`–`NU1904`) remain errors.
> If you see NU1900 locally, fix the unreachable feed in your user-level
> `NuGet.config` or remove it; do not change the project files.

#### 1. **Identify the Root Cause** (Don't Add Overrides Immediately!)

```bash
# Check which package introduces the vulnerable dependency
dotnet list <project.csproj> package --include-transitive | grep -B3 "<VulnerablePackage>"

# Check for outdated packages
dotnet list <project.csproj> package --outdated
```

#### 2. **Fix at Source** (Preferred Approach)

**Option A: Update the parent package**
```bash
# Example: If Microsoft.AspNetCore.Authentication 2.2.0 brings old dependencies
# Update it to the latest version or remove if unused
dotnet add package Microsoft.AspNetCore.Authentication --version 2.3.0
```

**Option B: Remove unused packages**
```bash
# Check if package is actually used in code
grep -r "PackageName" <project-directory> --include="*.cs"

# If not used, remove it completely
dotnet remove package PackageName
```

**Option C: Migrate to newer SDK**
```bash
# Example: Microsoft.Azure.ServiceBus (old) → Azure.Messaging.ServiceBus (new)
# 1. Update code to use new namespaces
# 2. Remove old package, add new one
dotnet remove package Microsoft.Azure.ServiceBus
dotnet add package Azure.Messaging.ServiceBus
```

#### 3. **Override Only as Last Resort**

Only add direct `PackageReference` overrides when:
- The parent package cannot be updated (breaking changes)
- The parent package is a third-party library you don't control
- Migration to a new package is not feasible

```xml
<!-- Last resort: Override transitive dependency -->
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
</ItemGroup>
```

**Add overrides ONLY in the project that directly references the problematic parent package** - transitive dependencies will propagate to child projects automatically.

#### 4. **Verify the Fix**

```bash
# Build and check for security warnings
dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU1902|NU1903|NU1603"

# Should return nothing if all vulnerabilities are fixed
```

#### Example: Fixing Hangfire Security Warning

❌ **BAD** (Masking the problem):
```xml
<!-- DreamTravel.Commands.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />

<!-- DreamTravel.Queries.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />

<!-- DreamTravel.Infrastructure.csproj -->
<PackageReference Include="Hangfire.Core" Version="1.8.16" />  <!-- Old version! -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

✅ **GOOD** (Fixing at source):
```xml
<!-- DreamTravel.Infrastructure.csproj -->
<PackageReference Include="Hangfire.Core" Version="1.8.22" />  <!-- Updated! -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />  <!-- Only here if still needed -->

<!-- Child projects automatically get the fix through ProjectReference -->
```

**Why this matters:**
- Updating at source fixes the root cause
- Reduces duplicate package references across projects
- Makes future maintenance easier
- Ensures compatibility with the parent package's expectations

## Claude Code Workflow

**IMPORTANT**: When working on tasks, always verify changes by building the solution after completing each task.

### After Every Task:

1. **Build the solution** to catch compilation errors early:
   ```bash
   dotnet build SolTechnology.Core.slnx
   ```

2. **For DreamTravel changes**, build the sample app:
   ```bash
   cd sample-tale-code-apps/DreamTravel
   dotnet build
   ```

3. **Run relevant tests** if the change affects testable logic:
   ```bash
   # Core library tests
   .\.github\runTests.ps1

   # Specific project tests
   cd tests
   dotnet test SolTechnology.Core.AUID.Tests
   ```

**Why**: Building after each task ensures:
- No compilation errors introduced
- Type mismatches caught immediately
- Dependencies are correct
- Changes are compatible with existing code

### Command Permissions and Allow List

**IMPORTANT**: Before asking the user for permission to execute a Bash command, ALWAYS check `.claude/settings.local.json` for similar existing patterns in the `allow` list to avoid unnecessary interaction delays.

**Examples of reusable patterns:**
- `Bash(Select-String -Pattern "...")` - Many patterns already allowed for filtering test output (error, failed, passed, Niepowodzenie, Failed, etc.)
- `Bash(Select-Object -Last N)` - Multiple variants exist (Last 5, 10, 20, 30)
- `Bash(timeout N dotnet test:*)` - Several timeout variants exist (60, 120, 180, 300 seconds)
- `Bash(dotnet *:*)` - Most dotnet commands are pre-approved

**How to check:**
1. Before executing a command that might need permission, mentally check if a similar pattern exists in the allow list
2. Look for wildcard patterns (`*`) that might match your command
3. For `Select-String` commands, check if a similar pattern exists even with different search terms
4. For `Select-Object` commands, use existing `-Last N` values if available
5. Prefer using existing approved patterns over asking for new permissions

**Example decision tree:**
- Need to filter test output for "Success"? → Check if `Select-String -Pattern "..."` with similar terms exists (passed, failed, etc.)
- Need to show last 15 lines? → Use existing `Select-Object -Last 10` or `Last 20` instead of asking for `-Last 15`
- Need to run tests with timeout? → Use existing timeout values (60, 120, 180, 300) instead of custom values

This reduces user interruptions and speeds up development workflow.

## Architecture Patterns

### CQRS Pattern

Commands and Queries are strictly separated using MediatR:

**Commands** (write operations):
- Implement `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResult>`
- Focus on data consistency over performance
- Must be retryable and validate input
- Return `Result` or `Result<T>`

**Queries** (read operations):
- Implement `IQueryHandler<TQuery, TResult>`
- Optimize for performance
- Return `Result<T>`

**Registration**:
```csharp
services.RegisterCommands();  // Registers all command handlers in calling assembly
services.RegisterQueries();   // Registers all query handlers in calling assembly
```

### Result Pattern

All operations return `Result` or `Result<T>` for explicit success/failure handling:

```csharp
// Success
return Result<City>.Success(city);

// Failure
return Result<City>.Fail("City not found");
return Result<City>.Fail(new Error { Message = "..." });

// Implicit conversion
City city = GetCity();
return city;  // Automatically converts to Result<City>
```

### Story Framework Pattern

**Story Framework** is the unified approach for workflow orchestration, replacing the deprecated Chain pattern. It supports both simple automated workflows and interactive workflows with persistence.

**Key Concepts:**
- **StoryHandler** — main orchestrator, inherits from `StoryHandler<TInput, TContext, TOutput>`.
- **Context** — state object that carries data through the story, inherits from `Context<TInput, TOutput>`.
- **Chapter** — individual step, inherits from `Chapter<TContext>` (or implements `IChapter<TContext>`).
- **InteractiveChapter** — chapter that pauses for user input, inherits from `InteractiveChapter<TContext, TChapterInput>`.

**Basic Story (automated workflow):**
```csharp
public class OrderProcessingStory : StoryHandler<OrderInput, OrderContext, OrderOutput>
{
    public OrderProcessingStory(IServiceProvider sp, ILogger<OrderProcessingStory> logger)
        : base(sp, logger) { }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidateOrderChapter>();
        await ReadChapter<ProcessPaymentChapter>();
        await ReadChapter<ShipOrderChapter>();

        Context.Output.OrderId = Context.ProcessedOrderId;
    }
}
```

**Interactive Story (interactive workflow with persistence):**
```csharp
public class UserOnboardingStory : StoryHandler<OnboardingInput, OnboardingContext, OnboardingOutput>
{
    protected override async Task TellStory()
    {
        await ReadChapter<CollectBasicInfoChapter>();   // Pauses for user input
        await ReadChapter<VerifyEmailChapter>();        // Pauses for email verification
        await ReadChapter<SetupPreferencesChapter>();   // Pauses for preferences
        await ReadChapter<CompleteOnboardingChapter>(); // Automated
    }
}

public class CollectBasicInfoChapter : InteractiveChapter<OnboardingContext, UserBasicInfo>
{
    public override Task<Result> ReadWithInput(OnboardingContext context, UserBasicInfo userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name))
            return Result.FailAsTask("Name is required");

        context.UserName  = userInput.Name;
        context.UserEmail = userInput.Email;
        return Result.SuccessAsTask();
    }
}
```

**Registration:**
```csharp
// Default: in-memory persistence — supports both automated AND interactive stories.
// Ideal for dev, tests, single-process apps. Registers StoryManager + InMemoryStoryRepository.
services.RegisterStories();

// Production: durable SQLite persistence.
services.RegisterStories(StoryOptions.WithSqlitePersistence("stories.db"));

// Opt-out: no repository, no StoryManager. Automated TellStory() flows only.
// Running an InteractiveChapter fails with a clear error message.
services.RegisterStories(StoryOptions.WithoutPersistence());

// Scan additional assemblies (MediatR-style)
services.RegisterStories(StoryOptions.WithInMemoryPersistence(),
    typeof(UserOnboardingStory).Assembly);
```

`RegisterStories` registers `IChapter<>` implementations, `StoryHandler<,,>` subclasses,
`StoryHandlerRegistry` (controller whitelist), and — when persistence is enabled — the
configured `IStoryRepository` plus `StoryManager`.

**Behavioral breaking change:** `RegisterStories()` without arguments now defaults to
in-memory persistence (previously no persistence). Use `StoryOptions.WithoutPersistence()`
to recover the old behavior.

**Usage with StoryManager (for interactive workflows):**
```csharp
var start = await storyManager.StartStory<UserOnboardingStory, OnboardingInput, OnboardingContext, OnboardingOutput>(
    input,
    idempotencyKey: Request.Headers["Idempotency-Key"]);

var storyId = start.Data!.StoryId;

// Resume with user input
var userInput = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"john@example.com\"}");
var resume = await storyManager.ResumeStory<UserOnboardingStory, OnboardingInput, OnboardingContext, OnboardingOutput>(
    storyId,
    userInput.RootElement);

// Cancel a paused story
await storyManager.CancelStory(storyId);
```

**Pause / cancel errors are typed markers — detect by type, not by string:**

```csharp
if (result.Error is StoryPausedError p)         { /* paused at p.ChapterId */ }
if (result.Error is StoryCancelledError)        { /* cancelled */ }
```

**Tale Code philosophy:** Stories read like prose — `TellStory()` narrates what happens; chapters are named as actions (verbs); the flow is linear and obvious.

### ModuleInstaller Pattern

Each library exposes a `ModuleInstaller.cs` with extension methods for service registration. Always use these when integrating libraries into projects.

### Validation

Uses FluentValidation integrated into MediatR pipeline. Create validators for commands/queries:

```csharp
public class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}
```

Validators are automatically discovered and executed when registered via `RegisterCommands()` or `RegisterQueries()`.

## Code Style Conventions

1. **Tale Code Philosophy**: Code should read like prose - prioritize clarity and readability
2. **Warnings as Errors**: `TreatWarningsAsErrors` is enabled in `Directory.Build.props`
3. **Target Framework**: .NET 10.0
4. **Solution Format**: `.slnx` (XML-based solution file format)
5. **Nullable Reference Types**: Enabled across all projects
6. **Implicit Usings**: Enabled
7. **No Regions**: NEVER use `#region` directives - if code needs separation, use separate classes or partial classes instead
   - ❌ BAD: `#region Private Methods` / `#region Test Data`
   - ✅ GOOD: Create separate class or use partial class
   - **Exception**: Test files with existing regions (like `AdvancedScenariosTests.cs`) may keep them for organizing test categories
8. **Logging Values**: Always wrap variable values in square brackets `[]` to make empty values visible
   - ❌ BAD: `_logger.LogInformation($"Processing order {orderId}")`
   - ✅ GOOD: `_logger.LogInformation($"Processing order [{orderId}]")`
   - This makes it clear when a value is empty: `"Processing order []"` vs `"Processing order "`
9. **Acronym Casing**: Use ALL CAPS for all acronyms (Python PEP 8 style)
   - **All acronyms**: ALL CAPS regardless of length → `UUID`, `API`, `SQL`, `HTML`, `XML`, `UI`, `IO`, `DB`, `CQRS`, `AUID`
   - **In class names**: `UUIDGenerator`, `APIClient`, `SQLConnection`, `HTMLParser`, `XMLDocument`
   - **In namespaces**: `SolTechnology.Core.API`, `SolTechnology.Core.SQL`, `SolTechnology.Core.HTML`, `SolTechnology.Core.CQRS`
   - Examples:
     - ✅ GOOD: `APIClient`, `XMLDocument`, `HTMLHelper`, `SQLConnection`, `UUIDGenerator`, `CQRSHandler`
     - ❌ BAD: `ApiClient`, `XmlDocument`, `HtmlHelper`, `SqlConnection`, `UuidGenerator`, `CqrsHandler`
   - **Rationale**: Follows Python PEP 8 convention - acronyms are visually distinct and immediately recognizable
   - **Note**: Existing published packages (`SolTechnology.Core.CQRS`, `SolTechnology.Core.AUID`) keep their current names for backwards compatibility
10. **Testing Framework**:
   - Use NUnit for all tests
   - For integration tests, use WebApplicationFactory and Testcontainers
   - **Prefer fewer, denser tests over many shallow ones** — see `Testing Philosophy` section for full guidance
   - **For DreamTravel: prefer full integration tests** (real API host + Testcontainers for SQL/ServiceBus/etc.) over unit tests that mock MediatR or `HttpClient`
   - Parameterize with `TestCase` / `TestCaseSource` instead of duplicating test methods
   - **Always delimit test phases with `// Arrange`, `// Act`, `// Assert` comments** — the one place where comments restating *what* is mandatory (they make failure triage and review much faster). Skip a phase only when genuinely empty. See `docs/ClaudeCodingGuide.md` §8.
   - A test earns its place only if its removal would let a real regression through
11. **Validation Framework**: Use FluentValidation for all input validation
12. **String composition**:
   - **Avoid `+` concatenation chains and mixed `$"..." + "literal"`** — every `+` between a non-constant and another string is a separate runtime allocation (`string.Concat` over the result of `string.Format` / interpolation handler). For multi-line messages prefer a single interpolated raw string literal:
     ```csharp
     // ❌ BAD — N+1 allocations: one for the interpolation, one per `+`
     throw new InvalidOperationException(
         $"Chapter '{id}' is interactive and requires persistence. " +
         "Register a repository via the builder, e.g. " +
         ".UseInMemoryStoryRepository() (default).");

     // ✅ GOOD — single allocation via raw interpolated string
     throw new InvalidOperationException(
         $"""
         Chapter '{id}' is interactive and requires persistence.
         Register a repository via the builder, e.g.
         .UseInMemoryStoryRepository() (default).
         """);
     ```
   - For purely-literal multi-line text use a raw string literal `"""..."""` (no interpolation overhead at all — the compiler emits one constant).
   - For dynamic loops use `StringBuilder` or `string.Create` / `string.Concat(IEnumerable<string>)` — never `result += item` in a loop.
   - Compile-time constant folding only kicks in for `"a" + "b"` where **both** sides are literals; the moment one side is interpolated or a variable, you pay the allocation.
13. **Documentation Language**: All documentation must be written in English
   - **Includes**: ADRs, README files, package documentation (*.md files), XML comments, code comments
   - **Exception**: User-facing UI text and error messages can be localized
   - **Rationale**: Ensures consistency and accessibility for international developers
14. **Primary constructors**: Always prefer C# 12 primary constructors for classes whose only ctor responsibility is dependency capture. Stop hand-writing `private readonly` field + assigning constructor when the compiler will do it for free.
   ```csharp
   // ❌ BAD — boilerplate, four lines for one idea
   public sealed class StoryManager
   {
       private readonly IServiceScopeFactory _scopeFactory;
       private readonly ILogger<StoryManager> _logger;

       public StoryManager(IServiceScopeFactory scopeFactory, ILogger<StoryManager> logger)
       {
           _scopeFactory = scopeFactory;
           _logger = logger;
       }
   }

   // ✅ GOOD — primary constructor; parameters are in scope inside the class body
   public sealed class StoryManager(
       IServiceScopeFactory scopeFactory,
       ILogger<StoryManager> logger)
   {
       // use `scopeFactory` / `logger` directly inside methods
   }
   ```
   - Applies to `class`, `struct`, `record`, `record struct`. For `record`, primary constructor parameters become public properties — that's the intended behavior.
   - When you need additional ctors (validation, defaults), keep the primary one and add `: this(...)` overloads.
   - Don't redeclare a primary constructor parameter as a field/property unless you need a different lifecycle (e.g. lazy init, mutability, explicit `[FromKeyedServices]`).
   - When inheriting, forward through the base primary constructor: `class Foo(Bar bar) : Base(bar)`.
15. **Always brace `if`/`else`/`for`/`foreach`/`while`/`using`** — even single-statement bodies. No exceptions for "short" lines, no exceptions for early-returns. Bracing is non-negotiable because:
   - One-liners silently break when a second statement is added (the classic Apple `goto fail;` bug).
   - Diff churn when adding logging/asserts later is contained to one line instead of reshaping the block.
   - Code reviewers don't have to mentally parse indentation as control flow.
   ```csharp
   // ❌ BAD
   if (input == null) return BadRequest();
   foreach (var x in items) Process(x);

   // ✅ GOOD
   if (input == null)
   {
       return BadRequest();
   }
   foreach (var x in items)
   {
       Process(x);
   }
   ```
   - The only acceptable single-line forms are expression-bodied members (`=>`) and ternary expressions, because those are *expressions*, not control flow.
16. **Comments are why-not-what, ≤ 2 lines.** Tale Code reads like prose — names carry the meaning. Write a comment only when a reader cannot infer the *why* from the code (non-obvious framework quirk, workaround for a specific bug/version, ADR pointer). No essays, no restating the next line. If the explanation needs a paragraph, it belongs in an ADR or XML doc on the public type. See `docs/ClaudeCodingGuide.md` §9.11.

## Important Implementation Notes

### Clean Architecture Layer Dependencies

Dependencies flow in one direction only (from top to bottom):
- Presentation → Logic → Data → Infrastructure
- Domain has no dependencies
- Never reference higher layers from lower layers

### CQRS Handlers Structure

Handlers should contain minimal code - they orchestrate executors/steps:
- **Query/Command** - Input model with validation
- **Handler** - Orchestrates the flow (for complex workflows, use Story Framework)
- **Executors** - Actual implementation logic
- **Result** - Output model

For multi-step workflows with complex orchestration, prefer Story Framework over putting logic directly in handlers.

### Pipeline Behaviors

MediatR pipeline includes:
1. `LoggingPipelineBehavior` - Automatic logging
2. `FluentValidationPipelineBehavior` - Automatic validation

Both are registered automatically when using `RegisterCommands()` or `RegisterQueries()`.

### Error Handling

- Use `Result` pattern - avoid throwing exceptions for business logic failures
- Exceptions are caught in Story chapters and converted to `Error`
- Use `AggregateError` for multiple errors in Story operations

### Testing Philosophy

**Core principle: fewer, denser tests — not more, shallower ones.**

Prefer a small number of tests that each exercise a meaningful slice of behavior end-to-end over a large number of tests that each assert a single trivial fact. One test that drives a full pause → persist → resume → complete cycle is worth more than five tests that each poke at a single property.

**Guidelines:**

1. **Consolidate, don't multiply.** If three tests share 80% of their arrange/act and differ only in one assertion, merge them — or parameterize with `TestCase` / `TestCaseSource`.
2. **Test behavior, not shape.** Don't write tests that mirror the implementation (e.g. "property X has a setter"). Test observable outcomes: persisted state, returned `Result`, logged events, HTTP status, DB rows.
3. **Each test must earn its place.** A test is worth writing only if removing it would let a real bug slip through. Coverage-padding tests are noise — they slow the suite and obscure real failures.
4. **Prefer wide scenarios over narrow unit tests** for workflow-like features (Story Framework, CQRS pipelines, chains). One realistic flow catches integration bugs that unit tests cannot.
5. **Name tests by the scenario.** `Resume_AfterPause_CompletesStory_AndPersistsTerminalState` beats `ResumeStory_ReturnsSuccess`.

**Bar for adding a new test:**

- Does it cover a scenario no existing test covers?
- Would it fail on a real regression?
- Is the assertion specific enough that the failure message tells you what broke?

If any answer is "no", extend an existing test instead.

**Comprehensive QA — dense, not bloated.** Cover these dimensions with a small set of dense tests:

1. Happy path + primary error paths in the same class, ideally parameterized.
2. Edge cases (empty, null, extremes, Unicode) consolidated into one or two `TestCaseSource`-driven tests.
3. Concurrency and state transitions — one realistic scenario per transition, end-to-end.
4. Persistence failures — injected via fake repository, one test per failure mode.

**Example from Story Framework tests:**
```csharp
[Test]
public async Task Resume_WithExtremelyLongStrings_ShouldHandleOrReject()
{
    var longString = new string('A', 10000);
    var input = JsonDocument.Parse($"{{\"name\": \"{longString}\", ...}}");
    var result = await storyManager.ResumeStory(..., input.RootElement);

    if (result.IsFailure)
        result.Error.Message.Should().ContainAny("too long", "length", "maximum");
}
```

### Integration Tests — Preferred for DreamTravel

**For DreamTravel, strongly prefer full integration tests over isolated unit tests.**

Unit tests on DreamTravel handlers, executors, and domain services have marginal value — they mostly re-test MediatR, AutoMapper, and FluentValidation plumbing. Real bugs live at the seams: SQL, HTTP clients, Service Bus, Story persistence, Aspire wiring. Those seams are exercised only by integration tests.

**What "full integration test" means:**

- `WebApplicationFactory<TEntryPoint>` boots the real API or Worker pipeline — controllers, MediatR, validators, DI, configuration.
- **Testcontainers** for real infrastructure: SQL Server, Service Bus emulator, Azurite, Redis. No infra mocks.
- Full request path: HTTP in → validation → handler → executors → SQL/HTTP/bus → Result → HTTP response.
- Seeds and asserts against the real database, not an in-memory substitute.
- Story-driven flows: start a story through the API, assert the persisted `StoryInstance`, resume through the API, assert terminal state and side effects.

**When unit tests are still justified in DreamTravel:**

- Pure algorithmic code with many input permutations (e.g. `TravelingSalesmanProblem` solver) — parameterized unit tests are appropriate and much faster than containers.
- Domain invariants on value objects and entities.
- Edge cases that are expensive or non-deterministic to reproduce at integration level.

**Anti-patterns to avoid in DreamTravel tests:**

- Unit tests that mock `IMediator`, `IDbConnection`, or `HttpClient` to assert that a handler "calls" them — this tests nothing real.
- One test class per handler with trivial `Handle_ReturnsSuccess` tests.
- "Integration" tests that mock out the database or external APIs — at that point it's a unit test in disguise.

**Layout signal:** `tests/Component/` and `tests/EndToEnd/` are where the value is. `tests/Unit/` stays small and targeted — not the default home for new tests.

## CI/CD Pipelines

This repository uses both GitHub Actions and Azure DevOps for CI/CD.

### GitHub Actions (Core Libraries)

**Location**: `.github/workflows/publishPackages.yml`

**Scope**: Core NuGet packages (`src/SolTechnology.Core.*`)

**Triggers**: Push/PR to `master` branch

**Steps**:
1. Setup .NET 10.0.x SDK
2. Restore workload: `dotnet workload restore SolTechnology.Core.slnx`
3. Restore dependencies: `dotnet restore SolTechnology.Core.slnx`
4. Build: `dotnet build SolTechnology.Core.slnx --no-restore`
5. Test: Run `.github/runTests.ps1` (tests all projects in `tests/` directory)
6. Pack all core libraries (SolTechnology.Core.*)
7. Publish to NuGet.org (master branch only, requires `NUGET_API_KEY` secret)

**Test Script**: `.github/runTests.ps1`
```powershell
ForEach ($folder in (Get-ChildItem -Path tests -Directory)) {
    dotnet test --no-build $folder.FullName
}
```
Tests covered:
- `tests/SolTechnology.Core.AUID.Tests` (91 tests)
- `tests/SolTechnology.Core.HTTP.Tests` (1 test)
- `tests/SolTechnology.Core.Guards.Tests` (150 tests)
- `tests/SolTechnology.Core.SQL.Tests` (1 test)
- `tests/SolTechnology.Core.Story.Tests` (91 tests - includes comprehensive QA scenarios)

### Azure DevOps (DreamTravel Sample)

**Location**: `sample-tale-code-apps/DreamTravel/devOps/pipelines/`

#### 1. Build & Test Pipeline
**File**: `build&test.yml`

**Scope**: DreamTravel sample application

**Triggers**: Push to `master` branch

**Stages**:

**Stage 1: Test**
- Setup .NET 10.0.x SDK
- Start SQL Server container (Docker)
- Deploy database locally using DreamTravelDatabase project
- Run unit tests: `tests/**/*UnitTests.csproj`
- Run integration tests: `tests/**/*IntegrationTests.csproj`
- Run component tests: `tests/**/*Component.Tests.csproj`

Tests covered:
- `DreamTravel.Trips.Commands.UnitTests`
- `DreamTravel.Trips.Queries.UnitTests`
- `DreamTravel.Trips.TravelingSalesmanProblem.UnitTests`
- `DreamTravel.Trips.GeolocationDataClients.IntegrationTests`
- `DreamTravel.Component.Tests`

**Stage 2: BuildAndPublish** (only on non-PR builds)
- Publish infrastructure templates
- Publish Database project
- Publish DreamTravel.Api
- Publish DreamTravel.Worker
- Upload artifacts

**Variables required**:
- Variable group: `dream-travel-test` (for Test stage)

#### 2. E2E Tests Pipeline
**File**: `e2etests.yml`

**Scope**: End-to-end tests for DreamTravel

**Triggers**: Manual or called from other pipelines

**Steps**:
- Setup .NET 10.0.x SDK
- Run E2E tests: `tests/**/*E2E.Tests.csproj`

Tests covered:
- `DreamTravel.E2E.Tests`

**Note**: E2E pipeline is separate and can be triggered independently or as part of a larger workflow.

#### 3. Deploy Pipeline
**File**: `deploy.yml`

**Scope**: Azure deployment for DreamTravel

#### 4. Trigger External Pipeline
**File**: `triggerExternalPipeline.yml`

**Scope**: Template for triggering and monitoring external pipelines

### Important Notes

1. **SDK Version Consistency**: All pipelines must use .NET 10.0.x to match the `TargetFramework` in `Directory.Build.props`

2. **Test Separation**:
   - GitHub Actions: Tests core libraries only (`tests/` directory at root)
   - Azure DevOps: Tests DreamTravel sample app (`sample-tale-code-apps/DreamTravel/tests/`)

3. **When Updating SDK Version**: Update in ALL pipeline files:
   - `.github/workflows/publishPackages.yml` (line 19)
   - `sample-tale-code-apps/DreamTravel/devOps/pipelines/build&test.yml` (lines 33, 109)
   - `sample-tale-code-apps/DreamTravel/devOps/pipelines/e2etests.yml` (line 16)

4. **Docker Requirements**: Build & Test pipeline requires Docker for SQL Server container

5. **Secrets Required**:
   - GitHub: `NUGET_API_KEY` for publishing packages
   - Azure DevOps: `dream-travel-test` variable group with connection strings and API keys

## Working with DreamTravel Sample

DreamTravel is a Traveling Salesman Problem solver that demonstrates:
- CQRS pattern with Commands and Queries
- Story Framework for workflow orchestration (migrated from deprecated Chain pattern)
- Integration with external APIs (Google Maps, Michelin)
- .NET Aspire for orchestration
- Multi-layer architecture
- Clean Architecture with strict layer separation

Key entry points:
- API: `DreamTravel.Api` - Controllers for user-facing queries
- Worker: `DreamTravel.Worker` - Background processing triggered by messages
- UI: `DreamTravel.Ui` - Blazor UI

**Note**: DreamTravel is being actively migrated to use Story Framework for all workflow orchestration, replacing the deprecated Flow/Chain pattern.

## Common Gotchas

1. **Test Discovery**: Tests are in `tests/` directory (outside `src/`), referenced in solution as `Tests` folder
2. **Assembly Scanning**: `ModuleInstaller` methods use `Assembly.GetCallingAssembly()` - they must be called from the assembly containing handlers
3. **Story Chapters**: Must be registered in DI (automatically scanned when using `RegisterStories()`)
4. **Result Implicit Conversion**: You can return domain objects directly - they'll auto-convert to `Result<T>`
5. **Workload Restore**: Required before build - see GitHub workflow for reference
6. **Windows PowerShell**: This repo runs on Windows, so shell commands use PowerShell syntax (e.g., `Select-String`, `Select-Object`) not bash
7. **Story Persistence**: SQLite database for interactive workflows is stored in configured path (default: `stories.db`), use in-memory for tests
8. **Auid Serialization**: Always use ProjectReference (not PackageReference) for SolTechnology.Core.AUID to ensure AuidJsonConverter is available for System.Text.Json serialization
9. **JSON Options**: Story Framework uses `StoryJsonOptions.Default` with `PropertyNameCaseInsensitive = true` and `IncludeFields = true` for consistent serialization

## Technical Requirements

### SDK and Runtime

- **.NET SDK**: 10.0.100 or later (enforced by `global.json` at repository root)
- **Target Framework**: .NET 10.0 (`net10.0`)
- **CLI**: All standard `dotnet` commands work with `.slnx` solution files

### IDE Support

- **JetBrains Rider**: 2024.3+ (full `.slnx` support)
- **Visual Studio**: 2022 17.13+ (for `.slnx` editing)
- **VS Code**: Works with `dotnet` CLI for building/testing

### Solution Structure

- **Solution File**: `SolTechnology.Core.slnx` (XML-based format, located at repository root)
- **Build Configuration**: 3-tier `Directory.Build.props` hierarchy (root → src/ → projects)
- **Solution Folders**: src/, tests/, docs/, sample-tale-code-apps/, .github/

The `.slnx` format is functionally equivalent to traditional `.sln` but uses XML structure for better readability and version control friendliness. All standard `dotnet` CLI commands work seamlessly with `.slnx` files.
