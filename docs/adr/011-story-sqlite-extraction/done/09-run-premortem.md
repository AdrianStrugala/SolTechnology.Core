---
adr: 011-story-sqlite-extraction
step: 09 of 09
status: done
---

# Step 09: Run the premortem (mandatory gate)

## Summary
Before any production or test code is written, run the
[premortem](../../../../.github/skills/premortem/SKILL.md) skill against this plan. Implementation is
**blocked** until premortem returns **Go** or **Go with mitigations**. This is the last step in the
plan by ADR-006 convention.

## Affected components
- none (gate only)

## Details
- ✅ **Both open questions were resolved by the user (2026-06-22)** — no longer blockers:
  1. **OQ1 — project name.** Per [ADR-001](../../001-acronym-capitalization-refactoring.md): **`DreamTravel.SQLite`**
     (`SQL` acronym uppercase). The relocated symbols follow suit — `SQLiteStoryRepository`,
     `SQLiteStoryRepositoryOptions`, `UseSQLiteStoryRepository`, test project `DreamTravel.SQLite.UnitTests`,
     test class `SQLiteRepositoryTests`.
  2. **OQ2 — phantom persistence API.** Confirmed a **docs fix** to the real
     `RegisterStories().UseSQLiteStoryRepository(...)` API; the `StoryOptions.WithSqlitePersistence(...)` /
     `WithInMemoryPersistence()` / `WithoutPersistence()` methods are **not** implemented.
- Run the premortem skill imagining each failure mode has shipped to production. Seed it with the
  module-specific risks already surfaced here:
  - **Public-seam regression:** the container fails to construct `SQLiteStoryRepository` because
    `SQLiteStoryRepositoryOptions` was not registered before `UseStoryRepository<SQLiteStoryRepository>()`
    (mitigation: the Step 03 extension always registers options first; covered by a Step 04 test).
  - **Lost-code window:** library copies deleted before the new home is green (mitigated by sequencing —
    deletes happen in Step 05/06, after Steps 01–04 are green).
  - **Newtonsoft over-removal:** removing `Newtonsoft.Json` breaks an unseen consumer of the library
    (mitigated by finding #2 — zero `Newtonsoft`/`JsonConvert` usages; verify once more at implementation).
  - **Audit still red:** a transitive path keeps NU1903 in `src/` after Step 06 (mitigated by the Step 08
    `--force-evaluate` check; if it persists, investigate whether another `src/` package pulls SQLitePCLRaw).
  - **Sample CI cost:** the new SQLite test project adds a file-DB test to DreamTravel CI (accepted; the
    tests already self-clean temp DB files in `TearDown`).
  - **Cross-assembly test coupling (coverage decision (c)):** `DreamTravel.SQLite.UnitTests` now references
    `DreamTravel.Workflows` to drive `SampleOrderWorkflowStory`. Risk: the workflow's chapter logic changes
    and silently breaks the SQLite tests for a non-persistence reason. Accepted — it is exactly the
    end-to-end path we want guarded; the repo-level `SQLiteRepositoryTests` still isolate pure persistence.
  - **Unverified failure-status assertion:** Step 04 case 2 asserts the post-invalid-input status
    (`WaitingForInput` vs `Failed`). Confirm the actual `InteractiveChapter`/`StoryEngine` behaviour while
    drafting the test and assert the observed value — do not hard-code an assumption.
- Optionally pair with the [blue-red-team](../../../../.github/skills/blue-red-team/SKILL.md) skill on the
  "relocate to sample" vs "new `SolTechnology.Core.Story.Sqlite` package" fork (ADR Alternative 2).

## Acceptance criteria
- Premortem verdict recorded (**Go** / **Go with mitigations** / **No-Go**) with any mitigations folded
  back into the relevant step files.
- Both open questions answered and reflected in Steps 01/02/04 (name) and Step 07 (docs).
- Only after a Go verdict does implementation begin (Step 01).

## Open questions
- None. OQ1 (naming) and OQ2 (docs) are resolved; the coverage decision is option (c) (Step 04). The
  premortem itself is the remaining gate before coding.

---

# Premortem — ADR-011: Extract SQLite Story persistence into DreamTravel (recorded 2026-06-22)

## Frame
- **Modules touched:** `SolTechnology.Core.Story` (public API removal + 3 package removals + suppress
  removal), `tests/SolTechnology.Core.Story.Tests` (test + suppress removal),
  `sample-tale-code-apps/DreamTravel` (new `DreamTravel.SQLite` DataLayer project + new unit-test project).
- **API delta (removed, public):** `SqliteStoryRepository`, `SqliteStoryRepositoryOptions`, and both
  `UseSqliteStoryRepository(...)` overloads leave `SolTechnology.Core.Story`'s surface
  (`Builder/StoryBuilderExtensions.cs:32,46`; `Persistence/SqliteStoryRepository.cs:13`;
  `Persistence/SqliteStoryRepositoryOptions.cs:8`). **Kept:** `IStoryRepository`,
  `InMemoryStoryRepository`, `UseInMemoryStoryRepository()`, `UseStoryRepository<T>(ServiceLifetime)`.
- **Semver:** **MINOR** under 0.x convention (`Story` 0.7.0 → 0.8.0) — a breaking removal, permitted pre-1.0.
- **Consumers in workspace:** none call the SQLite API — verified the only `UseSqliteStoryRepository`
  reference in DreamTravel is a doc comment (`DreamTravel.Workflows/ModuleInstaller.cs:12`); every host
  uses the in-memory default (`DreamTravel.Api/Program.cs` `AddFlows()`).
- **External consumers:** invisible NuGet downloaders of `SolTechnology.Core.Story` who call
  `UseSqliteStoryRepository(...)` — the real blast radius.

## Imagined failure (worst credible end state)
Two weeks after publishing `Story` 0.8.0, a public consumer who shipped a paused interactive workflow
on the built-in SQLite provider upgrades, their `dotnet build` breaks (`UseSqliteStoryRepository` is
gone), and — worse — a teammate "fixes" it by re-adding `Microsoft.Data.Sqlite` ad-hoc, silently
re-importing the very CVE we removed. Meanwhile our own `Story.Tests` build stays red because a
transitive path still trips NU1903, or the new DreamTravel SQLite test flakes on a leftover temp DB file.

## Scenarios

| # | Scenario | Trigger (file:line) | Blast | Sev | Lik | Existing control | Mitigation |
|---|---|---|---|---|---|---|---|
| 1 | External consumer calling `UseSqliteStoryRepository(...)` breaks on upgrade | `StoryBuilderExtensions.cs:32,46` (removed) | public NuGet | H | M | ADR-011 documents the breaking removal | **Required:** bump `Story` 0.7.0 → 0.8.0 **and** add a migration breadcrumb in `docs/Story.md` ("copy `DreamTravel.SQLite`"). Step 07 frames the copy; **version bump is an unfilled gap** → fold into Step 06. |
| 2 | Version not bumped → 0.7.0 republished with a different public surface | no step edits `Story.csproj <Version>` | public NuGet | H | M | none — **gap** | **Required:** Step 06 bumps `<Version>0.8.0</Version>`. |
| 3 | NU1903 survives in `src/` via another transitive path after suppress removal | `Story.csproj` package removal (Step 06) | internal build + CI | H | L | Step 08 `dotnet restore --force-evaluate` check | Keep Step 08 as a hard gate; if it persists, no other `src/` package pulls SQLitePCLRaw (verified — only Story did). |
| 4 | Relocated repo fails to compile cross-assembly because it needs an `internal` Story type | `SqliteStoryRepository.cs:225,229,266,269` use **plain** `JsonSerializer`, **not** `internal StoryJsonOptions` | sample build | M | L | verified: zero `StoryJsonOptions` refs in the repo file | None needed — move is clean; no `InternalsVisibleTo`. Pre-existing History/Context serialization split is copied verbatim → no drift introduced. |
| 5 | DI can't construct `SQLiteStoryRepository` (options not registered before `UseStoryRepository<T>()`) | Step 03 extension order | sample runtime | M | L | Step 03 registers options first; Step 04 resolution test | Keep the Step 04 assertion that `IStoryRepository` resolves to `SQLiteStoryRepository`. |
| 6 | Removing `Newtonsoft.Json` breaks a consumer who relied on getting it transitively via Story | `Story.csproj` Newtonsoft removal (Step 06) | public NuGet | M | L | finding #2: zero `Newtonsoft`/`JsonConvert` in Story source | Accepted risk — relying on a transitive package is the consumer's bug; pre-1.0. |
| 7 | New SQLite test leaves temp DB files / flakes on parallel runs | Step 04 `SampleOrderWorkflowSQLiteTests` | DreamTravel CI | L | M | Step 04 mandates unique temp path + `TearDown` cleanup (mirrors `SqliteRepositoryTests`) | Keep the per-test unique path + cleanup. |
| 8 | Step 04 case 2 asserts the wrong post-invalid-input status (`WaitingForInput` vs `Failed`) | interactive-chapter failure contract (`StoryEngine`/`InteractiveChapter`) | sample test correctness | M | M | none — **gap** | **Required:** verify the actual status while drafting the test; assert the observed value, don't hard-code. |
| 9 | New `DreamTravel.SQLite` accidentally published to NuGet | `publishPackages.yml` packs an explicit list | public NuGet | M | L | the pack list names only `src/SolTechnology.Core.*`; samples are excluded (`publishPackages.yml:30–71`) | None needed — sample is not in the pack list. |
| 10 | `SQLiteStoryRepositoryOptions` default ctor side-effect (`Directory.CreateDirectory`) runs in tests building options for a temp path | `SqliteStoryRepositoryOptions.cs:33–41` | sample test env | L | L | pre-existing behaviour, copied verbatim | Accepted — tests pass an explicit `Data Source=<temp>` so the default path is overwritten; the dir creation is harmless. |

## Top 3 risks
1. **#2 (version not bumped)** — the one concrete, unfilled gap in the plan; without it 0.7.0 ships with a changed surface. Cheap to fix, high impact.
2. **#1 (consumer compile break)** — intended and ADR-documented, but external blast radius is real; the migration breadcrumb + version bump are the mitigation.
3. **#8 (unverified failure-status assertion)** — a correctness trap in the new test that must be resolved empirically, not assumed.

## Required mitigations before merge
- **Step 06:** bump `src/SolTechnology.Core.Story/SolTechnology.Core.Story.csproj` `<Version>` from `0.7.0` to `0.8.0` (folded in by this premortem).
- **Step 07:** ensure `docs/Story.md` carries a one-line migration breadcrumb for consumers who used the built-in SQLite provider ("copy `DreamTravel.SQLite`").
- **Step 08:** keep `dotnet restore SolTechnology.Core.slnx --force-evaluate` as a hard gate proving zero NU1903 in `src/`.
- **Step 04:** verify the actual post-invalid-input `StoryStatus` against `StoryEngine`/`InteractiveChapter` behaviour while drafting; assert the observed value.

## Accepted risks
- **#6** Newtonsoft transitive-reliance break — consumer bug, pre-1.0.
- **#10** options default-ctor directory creation — harmless, overridden by explicit temp connection strings in tests.

## Decision
**Go with mitigations.** No `H`-severity scenario lacks a plausible mitigation; the breaking removal is
intended and ADR-documented. The single unfilled gap (version bump, #2) is folded into Step 06; the
remaining required mitigations (#1 breadcrumb, #3 audit gate, #8 status verification) are already located
in Steps 07/08/04. Implementation may begin at Step 01 once Step 06 carries the version bump.

