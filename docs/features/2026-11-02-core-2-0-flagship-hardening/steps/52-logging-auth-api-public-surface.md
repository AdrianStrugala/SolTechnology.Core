---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 52
status: to-do
---

# Step 52: Logging, Authentication, and Api — public surface cleanup

## Summary
Four small public-surface fixes, merged into one PR during plan review (RQ11), implement `C-20`,
`C-21`, and `C-22`:

- `ILoggerExtensions` carries an `I` prefix although it is not an interface, and it uses
  `static class` + `this` instead of an `extension` block (`logging-cache#21`,
  `ILoggerExtensions.cs:12`, Guide §9.7, §10).
- `CoreLoggingActivitySources.Operations` is documented as "not public API" but is a public field
  (`CoreLoggingActivitySources.cs:32`). CQRS reads it
  (`CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:28`), and CQRS is a separately versioned
  package. RQ9 (user, 2026-11-02): CQRS creates its own `ActivitySource` with the same name, so no
  `InternalsVisibleTo` crosses package boundaries.
- `ApiKeyAuthenticationSchemeOptions.ApiKey` is a public mutable field, and
  `ApiKeyAuthenticationHandler` takes a redundant `ILogger<ApiKeyAuthenticationHandler>` parameter
  although the base class provides `Logger`.
- `ConfigureSwaggerOptions` is a public implementation type of the Api installer.

## Affected components
- `src/SolTechnology.Core.Logging/ILoggerExtensions.cs` — DELETE
- `src/SolTechnology.Core.Logging/LoggerExtensions.cs` — NEW — `public static class LoggerExtensions { extension(ILogger logger) { … } }`, same members
- `src/SolTechnology.Core.Logging/Operations/CoreLoggingActivitySources.cs` — EDIT — `:32` `Operations` → `internal`; `OperationsName` stays a public `const`
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — `:28` uses a NEW `private static readonly ActivitySource OperationsSource = new(CoreLoggingActivitySources.OperationsName);`
- `src/SolTechnology.Core.Authentication/APIKeyAuthenticationSchemeOptions.cs` — EDIT — `:9` `public string ApiKey { get; set; } = "";`
- `src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs` — EDIT — primary constructor without the `logger` parameter; use `Logger`
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `:247` `ConfigureSwaggerOptions` → `internal sealed`
- `tests/SolTechnology.Core.Logging.Tests/**`, `tests/SolTechnology.Core.CQRS.Tests/**`, `tests/SolTechnology.Core.Authentication.Tests/**`, `tests/SolTechnology.Core.API.Tests/**` — EDIT

## Changes
- Call sites using `logger.OperationStarted(...)` syntax compile unchanged. Only static-call syntax (`ILoggerExtensions.X(logger)`) breaks.
- Two `ActivitySource` instances with one name are both observed by a listener subscribed to `OperationsName`; consumers keep `AddSource(CoreLoggingActivitySources.OperationsName)`.
- Test: `LoggingPipelineBehavior_ListenerOnOperationsName_ReceivesActivity` (CQRS.Tests, `ActivityListener` filtered by name).
- Depends on step 07 (instance logger) having merged.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test` is green for the Logging, CQRS, Authentication, and API test projects.
- [ ] `grep -rn "CoreLoggingActivitySources.Operations\b" src --include=*.cs | grep -v "OperationsName"` lists only `src/SolTechnology.Core.Logging`.
- [ ] `grep -rn "InternalsVisibleTo" src/SolTechnology.Core.Logging` adds no entry for CQRS.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none (`C-20`, `C-21`, `C-22` confirmed by the user on 2026-11-02; RQ9 resolved: CQRS owns its `ActivitySource`)

## Deviations
