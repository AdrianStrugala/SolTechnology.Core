---
spec: 2026-11-02-core-2-0-tale
step: 05
status: done
---

# Step 05: Tale — registration and schema safety, correct log scope

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

Former core step 26; moved here by the per-library regroup (user, 2026-11-02).

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
- `docs/Tale.md:519-523` (log category) is corrected in step 13.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### Test placement and the premortem mitigation for `ModuleInstaller.cs`

**Original plan:** `AddSolTale_NoArgs_ScansCallerAssembly` and the log-scope test in the listed
test files; the schema test in `TaleControllerTests.cs`; premortem mitigations for
`ModuleInstaller.cs` applied.

**Actual implementation:** `AddSolTale_NoArgs_ScansCallerAssembly` and
`Handle_NewTale_LogScopeHasAssignedTaleId` live in `RegistrationDefaultsTests.cs`; both failed on
`master` before the change (`86bb056f`: the test assembly was not scanned; the scope carried
`Auid.Empty`). `ToDataFields_SelfReferencingType_Terminates` goes through the public
`InteractiveChapter.GetRequiredInputSchema()` so that it survives step 11 making `SchemaBuilder`
internal; it was not run on the old code because the recursion kills the test host. The only
premortem item for this step's `ModuleInstaller.cs` change is core scenario 23: the no-args scan
test is the guard that step 15 (RM-16) keeps unmodified. The schema walk tracks the types on the
current path (not every type seen), so a type that appears in two sibling properties is still
expanded in both.
