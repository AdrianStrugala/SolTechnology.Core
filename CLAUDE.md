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
3. **Target Framework**: .NET 10.0
4. **Solution Format**: `.slnx` (XML-based solution file format)
5. **Nullable Reference Types**: Enabled across all projects
6. **Implicit Usings**: Enabled
7. **Testing Framework**:
   - Use NUnit for all tests
   - For integration tests, use WebApplicationFactory and Testcontainers
8. **Validation Framework**: Use FluentValidation for all input validation

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
- `tests/SolTechnology.Core.AUID.Tests`
- `tests/SolTechnology.Core.ApiClient.Tests`
- `tests/SolTechnology.Core.Guards.Tests`
- `tests/SolTechnology.Core.Sql.Tests`

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
   - Azure DevOps: Tests DreamTravel sample app (`sample-tale-code-apps/DreamTravel/backend/tests/`)

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
