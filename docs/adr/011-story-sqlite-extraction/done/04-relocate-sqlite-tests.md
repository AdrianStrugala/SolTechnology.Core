---
adr: 011-story-sqlite-extraction
step: 04 of 09
status: done
---

# Step 04: Create the SQLite test project, relocate the repo tests, and add SampleOrderWorkflowStory SQLite coverage

> **Reviewer revision (2026-06-22).** Coverage decision = **(c)**: the engine↔SQLite pause/resume path is
> now exercised by driving the **real `SampleOrderWorkflowStory`** through `StoryManager` on a real SQLite
> file DB — not a synthetic fixture, and not dropped. The new test project therefore references **both**
> `DreamTravel.SQLite` (the relocated provider) and `DreamTravel.Workflows` (the story). The earlier
> "drop the full pause/resume assertion" plan is **superseded**. Also folds in the reviewer's fixes:
> BOM-less test csproj (matches the other `tests/Unit` csprojs), inline provider build (no cross-assembly
> `LifecycleStoryV1`), and the two-constructor DI-selection note.

## Summary
Create a new unit-test project for the relocated provider and **copy** the SQLite repository tests into
it, plus add a small suite that drives `SampleOrderWorkflowStory` end-to-end on SQLite (start → persist
at the interactive pause → resume from a fresh repository instance → complete), covering the failure
paths too. The repo tests now exist in both the new project and `Story.Tests` (the library still has
SQLite until Step 05/06), so **every project stays green**. Bundling test infrastructure with the test
logic it hosts is acceptable for a test relocation (the plumbing/logic split rule targets production code,
not test scaffolding).

## Affected components
- `sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.SQLite.UnitTests/DreamTravel.SQLite.UnitTests.csproj` — new
- `sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.SQLite.UnitTests/SQLiteRepositoryTests.cs` — new (from `tests/SolTechnology.Core.Story.Tests/SqliteRepositoryTests.cs`)
- `sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.SQLite.UnitTests/SQLiteRegistrationTests.cs` — new (the two `UseSqliteStoryRepository_*` cases from `RegistrationDefaultsTests.cs`)
- `sample-tale-code-apps/DreamTravel/tests/Unit/DreamTravel.SQLite.UnitTests/SampleOrderWorkflowSQLiteTests.cs` — **new** (the (c) end-to-end coverage)
- `SolTechnology.Core.slnx` — register the new test project under `/SampleApps/DreamTravel/tests/Unit/`

## Details

### csproj
- Template on `DreamTravel.Queries.UnitTests.csproj` (NUnit + FluentAssertions + NSubstitute + AutoFixture
  come from `sample-tale-code-apps/DreamTravel/tests/Directory.Build.props` — do **not** re-declare them).
- **No UTF-8 BOM** — the existing `tests/Unit/*.UnitTests.csproj` files are BOM-less; match them. (The BOM
  convention applies only to the DataLayer `src` csprojs in Step 01.)
- `ProjectReference`s, both verified by depth from `tests/Unit/<proj>/`:
  - to the provider: `..\..\..\src\DataLayer\DreamTravel.SQLite\DreamTravel.SQLite.csproj`
  - to the story: `..\..\..\src\LogicLayer\DreamTravel.Workflows\DreamTravel.Workflows.csproj`
    (re-verify the `..` segment count against a sibling test project while drafting).
- Add the same **commented** `<NuGetAuditSuppress>` for GHSA-2m69-gcr7-jv3q (SQLite packages flow in
  transitively; the suppress keeps the test build output clean — warning-level, not a gate).

### `SQLiteRepositoryTests.cs` (relocated, repo round-trip)
- Copy from the library's `SqliteRepositoryTests.cs`; change the namespace, the test class name to
  `SQLiteRepositoryTests`, and the `using`/type references for `SQLiteStoryRepository` (new namespace +
  ADR-001 name). It already depends only on public `Story` types (`StoryInstance`, `ChapterInfo`, `Auid`,
  `StoryStatus`), so no fixture changes are needed.

### `SQLiteRegistrationTests.cs` (relocated, DI registration)
- Relocate the two cases (rename `Sqlite` → `SQLite`):
  `UseSqliteStoryRepository_ReplacesDefault_CreatesDatabaseFile_AndSupportsFullCycle` and
  `UseSqliteStoryRepository_ConfigureCallback_AppliesOptions_AndRegistersThemInDI`.
- These referenced the pausable `LifecycleStoryV1` fixture in `Story.Tests` (which is **not** moving and
  must **not** be referenced). Build the provider **inline** instead:
  `new ServiceCollection().AddLogging().RegisterStories().UseSQLiteStoryRepository("Data Source=<temp>")`.
  `RegisterStories(Action<StoryOptions>? configure = null, params Assembly[] assemblies)` accepts an empty
  assembly list, so **no story fixture is needed** for the registration-level assertions.
- Assert: `IStoryRepository` resolves to `SQLiteStoryRepository`; the DB file is created (the repo ctor
  calls `EnsureDatabaseInitialized()`); `SQLiteStoryRepositoryOptions` is registered with the configured
  values; `StoryManager` resolves.

### `SampleOrderWorkflowSQLiteTests.cs` (NEW — the (c) end-to-end coverage)
Drive the real `SampleOrderWorkflowStory` through `StoryManager` on a real SQLite file DB. This is the
unique value of option (c): it exercises engine → SQLite `SaveAsync` at the pause boundary → SQLite
`FindById` + context deserialize on resume → continue → complete, **across repository instances**.

- **Provider build (per test, unique temp DB path):**
  `new ServiceCollection().AddLogging().RegisterStories(typeof(SampleOrderWorkflowStory).Assembly).UseSQLiteStoryRepository("Data Source=<unique temp>")`,
  then resolve `StoryManager`.
- **Driver API (verified):**
  - `StartStory<SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(input, idempotencyKey: null, ct)` → `Result<StoryInstance>`
  - `ResumeStory<SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(storyId, userInput, ct)` → `Result<StoryInstance>`; `userInput` is a `JsonElement?` — serialize a `CustomerDetailsInput { Name, Address }` (namespace `SolTechnology.Core.Journey.Workflow.Steps.Dtos`) to `JsonElement`.
  - `StoryStatus` values: `Created, Running, WaitingForInput, Completed, Failed, Cancelled`.
- **Test cases (none of these exist today — the only current coverage is the API-level, in-memory
  `tests/Component/SampleOrderWorkflow/SampleOrderWorkflowTests.HappyPath`):**
  1. **Happy full cycle + cross-instance durability (headline).** `StartStory` with `{OrderId:"2137", Quantity:17}`
     → assert `WaitingForInput` + non-empty `StoryId`. Construct a **brand-new** `SQLiteStoryRepository`
     on the same DB file and `FindById(storyId)` → assert the paused `StoryInstance` was persisted (proves
     serialize-to-SQLite at the pause). Then `ResumeStory` with `{Name:"Adus", Address:"yes"}` → assert
     `Completed`, `History` non-empty, and the projected `SampleOrderResult` (`IsSuccessfullyProcessed == true`,
     `Name == "Adus"`, `FinalMessage` set). Re-read once more from a fresh repo → assert `Completed` persisted.
  2. **Invalid customer input on resume.** `StartStory` → `WaitingForInput`; `ResumeStory` with `{Name:"", Address:""}`
     → `CustomerDetailsChapter.ReadWithInput` returns `Result.Fail("Name and Address cannot be empty.")`.
     Assert the story does **not** complete and stays `WaitingForInput` (the interactive-chapter retry
     contract — **verify the exact post-failure status against `StoryEngine`/`InteractiveChapter`
     behaviour while drafting** and assert the observed value explicitly). Proves the failure path persists.
  3. **Backend automated-chapter failure.** `StartStory` with `Quantity = -1` → `WaitingForInput`;
     `ResumeStory` with valid customer input → `BackendProcessingChapter` fails ("Invalid quantity for
     processing."). Assert `Failed` status, then re-read from a fresh repo to confirm `Failed` is persisted.
     (`OrderId` containing `FAIL_PAYMENT` is an equivalent trigger — optional extra case.)
- **Hygiene:** unique temp DB file per test; delete it in `TearDown` (mirror the `SQLiteRepositoryTests`
  cleanup). Note in the class `<summary>` that this is the SQLite-backed complement to the in-memory,
  API-level `SampleOrderWorkflowTests`.

### slnx
- Register the project in `SolTechnology.Core.slnx` under the existing
  `<Folder Name="/SampleApps/DreamTravel/tests/Unit/">` block.

### Two-constructor DI note (carry into Step 03 too)
- `SQLiteStoryRepository` keeps both ctors: `(SQLiteStoryRepositoryOptions? options = null)` and
  `(string connectionString)`. The container selects the options ctor because the sibling `string` ctor
  is not DI-resolvable (no registered `string`). `SQLiteRepositoryTests` uses the `string` ctor directly,
  so both must stay.

## Acceptance criteria
- `dotnet test` on the new project passes: relocated repo round-trip tests + the two adapted registration
  tests + all three `SampleOrderWorkflowSQLiteTests` cases.
- The new test project references `DreamTravel.SQLite` **and** `DreamTravel.Workflows` only — no
  `ProjectReference` back into `tests/SolTechnology.Core.Story.Tests`, and no dependency on
  `LifecycleStoryV1`.
- The existing `tests/Component/SampleOrderWorkflow/SampleOrderWorkflowTests.cs` stays unchanged (the new
  SQLite tests are additive — different entry point and persistence).
- `Story.Tests` still builds and passes (its SQLite tests are untouched in this step).

## Open questions
- None. OQ1 resolved (`DreamTravel.SQLite` naming). Coverage decision resolved (option (c), expanded).

