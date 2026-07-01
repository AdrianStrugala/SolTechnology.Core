---
adr: 013-release-1.0
step: 05b of 11
status: done
---

<!-- Authored (2026-06-30): folds in the ACCEPTED "Tale noun + package rename" maintainer decision.
     Split out of step 05 because the Story->Tale rebrand is now the single largest rename in the
     plan (new package identity + ~15 public types + namespace collapse + tests + sample). This step
     is build-affecting CODE ONLY (src + tests + sample + .slnx); the governing PROSE (the
     command-query-event-story skill, ClaudeCodingGuide §0/§3/§4, CLAUDE.md, docs/Story.md->Tale.md,
     diagrams) is owned by step 11 — the skill cites the guide ("guide wins, same PR" §19), so skill
     and guide must move together, and prose does not affect build-greenness so it is safe to defer
     past this wave. Planned on B2 (full Tale* rebrand); the B1/B2 choice (step 00) only changes how
     many type names the docs enumerate, never whether this code is written.
     2026-06-30 (sub-questions answered): 13a→B2 CONFIRMED (no structural change here — already B2);
     13c→CONFIRMED change the route, breaking accepted. Folded in: base `[Route("api/story")]→[api/tale]`,
     sample `DreamTravelStoryController→DreamTravelTaleController` + `[api/dreamtravel/story]→[api/dreamtravel/tale]`,
     `SQLiteStoryRepository→SQLiteTaleRepository` (+ its `…Options`), `SaveCityStory→SaveCityTale` use-case
     namespace, all hard-coded test URL strings, and **regenerating the verified contract snapshot** as a
     named deliverable. Full coherence: no `Story` framework token left in the sample's controllers,
     routes, class names, usings, namespaces, or comments. -->

# Step 05b: Tale rebrand + package rename (`SolTechnology.Core.Story` → `SolTechnology.Core.Tale`)

## Summary
Adopt the **Tale** noun for the authoring layer and rename the package. `SolTechnology.Core.Story`
becomes **`SolTechnology.Core.Tale`** (new package identity), the base class
`StoryHandler<TInput,TContext,TOutput>` becomes **`TaleHandler<,,>`**, the `…Story` subclass suffix
becomes **`…Tale`**, and the whole infrastructure surface rebrands to `Tale*` (B2). Because a
package/namespace rename cannot be done callee-before-caller across assemblies, the package, every
`Story*` type, **and** every call site (src + tests + sample + `.slnx`) move **together in one wave**
so `dotnet build SolTechnology.Core.slnx` and the DreamTravel solution stay green. The `Tale<>` /
`Tale<TContext,TOutput>` / `Tell()` / `TaleStep` brand types **stay** (they were already Tale). Pure
mechanical rename — one large PR. **13c (CONFIRMED — breaking accepted):** the base REST route
`[Route("api/story")]` becomes `[Route("api/tale")]`, the sample's `DreamTravelStoryController` +
`api/dreamtravel/story` route + `SQLiteStoryRepository` + `SaveCityStory` namespace all rebrand to
Tale, every hard-coded test URL string swaps `story`→`tale`, and the single component contract
snapshot is **regenerated on purpose** (a named deliverable, green after regen). **No `[Obsolete]` shims**; the old package id is deprecated +
unlisted server-side (step 10), not type-forwarded.

> **B2 (CONFIRMED 2026-06-30).** Full rebrand to `Tale*` and **collapse** the `…Story.Tale`
> sub-namespace into the root so `Tale<>` / `TaleStep` live in `SolTechnology.Core.Tale` (never an
> ugly `SolTechnology.Core.Tale.Tale`). B1 (keep infra as `Story*` inside the `.Tale` package) was
> rejected — it would reintroduce the two-noun confusion this change exists to kill. 13a needed no
> structural change here: this step was authored on B2.

## Affected components

### `src/` — the package
- `src/SolTechnology.Core.Story/` **folder → `src/SolTechnology.Core.Tale/`** (rename).
- `…/SolTechnology.Core.Story.csproj` **→ `SolTechnology.Core.Tale.csproj`** — EDIT — `PackageId`,
  `AssemblyName`, `RootNamespace`, `Product` all `SolTechnology.Core.Story` → `SolTechnology.Core.Tale`;
  refresh `<Description>` / `<PackageTags>` to the Tale brand. **Leave** the readme packaging
  (`<PackageReadmeFile>docs\readme.md</PackageReadmeFile>` + `<None Include="..\..\docs\Story.md" …
  PackagePath="docs\readme.md" />`) pointing at **`docs\Story.md`** for now — `Story.md` still exists
  and is non-empty, so pack stays green; step 11 flips the include to `docs\Tale.md` when it moves the
  doc content (avoids an empty-readme pack window). Do **not** add `<Version>` here — step 08 owns the
  `1.0.0` flip.
- Rename the type files and types (B2):
  - `StoryHandler.cs` → `TaleHandler.cs` — `StoryHandler<TInput,TContext,TOutput>` → `TaleHandler<,,>`.
  - `ModuleInstaller.cs` — `RegisterStories` → **`AddSolTale`**; `StoryHandlerRegistry` →
    `TaleHandlerRegistry`.
  - `StoryOptions.cs` → `TaleOptions.cs` — `StoryOptions` → `TaleOptions`; `StoryIdPrefix` →
    `TaleIdPrefix` (default `"STR"` stays unless the gate says otherwise); `RestrictControllerToRegisteredHandlers` keeps its name.
  - `StoryErrors.cs` → `TaleErrors.cs` — `StoryPausedError` → `TalePausedError`;
    `StoryCancelledError` → `TaleCancelledError` (keep `StoryId`/`ChapterId` property names? rename the
    `StoryId` property → `TaleId` for coherence — verify the controller + repository serialization).
  - `Api/StoryController.cs` → `Api/TaleController.cs` — `StoryController` → `TaleController`;
    `StoryInstanceDto` → `TaleInstanceDto`; `StoryResultDto` → `TaleResultDto`; base route attribute
    **`[Route("api/story")]` → `[Route("api/tale")]`** (13c CONFIRMED — breaking accepted). The public
    action methods (`StartStory`/`ResumeStory`/`CancelStory`/`GetStoryState`/`GetStoryResult`) and the
    `{storyId}` route-template token are **not** renamed by this step — see the residual-token note
    under *Changes*.
  - `Builder/IStoryBuilder.cs`/`StoryBuilder.cs`/`StoryBuilderExtensions.cs` → `ITaleBuilder` /
    `TaleBuilder` / `TaleBuilderExtensions`; `UseStoryRepository<T>` → `UseTaleRepository<T>`;
    `UseInMemoryStoryRepository` → `UseInMemoryTaleRepository`.
  - `Orchestration/StoryManager.cs` → `TaleManager.cs` (`StoryManager` → `TaleManager`);
    `Orchestration/StoryEngine.cs` → `TaleEngine.cs` (`StoryEngine` + `StoryJsonOptions` →
    `TaleEngine` + `TaleJsonOptions` — both `internal`).
  - `Persistence/IStoryRepository.cs`/`InMemoryStoryRepository.cs` → `ITaleRepository` /
    `InMemoryTaleRepository`.
  - `Models/StoryInstance.cs`/`StoryStatus.cs` → `TaleInstance` / `TaleStatus`.
  - `Tale/Tale.cs`, `Tale/TaleStep.cs` — types **unchanged** (already Tale); only their **namespace**
    changes (collapse — see below).
- **Namespace map (B2 collapse):**
  - `SolTechnology.Core.Story` → `SolTechnology.Core.Tale`
  - `SolTechnology.Core.Story.Builder` → `SolTechnology.Core.Tale.Builder`
  - `SolTechnology.Core.Story.Orchestration` → `SolTechnology.Core.Tale.Orchestration`
  - `SolTechnology.Core.Story.Persistence` → `SolTechnology.Core.Tale.Persistence`
  - `SolTechnology.Core.Story.Api` → `SolTechnology.Core.Tale.Api`
  - `SolTechnology.Core.Story.Models` → `SolTechnology.Core.Tale.Models`
  - `SolTechnology.Core.Story.Tale` → **`SolTechnology.Core.Tale`** (COLLAPSE into root — `Tale<>` and
    `TaleStep` move to the root namespace; drop the now-redundant `using SolTechnology.Core.Story.Tale;`
    inside the assembly).

### `tests/`
- `tests/SolTechnology.Core.Story.Tests/` **folder → `tests/SolTechnology.Core.Tale.Tests/`**;
  `…csproj` → `SolTechnology.Core.Tale.Tests.csproj` with the `ProjectReference` repointed to
  `..\..\src\SolTechnology.Core.Tale\SolTechnology.Core.Tale.csproj`.
- All test files (`namespace SolTechnology.Core.Story.Tests;` × ~13) → `…Tale.Tests`; every
  `using SolTechnology.Core.Story*` and symbol (`StoryHandler`, `StoryManager`, `StoryController`,
  `StoryEngine`, `RegisterStories`, `UseStoryRepository`, `StoryOptions`, `StoryInstance`, …) → the
  `Tale*` / `AddSolTale` equivalents. Rename the Story-named fixtures/files for coherence
  (`StoryHandlerTests.cs`/`StoryControllerTests.cs`/`StoryEngineTests.cs` → `Tale…`; internal fixture
  types such as `LifecycleStoryV1` → `LifecycleTaleV1`). `RegistrationDefaultsTests.cs`’s
  `RegisterStories()` assertions → `AddSolTale()`.

### `sample-tale-code-apps/DreamTravel/**`
- `DreamTravel.SQLite/SQLiteStoryRepository.cs` **→ `SQLiteTaleRepository.cs`** — `using …Story.Models`/`.Persistence`
  → `…Tale.*`; class `SQLiteStoryRepository` → **`SQLiteTaleRepository`**; `: IStoryRepository` →
  `: ITaleRepository`; XML-doc `<see cref="IStoryRepository"/>` → `ITaleRepository`. Its options class
  renames with it: `DreamTravel.SQLite/SQLiteStoryRepositoryOptions.cs` **→ `SQLiteTaleRepositoryOptions.cs`**
  (`SQLiteStoryRepositoryOptions` → **`SQLiteTaleRepositoryOptions`**; the stale XML-doc helper ref
  `<c>UseSQLiteStoryRepository</c>` → `UseSQLiteTaleRepository`). `DreamTravel.SQLite.csproj`
  `ProjectReference` → `SolTechnology.Core.Tale.csproj`. (13c CONFIRMED — full coherence; the old plan
  kept these names, that is now reversed: no `Story` token survives in the sample.)
- `DreamTravel.Api/Controllers/DreamTravelStoryController.cs` **→ `DreamTravelTaleController.cs`** —
  rename file + class `DreamTravelStoryController` → **`DreamTravelTaleController`**; usings
  `…Story`/`.Story.Api`/`.Story.Orchestration` → `…Tale.*`; `: StoryController` → `: TaleController`;
  route `[Route("api/dreamtravel/story")]` → **`[Route("api/dreamtravel/tale")]`** (13c CONFIRMED);
  ctor param types `StoryManager` → `TaleManager`, `StoryHandlerRegistry` → `TaleHandlerRegistry`,
  `StoryOptions` → `TaleOptions`, `ILogger<StoryController>` → `ILogger<TaleController>`; XML-doc
  `<see cref="StoryController"/>` → `<see cref="TaleController"/>` (and "Story API controller" prose →
  "Tale API controller"). **This deliberately churns the verified contract snapshot** — see the
  *Verified contract snapshot* deliverable below.
- `DreamTravel.Workflows/SampleOrderWorkflow/` — **`SampleOrderWorkflowStory.cs` → `SampleOrderWorkflowTale.cs`**;
  class `SampleOrderWorkflowStory` → `SampleOrderWorkflowTale`; `ILogger<SampleOrderWorkflowStory>` →
  `ILogger<SampleOrderWorkflowTale>`; base `StoryHandler<,,>` → `TaleHandler<,,>`; usings `…Story` +
  `…Story.Tale` → `…Tale` (collapsed). `SampleOrderContext.cs` + `Chapters/*` → `using …Tale`.
- `DreamTravel.Workflows/ModuleInstaller.cs` — usings `…Story`/`.Story.Builder` → `…Tale.*`;
  `AddFlows` returns `IStoryBuilder` → `ITaleBuilder`; `Action<StoryOptions>` → `Action<TaleOptions>`;
  body `services.RegisterStories(configure, typeof(SampleOrderWorkflowStory).Assembly)` →
  `services.AddSolTale(configure, typeof(SampleOrderWorkflowTale).Assembly)`. `DreamTravel.Workflows.csproj`
  `ProjectReference` → `…Tale.csproj`.
- `DreamTravel.DomainServices/CityDomain/CityDomainService.cs` — usings `…Story`/`.Story.Tale` →
  `…Tale`; base `StoryHandler<,,>` → `TaleHandler<,,>`; the `/// Story for saving a city…` XML-doc →
  "Tale…". **`CityDomainService` keeps its DomainService name** (a `TaleHandler` hosted in a domain
  service — call this out, do **not** suffix it `…Tale`). `DreamTravel.DomainServices/ModuleInstaller.cs`
  — `using …Story` → `…Tale`; `RegisterStories` → `AddSolTale`. `DreamTravel.DomainServices.csproj`
  `ProjectReference` → `…Tale.csproj`.
- **`DreamTravel.DomainServices/CityDomain/SaveCityStory/` folder → `SaveCityTale/` (13c full
  coherence — was scoped out, now renamed).** The maintainer's "no `Story` token left in the sample"
  reaches this use-case namespace. Rename the folder + the `namespace DreamTravel.DomainServices.CityDomain.SaveCityStory`
  (→ `…SaveCityTale`) across all 7 files — `SaveCityInput.cs`, `SaveCityNarration.cs`, `SaveCityResult.cs`,
  and `Chapters/{LoadCityForSave,PersistCity,AssignAlternativeNameChapter,IncrementSearchCountChapter}.cs`
  — plus the `// … SaveCityStory …` doc comments inside them, and the two `using` lines in
  `CityDomainService.cs` (`…CityDomain.SaveCityStory;` + `…SaveCityStory.Chapters;`). Step 11 repoints
  the renamed skill's reference path (`…/SaveCityStory/` → `…/SaveCityTale/`) and the guide's
  `typeof(SaveCityStory)` examples to match.
- `DreamTravel.Queries/CalculateBestPath/CalculateBestPathTale.cs` — already Tale-named; base
  `StoryHandler<,,>` → `TaleHandler<,,>`; usings `…Story`/`.Story.Tale` → `…Tale`. Chapters +
  `CalculateBestPathContext.cs` + `ModuleInstaller.cs` (`RegisterStories` → `AddSolTale`) updated.
  `DreamTravel.Queries.csproj` `ProjectReference` → `…Tale.csproj`.
- **Load-bearing runtime strings (13c — every URL swaps `story`→`tale`):**
  `tests/Component/SampleOrderWorkflow/SampleOrderWorkflowTests.cs` hard-codes **five** URLs against
  the now-renamed `api/dreamtravel/tale` route. Update all five (they 404 at **runtime**, not compile,
  if missed):
  - line 26 `"/api/dreamtravel/story/SampleOrderWorkflowStory/start"` →
    **`"/api/dreamtravel/tale/SampleOrderWorkflowTale/start"`** (both the route segment *and* the
    `handlerType.Name` key change);
  - lines 46, 56, 72 `$"/api/dreamtravel/story/{storyId}"` → **`$"/api/dreamtravel/tale/{storyId}"`**;
  - line 83 `$"/api/dreamtravel/story/{storyId}/result"` → **`$"/api/dreamtravel/tale/{storyId}/result"`**.
  These tests also use `StoryInstanceDto`/`StoryStatus`/`StoryId` (→ `TaleInstanceDto`/`TaleStatus`/`TaleId`)
  and `using …Story.Api`/`.Models` (→ `…Tale.*`).
- **SQLite E2E test:** `tests/Component/SQLiteTests/SampleOrderWorkflowSQLiteTests.cs` — `using …Story`/`.Builder`/`.Models`/`.Orchestration`/`.Persistence`
  → `…Tale.*`; `<see cref="SampleOrderWorkflowStory"/>`/`<see cref="StoryManager"/>` → `…Tale`/`TaleManager`;
  `StoryManager` → `TaleManager`; `SampleOrderWorkflowStory` → `SampleOrderWorkflowTale`; `StoryStatus`
  → `TaleStatus`; `instance.StoryId` → `instance.TaleId`; `new SQLiteStoryRepository(…)` →
  `new SQLiteTaleRepository(…)` (lines 56/73/101/138); `new SQLiteStoryRepositoryOptions{…}` →
  `new SQLiteTaleRepositoryOptions{…}` (line 147); `services.RegisterStories(…)` → `services.AddSolTale(…)`
  (line 148); `.UseStoryRepository<SQLiteStoryRepository>()` → `.UseTaleRepository<SQLiteTaleRepository>()`
  (line 149).

### `.slnx`
- Line 113 `src/SolTechnology.Core.Story/SolTechnology.Core.Story.csproj` → `…Tale/…Tale.csproj`.
- Line 94 `tests/SolTechnology.Core.Story.Tests/…csproj` → `…Tale.Tests/…csproj`.
- A repo-wide search for `Story.Tests` finds it only in `.slnx`, historical ADR docs, and the test
  files’ own namespaces — **no CI `.yml` or test-runner script hard-codes the project name**, so the
  folder/csproj rename + the `.slnx` row update suffice for discovery (`runTests.ps1` /
  `publishPackages.yml` drive off the solution, not a literal `Story.Tests`).

## Changes
- Apply the type + namespace map above in **one wave**. The brand types `Tale<>`,
  `Tale<TContext,TOutput>`, `Tell()`, `TaleStep` keep their names; only their **namespace** collapses
  to the root.
- **Registration:** `RegisterStories` → `AddSolTale` (matches the `AddSol*` convention from steps
  03–05); the builder continuations `UseTaleRepository<T>` / `UseInMemoryTaleRepository` stay
  **unprefixed** (builder-scoped continuations — same rule answers 2/4 applied to step 05).
- **Symbol-string sweep (repo-wide for the renamed symbols, `src tests sample-tale-code-apps` only —
  docs are owned by steps 09/10/11).** No `<c>` / `<see cref>` XML-doc, comment, `throw`, or log
  string may still name an old symbol. Known hits: `InteractiveChapter.cs:36` throws
  `"…orchestrate through StoryManager/StoryHandler."` → `"…through TaleManager/TaleHandler."`; the
  builder/options XML-doc comments naming `RegisterStories` / `UseStoryRepository` /
  `UseInMemoryStoryRepository` → `AddSolTale` / `UseTaleRepository` / `UseInMemoryTaleRepository`;
  `TaleHandler.cs` (ex-`StoryHandler.cs`) XML-doc example `public class SaveCityStory : StoryHandler<…>`
  → `public class SaveCityTale : TaleHandler<…>`; `Program.cs:111` comment `// … migrated to Story
  framework …` → "Tale framework". Verify with
  `grep -rn "SolTechnology\.Core\.Story\|StoryHandler\|StoryManager\|StoryController\|StoryEngine\|StoryOptions\|IStoryRepository\|InMemoryStoryRepository\|StoryHandlerRegistry\|RegisterStories\|UseStoryRepository\|UseInMemoryStoryRepository\|StoryPausedError\|StoryCancelledError\|StoryInstance\|StoryStatus\|StoryInstanceDto\|StoryResultDto\|DreamTravelStoryController\|SQLiteStoryRepository\|SampleOrderWorkflowStory\|SaveCityStory\|api/dreamtravel/story\|api/story" src tests sample-tale-code-apps`
  returning only the `Tale*` / `AddSolTale` / `api/tale` names. **Deliberately kept** (not Story-framework
  tokens): `CityDomainService` (a `TaleHandler` hosted in a DomainService — its noun is intentional),
  and the Core public **method names** + route-template token covered by the residual-token note below.
- **Residual `Story` tokens NOT renamed by this step (flagged).** Two categories survive because 13a
  CONFIRMED kept 05b's structural scope as-is (B2 renames *types + namespaces*, not method bodies):
  (a) the `TaleManager` / `TaleController` public **method names** `StartStory` / `ResumeStory` /
  `CancelStory` / `GetStoryState` / `GetStoryResult`, and (b) the `{storyId}` **route-template token**
  (`[HttpPost("{storyId}")]` etc.) and its `[FromRoute] string storyId` params. Renaming these is a
  Core-API change beyond the type/namespace rebrand — left to a separate maintainer call (surfaced in
  the review). Consequence: `manager.StartStory<SampleOrderWorkflowTale, …>` still reads `StartStory`
  in the sample test, and the regenerated snapshot keeps the `{storyId}` path token. The "no `Story`
  token" coherence goal therefore covers controllers, routes, class names, usings, namespaces, and
  comments — **not** these Core method names / route params.
- **Verified contract snapshot — regeneration is a named deliverable (13c).** Renaming the sample
  controller + route churns the single component snapshot
  `sample-tale-code-apps/DreamTravel/tests/Component/ContractTests.ContractTest_reviewChangesToTheApi.verified.txt`:
  the OpenAPI tag `DreamTravelStory → DreamTravelTale` (lines 45/74/98/114/131/750) and the route
  segment `…/dreamtravel/story/… → …/dreamtravel/tale/…` (lines 42/71/128). Regenerate the `.verified.txt`
  (accept the `.received.txt`) **in this PR** and confirm `ContractTest_reviewChangesToTheApi` is green
  after regen. This is the only `*.verified.txt` snapshot under the DreamTravel component tests.
- No `[Obsolete]` shims; no type-forwarding. The old `SolTechnology.Core.Story` package is frozen at
  `0.8.0` and deprecated + unlisted on nuget.org via the **step 10** runbook (mirrors the
  `ApiClient → HTTP` playbook — there is no buildable source under the old id to annotate).

## Acceptance criteria
- [ ] No `SolTechnology.Core.Story` package, type, or namespace remains in `src/`; the project is
      `SolTechnology.Core.Tale` (folder, csproj, `PackageId`, `AssemblyName`, `RootNamespace`).
- [ ] `StoryHandler<,,>` → `TaleHandler<,,>` and every B2 type (`TaleManager`, `TaleController` +
      `TaleInstanceDto`/`TaleResultDto`, `ITaleRepository`/`InMemoryTaleRepository`, `TaleOptions`,
      `TalePausedError`/`TaleCancelledError`, `ITaleBuilder`/`TaleBuilder`, `TaleHandlerRegistry`,
      `TaleInstance`/`TaleStatus`, internal `TaleEngine`/`TaleJsonOptions`) is in place; `Tale<>` /
      `TaleStep` live in the root `SolTechnology.Core.Tale` namespace (no `…Tale.Tale`).
- [ ] The grep above returns only the new names across `src tests sample-tale-code-apps` (XML-doc,
      comments, throw/log strings included), except `CityDomainService` and the flagged residual Core
      method names (`StartStory`/`ResumeStory`/…) + `{storyId}` route-template token.
- [ ] `tests/SolTechnology.Core.Tale.Tests` builds and runs; `.slnx` lines 94 + 113 point at `.Tale`.
- [ ] **13c:** `DreamTravelStoryController → DreamTravelTaleController`, base + sample routes →
      `api/tale` / `api/dreamtravel/tale`, `SQLiteStoryRepository → SQLiteTaleRepository` (+ `…Options`),
      `SaveCityStory → SaveCityTale` namespace, `SampleOrderWorkflowStory → SampleOrderWorkflowTale`;
      **all five** `SampleOrderWorkflowTests.cs` URL strings swap `story`→`tale`; `CityDomainService`
      keeps its name. No `Story` framework token remains in the sample's controllers, routes, class
      names, usings, namespaces, or comments (residual Core method names excepted).
- [ ] **The `ContractTests.ContractTest_reviewChangesToTheApi.verified.txt` snapshot is regenerated**
      (tag `DreamTravelStory → DreamTravelTale`, route `…/story/… → …/tale/…`) and the contract test is
      green after regen.
- [ ] `dotnet build SolTechnology.Core.slnx` green; the renamed Tale tests + DreamTravel
      component/E2E (incl. SQLite pause/resume) pass; `dotnet pack` produces
      `SolTechnology.Core.Tale.*.nupkg` (readme still sourced from `docs/Story.md` until step 11) and
      **never** `SolTechnology.Core.Story.*`.

## Open questions
- **13a — RESOLVED → B2** (full `Tale*` rebrand + namespace collapse). No structural change here; 05b
  was authored on B2.
- **13c — RESOLVED → change the route, breaking accepted.** Base + sample routes → `api/tale` /
  `api/dreamtravel/tale`; sample controller + `SQLiteStoryRepository` + `SaveCityStory` renamed; the
  verified snapshot is regenerated as a deliverable.
- **Residual (flagged for the maintainer):** the Core public method names
  `StartStory`/`ResumeStory`/`CancelStory`/`GetStoryState`/`GetStoryResult` and the `{storyId}`
  route-template token keep their `Story` spelling (a Core-API rename beyond the type/namespace B2
  rebrand). Rename them too, or accept the residue? Not blocking — 05b ships either way.













