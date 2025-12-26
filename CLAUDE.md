# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
- **SolTechnology.Core.Sql** - SQL database access with Dapper and Entity Framework
- **SolTechnology.Core.BlobStorage** - Azure Blob Storage wrapper
- **SolTechnology.Core.ApiClient** - HTTP client abstractions
- **SolTechnology.Core.MessageBus** - Azure Service Bus messaging
- **SolTechnology.Core.Guards** - Input validation utilities
- **SolTechnology.Core.Authentication** - Basic and API key authentication
- **SolTechnology.Core.Logging** - Logging abstractions
- **SolTechnology.Core.Scheduler** - Cron-based task scheduling
- **SolTechnology.Core.Api** - API utilities and filters
- **SolTechnology.Core.Cache** - Caching abstractions
- **SolTechnology.Core.Story** - Story Framework for workflow orchestration with pausable workflows, persistence, and Tale Code philosophy
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
dotnet test SolTechnology.Core.Sql.Tests --no-build
dotnet test SolTechnology.Core.ApiClient.Tests --no-build
dotnet test SolTechnology.Core.Guards.Tests --no-build
```

### Packaging

```bash
# Pack individual library (example)
dotnet pack -c Release -o . ./src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj
```

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

**Story Framework** is the unified approach for workflow orchestration, replacing the deprecated Chain pattern. It supports both simple automated workflows and pausable workflows with persistence.

**Key Concepts:**
- **StoryHandler** - Main orchestrator, inherits from `StoryHandler<TInput, TContext, TOutput>`
- **Narration** - Context object that carries state through the story, inherits from `Context<TInput, TOutput>`
- **Chapter** - Individual step in the story, implements `IChapter<TContext>`
- **InteractiveChapter** - Chapter that pauses for user input, inherits from `InteractiveChapter<TContext, TChapterInput>`

**Basic Story (automated workflow):**
```csharp
public class OrderProcessingStory : StoryHandler<OrderInput, Ordercontext, OrderOutput>
{
    public OrderProcessingStory(IServiceProvider sp, ILogger<OrderProcessingStory> logger)
        : base(sp, logger) { }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidateOrderChapter>();
        await ReadChapter<ProcessPaymentChapter>();
        await ReadChapter<ShipOrderChapter>();

        context.Output.OrderId = context.ProcessedOrderId;
    }
}
```

**Interactive Story (pausable workflow with persistence):**
```csharp
public class UserOnboardingStory : StoryHandler<OnboardingInput, Onboardingcontext, OnboardingOutput>
{
    protected override async Task TellStory()
    {
        await ReadChapter<CollectBasicInfoChapter>();     // Pauses for user input
        await ReadChapter<VerifyEmailChapter>();           // Pauses for email verification
        await ReadChapter<SetupPreferencesChapter>();      // Pauses for preferences
        await ReadChapter<CompleteOnboardingChapter>();    // Automated
    }
}

// Interactive chapter with validation
public class CollectBasicInfoChapter : InteractiveChapter<Onboardingcontext, UserBasicInfo>
{
    public override Task<Result> ReadWithInput(OnboardingNarration context, UserBasicInfo userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Name))
            return Result.FailAsTask("Name is required");

        context.UserName = userInput.Name;
        context.UserEmail = userInput.Email;
        return Result.SuccessAsTask();
    }
}
```

**Registration:**
```csharp
services.AddStoryFramework(options =>
{
    options.EnablePersistence = true;  // For pausable workflows
    options.DatabasePath = "stories.db"; // SQLite persistence
});

// Or for in-memory testing
services.AddSingleton(StoryOptions.WithInMemoryPersistence());
```

**Usage with StoryManager (for pausable workflows):**
```csharp
// Start story
var result = await storyManager.StartStory<UserOnboardingStory, OnboardingInput, Onboardingcontext, OnboardingOutput>(input);
var storyId = result.Data.StoryId;

// Resume with user input
var userInput = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"john@example.com\"}");
var resumeResult = await storyManager.ResumeStory<UserOnboardingStory, OnboardingInput, Onboardingcontext, OnboardingOutput>(
    storyId,
    userInput.RootElement);
```

**Tale Code Philosophy:**
Stories read like prose - `TellStory()` method narrates what happens, chapters are named as actions (verbs), and the flow is clear and linear.

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
9. **Acronym Casing**: Follow Microsoft .NET naming guidelines for acronyms
   - **2-letter acronyms**: ALL CAPS → `UI`, `IO`, `DB`
   - **3+ letter acronyms**: Pascal case → `Api`, `Xml`, `Html`, `Sql`, `Cqrs`, `Auid`
   - Examples:
     - ✅ GOOD: `ApiClient`, `XmlDocument`, `HtmlHelper`, `SqlConnection`, `UIControl`, `IOStream`
     - ❌ BAD: `XMLDocument`, `HTMLHelper`, `SQLConnection`, `CQRS`, `AUID`
   - **Note**: Existing projects (`SolTechnology.Core.CQRS`, `SolTechnology.Core.AUID`) keep their current names for backwards compatibility, but new code should follow this convention
10. **Testing Framework**:
   - Use NUnit for all tests
   - For integration tests, use WebApplicationFactory and Testcontainers
   - Write comprehensive QA scenarios covering edge cases, error handling, concurrency, and security
11. **Validation Framework**: Use FluentValidation for all input validation

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

**Comprehensive QA Approach:**
When testing complex features (especially workflows, state machines, or frameworks), write extensive scenario-based tests covering:

1. **Happy Path**: Standard usage scenarios
2. **Error Handling**: Invalid inputs, missing data, type mismatches
3. **Edge Cases**: Empty strings, whitespace, null values, extreme values (very long strings, negative numbers, max integers)
4. **Security**: Special characters, potential injection attempts, Unicode characters
5. **Concurrency**: Multiple simultaneous operations, race conditions
6. **State Management**: Pause/resume cycles, state transitions, history tracking
7. **Repository Failures**: Database errors, persistence failures, load/save errors

**Example from Story Framework tests:**
```csharp
[Test]
public async Task Resume_WithExtremelyLongStrings_ShouldHandleOrReject()
{
    var longString = new string('A', 10000);
    var input = JsonDocument.Parse($"{{\"name\": \"{longString}\", ...}}");
    var result = await storyManager.ResumeStory(..., input.RootElement);

    // Should either handle gracefully or fail with clear validation error
    if (result.IsFailure)
        result.Error.Message.Should().ContainAny("too long", "length", "maximum");
}
```

**Best Practices:**
- Test both success and failure paths
- Use descriptive test names (e.g., `Resume_WithMissingRequiredFields_ShouldReturnError`)
- Organize tests by category (regions or separate classes)
- Verify error messages contain meaningful keywords (not just "failed" or "error")
- Test actual behavior, not implementation details

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
- `tests/SolTechnology.Core.ApiClient.Tests` (1 test)
- `tests/SolTechnology.Core.Guards.Tests` (150 tests)
- `tests/SolTechnology.Core.Sql.Tests` (1 test)
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
3. **Story Chapters**: Must be registered in DI (automatically scanned when using `AddStoryFramework()`)
4. **Result Implicit Conversion**: You can return domain objects directly - they'll auto-convert to `Result<T>`
5. **Workload Restore**: Required before build - see GitHub workflow for reference
6. **Windows PowerShell**: This repo runs on Windows, so shell commands use PowerShell syntax (e.g., `Select-String`, `Select-Object`) not bash
7. **Story Persistence**: SQLite database for pausable workflows is stored in configured path (default: `stories.db`), use in-memory for tests
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
