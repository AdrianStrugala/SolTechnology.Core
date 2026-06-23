---
adr: 011-story-sqlite-extraction
step: 05 of 09
status: done
---

# Step 05: Remove the SQLite *code* from the library and Story.Tests

## Summary
Now that the new home (Steps 01–04) is green, delete the SQLite **code** from
`SolTechnology.Core.Story` and the relocated **tests** from `SolTechnology.Core.Story.Tests`. This step
removes only code (logic); the SQLite **packages** and **audit suppressions** stay until Step 06, so
both projects keep building (the suppression still silences NU1903 while the now-unused packages
linger). Separating code removal (this step) from package/suppress removal (Step 06) honours the
plumbing-vs-logic boundary and keeps each PR's diff single-themed.

## Affected components
- `src/SolTechnology.Core.Story/Persistence/SqliteStoryRepository.cs` — delete
- `src/SolTechnology.Core.Story/Persistence/SqliteStoryRepositoryOptions.cs` — delete
- `src/SolTechnology.Core.Story/Builder/StoryBuilderExtensions.cs` — remove the two `UseSqliteStoryRepository` overloads + update the class XML summary
- `src/SolTechnology.Core.Story/Builder/IStoryBuilder.cs` — drop `UseSqliteStoryRepository` from the XML doc
- `src/SolTechnology.Core.Story/StoryOptions.cs` — drop `UseSqliteStoryRepository` from the XML doc
- `src/SolTechnology.Core.Story/ModuleInstaller.cs` — drop `UseSqliteStoryRepository` from the `<remarks>` doc
- `tests/SolTechnology.Core.Story.Tests/SqliteRepositoryTests.cs` — delete (relocated in Step 04)
- `tests/SolTechnology.Core.Story.Tests/RegistrationDefaultsTests.cs` — remove the two `UseSqliteStoryRepository_*` cases (and their now-unused local `using`/helpers if any become dead)

## Details
- In `StoryBuilderExtensions.cs`: delete both `UseSqliteStoryRepository` methods. **Keep**
  `UseInMemoryStoryRepository`, `UseStoryRepository<T>`, and **both** private helpers
  (`ReplaceRepository` is still used by `UseInMemoryStoryRepository`; `EnsureStoryManager` is used by
  both survivors). Update the class-level `<summary>` to drop the `UseSqliteStoryRepository` example.
- In `RegistrationDefaultsTests.cs`: remove only `UseSqliteStoryRepository_ReplacesDefault_*` and
  `UseSqliteStoryRepository_ConfigureCallback_*`. **Keep** `RegisterStories_Defaults_*` and
  `UseStoryRepository_Generic_*` and all shared helpers (`BuildProvider`, `AssertFullCycleCompletes`,
  `TryDelete`, `RecordingStoryRepository`) — they remain used by the surviving cases. Remove the
  `SolTechnology.Core.Story.Persistence` `using` only if it becomes unused after the deletions.
- Update the XML docs in `IStoryBuilder.cs`, `StoryOptions.cs`, `ModuleInstaller.cs` and the
  `StoryBuilderExtensions` summary so the surviving narrative lists only `UseInMemoryStoryRepository`
  (default) and `UseStoryRepository<T>()` (escape hatch). No `UseSqliteStoryRepository` references
  remain in `src/`.
- **Do not touch** any `PackageReference` or `<NuGetAuditSuppress>` in this step (Step 06 owns those).

## Acceptance criteria
- `grep -r "Sqlite" src/SolTechnology.Core.Story` returns nothing (code or XML doc).
- `grep -r "UseSqliteStoryRepository" tests/SolTechnology.Core.Story.Tests` returns nothing.
- `dotnet build src/SolTechnology.Core.Story` is green (suppression still present, masking the still-unused SQLite packages' NU1903).
- `dotnet test tests/SolTechnology.Core.Story.Tests` is green; the surviving in-memory + generic
  registration cases still pass.
- The public surface still exposes `IStoryRepository`, `InMemoryStoryRepository`,
  `UseInMemoryStoryRepository()`, `UseStoryRepository<T>()`.

## Open questions
- none

