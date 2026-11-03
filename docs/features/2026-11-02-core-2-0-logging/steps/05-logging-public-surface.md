---
spec: 2026-11-02-core-2-0-logging
step: 05
status: done
---

# Step 05: Logging — public surface cleanup (`LoggerExtensions`, internal `Operations`)

## Summary
This implements `C-20`. `ILoggerExtensions` carries an `I` prefix although it is not an
interface, and it uses `static class` + `this` instead of an `extension` block
(`logging-cache#21`, `ILoggerExtensions.cs:12`, Guide §9.7, §10).
`CoreLoggingActivitySources.Operations` is documented as "not public API" but is a public field
(`CoreLoggingActivitySources.cs:32`). CQRS reads it
(`CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:28`), and CQRS is a separately versioned
package. RQ9 (user, 2026-11-02): CQRS creates its own `ActivitySource` with the same name, so no
`InternalsVisibleTo` crosses package boundaries. The CQRS edit stays in this step because making
the field internal breaks the CQRS build otherwise.

Former core step 52 (Logging part, including its CQRS call-site edit); split per library on 2026-11-02. The Authentication and Api parts are api-auth step 05.

## Affected components
- `src/SolTechnology.Core.Logging/ILoggerExtensions.cs` — DELETE
- `src/SolTechnology.Core.Logging/LoggerExtensions.cs` — NEW — `public static class LoggerExtensions { extension(ILogger logger) { … } }`, same members
- `src/SolTechnology.Core.Logging/Operations/CoreLoggingActivitySources.cs` — EDIT — `:32` `Operations` → `internal`; `OperationsName` stays a public `const`
- `src/SolTechnology.Core.CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs` — EDIT — `:28` uses a NEW `private static readonly ActivitySource OperationsSource = new(CoreLoggingActivitySources.OperationsName);`
- `tests/SolTechnology.Core.Logging.Tests/**`, `tests/SolTechnology.Core.CQRS.Tests/**` — EDIT

## Changes
- Call sites using `logger.OperationStarted(...)` syntax compile unchanged. Only static-call syntax (`ILoggerExtensions.X(logger)`) breaks.
- Two `ActivitySource` instances with one name are both observed by a listener subscribed to `OperationsName`; consumers keep `AddSource(CoreLoggingActivitySources.OperationsName)`.
- Test: `LoggingPipelineBehavior_ListenerOnOperationsName_ReceivesActivity` (CQRS.Tests, `ActivityListener` filtered by name).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [x] `dotnet test` is green for the Logging and CQRS test projects.
- [x] `grep -rn "CoreLoggingActivitySources.Operations\b" src --include=*.cs | grep -v "OperationsName"` lists only `src/SolTechnology.Core.Logging`.
- [x] `grep -rn "InternalsVisibleTo" src/SolTechnology.Core.Logging` adds no entry for CQRS.

## Open questions
- none (`C-20` confirmed by the user on 2026-11-02; RQ9 resolved: CQRS owns its `ActivitySource`)

## Deviations

### CQRS source lives in a non-generic holder

**Original plan:** `LoggingPipelineBehavior.cs:28` uses a new `private static readonly ActivitySource OperationsSource = new(CoreLoggingActivitySources.OperationsName);`.

**Actual implementation:** `LoggingPipelineBehavior<TRequest, TResponse>` is generic, so a static field on it would create one `ActivitySource` per closed request/response type. The source is `OperationsActivitySource.Instance` in a new internal, non-generic `src/SolTechnology.Core.CQRS/PipelineBehaviors/OperationsActivitySource.cs`; the behavior calls `OperationsActivitySource.Instance.StartActivity(...)`. The listener test lives in a new `tests/SolTechnology.Core.CQRS.Tests/PipelineBehaviors/LoggingPipelineBehaviorTests.cs` (cqrs step 03 lists that file as NEW; it now extends it). No Logging test needed an edit: every call site uses extension syntax. The CQRS change is a separate `refactor(cqrs)` commit before the Logging break, so each commit builds. The hand-written constructor in `LoggingPipelineBehavior.cs:16-21` (Guide §15, fix on touch) is left to cqrs step 06, which plans that exact fix.
