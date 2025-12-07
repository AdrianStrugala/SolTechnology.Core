# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

SolTechnology.Core is a collection of NuGet packages that provide a foundation for building CQRS-driven applications using Azure technologies. The repository follows the "Tale Code" philosophy - making code readable like well-written prose. It includes both the core libraries (in `src/`) and a sample application called DreamTravel (in `sample-tale-code-apps/DreamTravel/`).

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
- **SolTechnology.Core.Flow** - Workflow and chain framework with pausable flows
- **SolTechnology.Core.AUID** - AUID (Application Unique ID) implementation
- **SolTechnology.Core.Faker** - Test data generation

### Sample Application (DreamTravel)

Located in `sample-tale-code-apps/DreamTravel/backend/src/`, organized by layers:

- **Presentation/** - Entry points (API, Worker, UI)
- **LogicLayer/** - Business logic (Commands, Queries, Domain Services, Flows)
- **DataLayer/** - Data access (SQL, HTTP clients, Graph database)
- **Infrastructure/** - Shared infrastructure and service defaults
- **DreamTravel.Trips.Domain** - Domain models
- **DreamTravel.Aspire** - .NET Aspire orchestration

### Tests

- Core library tests: `tests/SolTechnology.Core.*.Tests`
- DreamTravel tests: `sample-tale-code-apps/DreamTravel/backend/tests/`
  - `Unit/` - Unit tests for individual components
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
   cd sample-tale-code-apps/DreamTravel/backend
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

### Chain Pattern (SuperChain)

For complex multi-step operations, use `ChainHandler<TInput, TContext, TOutput>`:

1. Define context inheriting from `ChainContext<TInput, TOutput>`
2. Create steps implementing `IChainStep<TContext>`
3. Handler orchestrates steps via `Invoke<TStep>()`
4. Register with `services.RegisterChain()`

Example:
```csharp
public class MyHandler : ChainHandler<MyInput, MyContext, MyOutput>
{
    protected override async Task HandleChain()
    {
        await Invoke<Step1>();
        await Invoke<Step2>();
        await Invoke<Step3>();
    }
}
```

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
3. **Target Framework**: .NET 8.0
4. **Nullable Reference Types**: Enabled across all projects
5. **Implicit Usings**: Enabled
6. **Testing Framework**:
   - Use NUnit for all tests
   - For integration tests, use WebApplicationFactory and Testcontainers
7. **Validation Framework**: Use FluentValidation for all input validation

## Important Implementation Notes

### Clean Architecture Layer Dependencies

Dependencies flow in one direction only (from top to bottom):
- Presentation → Logic → Data → Infrastructure
- Domain has no dependencies
- Never reference higher layers from lower layers

### CQRS Handlers Structure

Handlers should contain minimal code - they orchestrate executors/steps:
- **Query/Command** - Input model with validation
- **Context** (optional) - Intermediate model for internal operations (performance optimization)
- **Handler** - Orchestrates the flow
- **Executors/Steps** - Actual implementation logic
- **Result** - Output model

### Pipeline Behaviors

MediatR pipeline includes:
1. `LoggingPipelineBehavior` - Automatic logging
2. `FluentValidationPipelineBehavior` - Automatic validation

Both are registered automatically when using `RegisterCommands()` or `RegisterQueries()`.

### Error Handling

- Use `Result` pattern - avoid throwing exceptions for business logic failures
- Exceptions are caught in chain steps and converted to `Error`
- Use `AggregateError` for multiple errors in chain operations

## CI/CD

GitHub Actions workflow: `.github/workflows/publishPackages.yml`

Triggers on push/PR to master:
1. Restore workload and dependencies
2. Build solution
3. Run all tests via PowerShell script
4. Pack each library
5. Publish to NuGet (on master branch only)

## Working with DreamTravel Sample

DreamTravel is a Traveling Salesman Problem solver that demonstrates:
- CQRS pattern with Commands and Queries
- Chain-based workflows
- Integration with external APIs (Google Maps, Michelin)
- .NET Aspire for orchestration
- Multi-layer architecture

Key entry points:
- API: `DreamTravel.Api` - Controllers for user-facing queries
- Worker: `DreamTravel.Worker` - Background processing triggered by messages
- UI: `DreamTravel.Ui` - Blazor UI

## Common Gotchas

1. **Test Discovery**: Tests are in `tests/` directory (outside `src/`), referenced in solution as `Tests` folder
2. **Assembly Scanning**: `ModuleInstaller` methods use `Assembly.GetCallingAssembly()` - they must be called from the assembly containing handlers
3. **Chain Steps**: Must be registered with `RegisterChain()` before use
4. **Result Implicit Conversion**: You can return domain objects directly - they'll auto-convert to `Result<T>`
5. **Workload Restore**: Required before build - see GitHub workflow for reference

## Solution Format and .NET Version

### Current Configuration

- **Solution Format**: `.slnx` (XML-based solution file introduced in .NET 10)
- **Solution Location**: Repository root (`SolTechnology.Core.slnx`)
- **Target Framework**: .NET 10.0
- **SDK Version**: 10.0.100+ (specified in `global.json` at repository root)
- **IDE Requirements**: JetBrains Rider 2024.3+ for full .slnx support

### Migration History

This repository was migrated from .NET 8.0 to .NET 10.0 and from traditional `.sln` to modern `.slnx` format. Key changes:

1. **Solution moved from** `src/SolTechnology.Core.sln` **to** `SolTechnology.Core.slnx` (at root)
2. **All projects upgraded** from `net8.0` to `net10.0` target framework
3. **Directory.Build.props hierarchy**: Root → src/ → projects (for shared build properties)
4. **global.json moved** to repository root for SDK version control
5. **Organized solution folders**: src/, tests/, docs/, sample-tale-code-apps/, .github/

### Building with .slnx

The `.slnx` format is functionally equivalent to `.sln` but uses XML structure for better readability and VCS friendliness:

```bash
# All standard dotnet commands work with .slnx
dotnet restore SolTechnology.Core.slnx
dotnet build SolTechnology.Core.slnx
dotnet test SolTechnology.Core.slnx
dotnet pack SolTechnology.Core.slnx
```

### Requirements

- **.NET SDK**: 10.0.100 or later (auto-enforced by `global.json`)
- **IDE**: JetBrains Rider 2024.3+ or Visual Studio 2022 17.13+ for .slnx editing
- **CLI**: .NET CLI 10.0+ fully supports .slnx format
