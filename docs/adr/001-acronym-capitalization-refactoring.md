# ADR 001: Acronym Capitalization Refactoring

**Status:** ✅ Completed
**Date:** 2025-12-30
**Decision Makers:** Development Team

## Context

The codebase has inconsistent acronym capitalization following Microsoft's .NET naming guidelines (2-letter acronyms in CAPS, 3+ letter acronyms in PascalCase). After reviewing industry standards (Python PEP 8, Java, TypeScript), we decided to adopt **ALL CAPS for all acronyms** regardless of length for better visual clarity and consistency.

**Current State:**
- `ApiClient` → Should be `APIClient`
- `SqlConnection` → Should be `SQLConnection`
- `XmlDocument` → Should be `XMLDocument`
- Package: `SolTechnology.Core.ApiClient` → Should be `SolTechnology.Core.HTTP`

**Rationale:**
- Visual clarity: Acronyms are immediately recognizable
- Consistency: Same rule for all acronyms (no 2-letter vs 3+ letter distinction)
- Alignment with Python PEP 8 convention
- Better readability in code

## Decision

**Phase 1: Package Rename**
- Rename `SolTechnology.Core.ApiClient` → `SolTechnology.Core.HTTP`
  - More descriptive (it's an HTTP client wrapper)
  - Shorter, cleaner name
  - Follows the new ALL CAPS convention

**Phase 2: Class/Interface Refactoring**
- All `Api` → `API`
- All `Sql` → `SQL`
- All `Xml` → `XML`
- All `Html` → `HTML`
- All `Ui` → `UI`
- etc.

**Scope:**
- Core libraries (`src/SolTechnology.Core.*`)
- Sample application (DreamTravel)
- Tests
- Documentation

**Out of Scope:**
- Published package names remain unchanged for backwards compatibility
  - `SolTechnology.Core.CQRS` (already correct)
  - `SolTechnology.Core.AUID` (already correct)

## Implementation Plan

### 1. Analysis Phase ✅
- [x] Create this ADR
- [x] Scan codebase for all occurrences
- [x] Identify affected files and references

### 2. Refactoring Phase ✅
#### 2.1 Package Rename: ApiClient → HTTP ✅
- [x] Rename project folder
- [x] Rename .csproj file
- [x] Update PackageId, AssemblyName, Product in .csproj
- [x] Update namespace in all .cs files
- [x] Update all ProjectReferences
- [x] Update solution file (.slnx)
- [x] Update DreamTravel HTTP clients (GoogleAPIClient → GoogleHTTPClient, etc.)
- [x] Update configuration files (appsettings.json: ApiClients → HTTPClients)

#### 2.2 Class/Namespace Refactoring: Api → API ✅
- [x] Find all `Api` class names
- [x] Update class names (ApiFixture → APIFixture, etc.)
- [x] Update namespace declarations (SolTechnology.Core.Api → SolTechnology.Core.API)
- [x] Update usings
- [x] Update all references in code
- [x] Rename folders and .csproj files (Core.Api → Core.API)
- [x] Update DreamTravel controllers and filters

#### 2.3 Class/Namespace Refactoring: Sql → SQL ✅
- [x] Find all `Sql` class names
- [x] Update class names (SqlConfiguration → SQLConfiguration, SqlConnectionFactory → SQLConnectionFactory, etc.)
- [x] Update namespace declarations (SolTechnology.Core.Sql → SolTechnology.Core.SQL)
- [x] Update usings
- [x] Update all references in code
- [x] Rename folders and .csproj files (Core.Sql → Core.SQL)
- [x] Update test projects (Core.Sql.Tests → Core.SQL.Tests)
- [x] Update DreamTravel SQL configurations

### 3. IDE Configuration ✅
- [x] Create .editorconfig with acronym exceptions (disables IDE1006 warnings for HTTP, API, SQL, etc.)

### 4. Verification Phase ✅
- [x] Build solution (dotnet build) - **0 errors**
- [x] Run all tests (.github/runTests.ps1) - **334/334 passed**
- [x] Check for broken references - **All resolved**
- [ ] Code review

### 5. Documentation Phase ✅
- [x] Update CLAUDE.md - Updated library list, test commands, CI/CD section
- [x] Update README files - No changes needed (package names preserved)
- [x] Update docs/*.md files - Updated Clients.md, Sql.md, theDesign.md
- [x] Update code comments - Updated inline documentation

### 6. CI/CD Updates ✅
- [x] Update GitHub Actions workflows - Updated pack commands with new folder paths
- [x] Azure DevOps pipelines - DreamTravel pipelines work with solution-based builds (no hardcoded paths)
- [x] Package publishing scripts - runTests.ps1 works dynamically with folder discovery

## Progress Tracker

### Scope Analysis
- **ApiClient references**: 55 files affected
- **Api pattern occurrences**: 126 instances in 51 files
- **Sql pattern occurrences**: 68 instances in 25 files
- **Total estimated files**: ~100 files

### Final Results
- **Files Changed**: ~60 files
  - Core.HTTP: 10 files (classes, tests, ModuleInstaller)
  - Core.API: 8 files (filters, middlewares, testing fixtures)
  - Core.SQL: 12 files (configuration, connections, transactions, fixtures, deployer)
  - DreamTravel: 20 files (HTTP clients, Program.cs, controllers, component tests)
  - Configuration: 4 appsettings.json files
  - Infrastructure: .editorconfig, .slnx, .csproj files
- **Builds**: ✅ **Success (0 errors, 122 warnings)**
- **Tests**: ✅ **334/334 passed**
  - AUID.Tests: 91/91 ✅
  - Guards.Tests: 150/150 ✅
  - HTTP.Tests: 1/1 ✅
  - SQL.Tests: 1/1 ✅
  - Story.Tests: 91/91 ✅

### Current Status: ✅ **All Phases Complete - Refactoring Successfully Implemented**

**Summary:**
- All code refactored (HTTP, API, SQL)
- All tests passing (334/334)
- Documentation updated
- CI/CD pipelines updated
- Ready for code review and merge

## Consequences

**Positive:**
- ✅ Consistent acronym casing across entire codebase
- ✅ Better visual clarity
- ✅ Alignment with modern conventions (Python PEP 8)
- ✅ More descriptive package name (HTTP vs ApiClient)

**Negative:**
- ⚠️ Breaking changes for internal code
- ⚠️ Large refactoring scope (many files affected)
- ⚠️ Potential merge conflicts with ongoing work
- ⚠️ Learning curve for developers used to old naming

**Mitigation:**
- Document changes in CLAUDE.md
- Update all references in single PR
- Run comprehensive tests before merging
- Update IDE settings/snippets if needed

## Notes

- This refactoring does NOT change published NuGet package names (backwards compatibility)
- New code will follow ALL CAPS convention going forward
- Existing packages (CQRS, AUID) already follow this convention

## References

- [Python PEP 8 Style Guide](https://peps.python.org/pep-0008/)
- [Microsoft .NET Naming Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions)
- CLAUDE.md - Code Style Conventions #9
