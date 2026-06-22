---
adr: 011-story-sqlite-extraction
step: 03 of 09
status: done
---

# Step 03: Add the consumer-side `UseSQLiteStoryRepository` builder extension

## Summary
Re-implement the two `UseSQLiteStoryRepository(...)` overloads (connection-string shortcut +
`Action<SQLiteStoryRepositoryOptions>` callback) as a **consumer-side** extension on the *public*
`IStoryBuilder` surface, in the new project. This is the **DI plumbing** for the SQLite provider —
deliberately separated from the repository logic (Step 02). It proves the framework's public
`UseStoryRepository<T>()` seam is sufficient, with **no new public API in the library**.

## Affected components
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/SQLiteStoryBuilderExtensions.cs` — new

## Details
- The original library overloads used the **private** helpers `ReplaceRepository` / `EnsureStoryManager`
  in `StoryBuilderExtensions`. A consumer cannot call those. Instead, build on the public seam:
  `IStoryBuilder.Services` + the public `UseStoryRepository<TRepository>(ServiceLifetime)`.
- Implement the configure-callback overload as the primitive, and the connection-string overload as a
  thin delegate to it (mirroring the original):
  - configure-callback overload:
    1. `var options = new SQLiteStoryRepositoryOptions(); configure(options);`
    2. `builder.Services.RemoveAll<SQLiteStoryRepositoryOptions>(); builder.Services.AddSingleton(options);`
    3. `return builder.UseStoryRepository<SQLiteStoryRepository>(ServiceLifetime.Singleton);`
  - connection-string overload: validate non-empty, then
    `return builder.UseSQLiteStoryRepository(o => o.ConnectionString = connectionString);`
- **Why this works (record in the PR description):** with `SQLiteStoryRepositoryOptions` registered as
  a singleton, the built-in container selects the `SQLiteStoryRepository(SQLiteStoryRepositoryOptions? options = null)`
  constructor and injects the registered options; `UseStoryRepository<T>()` already performs the
  `RemoveAll<IStoryRepository>()` + `EnsureStoryManager` wiring the original private `ReplaceRepository`
  did. The result is behaviourally equivalent to the deleted library overloads.
- **No constructor-ambiguity throw:** the container picks the options ctor because the sibling
  `(string connectionString)` ctor is **not** DI-resolvable (no registered `string`, no default value).
  Both ctors must stay — `SQLiteRepositoryTests` (Step 04) news up the repo via the `string` ctor.
- Namespace `DreamTravel.SQLite`; method name `UseSQLiteStoryRepository`, extended type `IStoryBuilder`.
  Required `using`s: `Microsoft.Extensions.DependencyInjection`,
  `Microsoft.Extensions.DependencyInjection.Extensions` (`RemoveAll`), `SolTechnology.Core.Story.Builder`.
- Preserve the original argument validation (`ArgumentException` on empty connection string,
  `ArgumentNullException` on null configure).

## Acceptance criteria
- The new project builds; both overloads compile against only the public `SolTechnology.Core.Story` surface.
- No change is required to any `src/SolTechnology.Core.Story` public type to make this compile (proves
  finding #1 — the existing seam is sufficient).
- A throwaway DI smoke check (or the relocated test in Step 04) shows that after
  `RegisterStories().UseSQLiteStoryRepository("Data Source=...")`, `IStoryRepository` resolves to
  `SQLiteStoryRepository` and `StoryManager` resolves.

## Open questions
- none (the public-seam sufficiency is verified in the ADR; this step confirms it in code).


