---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 26
status: to-do
---

# Step 26: Tale — registration and schema safety, correct log scope

## Summary
Three Tale defects with no public-signature impact:

- `AddSolTale()` without arguments calls `GetCallingAssembly()` from a private helper. Without
  inlining, that returns `SolTechnology.Core.Tale` itself, so only the entry assembly is scanned,
  which is testhost under `WebApplicationFactory` (`cqrs-tale#12`, `Tale/ModuleInstaller.cs:118-126`).
- `DataField.ToDataFields` recurses into every complex property with no visited set, and a
  self-referencing type kills the process with a StackOverflow (`cqrs-tale#13`,
  `Models/DataField.cs:60-63`).
- The log scope captures `TaleId` before `Initialize` assigns it (`cqrs-tale#19`,
  `TaleHandler.cs:150-159`, L-05).

## Affected components
- `src/SolTechnology.Core.Tale/ModuleInstaller.cs` — EDIT — `[MethodImpl(MethodImplOptions.NoInlining)]` on `AddSolTale` and capture `Assembly.GetCallingAssembly()` there
- `src/SolTechnology.Core.Tale/Models/DataField.cs` — EDIT — visited `HashSet<Type>` and max depth 8
- `src/SolTechnology.Core.Tale/TaleHandler.cs` — EDIT — open the scope after `Initialize`
- `tests/SolTechnology.Core.Tale.Tests/RegistrationDefaultsTests.cs` — EDIT
- `tests/SolTechnology.Core.Tale.Tests/TaleControllerTests.cs` — EDIT — schema of a self-referencing input type

## Changes
- Tests:
  - `AddSolTale_NoArgs_ScansCallerAssembly`.
  - `ToDataFields_SelfReferencingType_Terminates` (`class Node { Node? Parent; }` and `CultureInfo`).
  - `Handle_NewTale_LogScopeHasAssignedTaleId`.
- `docs/Tale.md:519-523` (log category) is corrected in step 60.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
