---
spec: 2026-11-02-flagship-showcase-hygiene
step: 00
status: done
---

# Step 00: Run premortem (gate)

## Summary
A premortem is mandatory (`CLAUDE.md §4`): step 05 edits a `ModuleInstaller.cs` and moves a type
that DreamTravel persists as a Hangfire job argument. Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author
this plan, after `review` is `done` and after the core steps this feature depends on are `done`
(see the brief), so the evidence is current. Record the full output here and the verdict in
`summary.md` `premortem:`. This step changes no `src/` code.

## Affected components
- `docs/features/2026-11-02-flagship-showcase-hygiene/steps/00-run-premortem.md` — EDIT — append a `## Premortem` section (before `## Deviations`) with the full skill output
- `docs/features/2026-11-02-flagship-showcase-hygiene/summary.md` — EDIT — `premortem:` verdict
- Step files named by required mitigations — EDIT (by the executing `implement-plan` session)

## Changes

- **Modules touched:**
  - Root community files: `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CHANGELOG.md`, `.github/ISSUE_TEMPLATE/*`, `.github/PULL_REQUEST_TEMPLATE.md`, `.github/CODEOWNERS`, `dontreadme.md` Guards row (step 01; the file keeps its name, user, 2026-11-03).
  - CI: `.github/workflows/publishPackages.yml` (steps 02, 07), `unlistDeprecatedPackages.yml` and `update-nuget-stats.yml` (step 02), `.github/dependabot.yml` (step 02), `docs/CICD.md` (steps 02, 07; resync 2026-11-10).
  - DreamTravel: `DreamTravel.Api` controllers and `Program.cs`, `DreamTravel.Queries` (and its `.csproj`, which gains a `DreamTravel.GraphDatabase` reference), `DreamTravel.Domain`, `DreamTravel.Worker`, unit and component tests (including the Swagger contract snapshot), `README.md`, `initWorkspace.bat`, `docker-compose.yml` (steps 03, 05, 06).
  - Guidance: `docs/ClaudeCodingGuide.md`, `CLAUDE.md` (step 04).
  - `SolTechnology.Core.slnx` `/Docs/` entries (step 01).
- **Contract delta:** see the feature brief
  [`../../2026-11-02-flagship-showcase-hygiene.md`](../../2026-11-02-flagship-showcase-hygiene.md):
  - No public/protected symbol in `src/SolTechnology.Core.*` changes; no `C-nn` applies.
  - No package version or `PackageReference` changes in `src/`.
  - The `publish` job gains `needs: [build, dreamtravel-tests]` (step 07): a failing DreamTravel test blocks a tag publish.
  - Semver: none for the packages.
- **`ModuleInstaller.cs` edits:**
  - `sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Queries/ModuleInstaller.cs`: step 05 (BOM removal; the `LimitCostOfPathsService` → `LimitCostOfPathsHandler` rename is picked up by `RegisterQueriesFromAssembly`).
- **`Directory.Build.props` edits:** none.
- **Persisted contracts:**
  - Step 05: `CitySearched` moves from `DreamTravel.Domain.Events` to `DreamTravel.Queries.Events`. DreamTravel registers `AddSolPersistentEvents()` (`DreamTravel.Api/Program.cs:125`, `DreamTravel.Worker/Program.cs:56`), so the event is stored as a Hangfire job argument with its type name (design choice H1 in the brief).
- **Applicable checklists** (`.github/skills/premortem/references/`): `di.md`, `cqrs.md`, `build-and-nuget.md`, `hangfire.md` (added by core step 00, present at `c0eb9482`), plus `api.md`, `logging.md`, and `testing-kits.md` because steps 03 and 07 change filter-visible controller results, Chronicle-visible request paths, and run the testing kits on Linux CI.
- **Steps under review:**
  - [01](01-community-health-files-and-root-readme.md),
    [02](02-workflow-supply-chain-hardening.md),
    [03](03-dreamtravel-mediator-controllers.md),
    [04](04-coding-guide-self-consistency.md),
    [05](05-dreamtravel-logic-and-domain-cleanup.md),
    [06](06-dreamtravel-run-instructions.md),
    [07](07-dreamtravel-tests-in-github-ci.md).
  - Close: [08](08-retrospective.md).

## Acceptance criteria
- [x] Premortem output (frame, imagined failure, scenario table, top 3 risks, required mitigations naming their step files, accepted risks, decision) recorded in this file.
- [x] `summary.md` `premortem:` updated to the verdict in the same change.

## Open questions
- none

## Premortem

Run on 2026-11-10 in a session that did not author the plan, against `master@c0eb9482` after the
drift resync (`fa2e55b5`). Source was read for every file the steps touch: the three workflows,
`runTests.ps1`, `README.md`, `dontreadme.md`, the DreamTravel controllers, `Program.cs` (API and
Worker), `DreamTravel.Queries`, `DreamTravel.Domain`, the Worker event handlers, the UI services,
the Aspire host, the component tests, `ResultConversionFilter`, `HangfireEventPublisher`, the
Guide, `CLAUDE.md`, and the `command-query-event-tale` skill. Stories were written before the
checklists. The sweep used `di.md`, `cqrs.md`, `build-and-nuget.md`, `hangfire.md`, `api.md`,
`logging.md`, and `testing-kits.md`; rows marked "sweep" came from a checklist. The owner
pre-approved the plan and asked for recorded decisions instead of confirmation stops; decisions
taken here are marked "decision".

### Premortem — `SolTechnology.Core — flagship showcase hygiene (steps 01–07)`

#### Frame

- **Modules touched**: no `src/SolTechnology.Core.*` code. Root community files and `README.md`
  (01); the three workflows, `.github/dependabot.yml`, and `docs/CICD.md` (02, 07); DreamTravel
  API controllers, `Program.cs`, `DreamTravel.Queries` (`ModuleInstaller.cs`, `.csproj`, two new
  queries, the renamed handler), `DreamTravel.Domain`, `DreamTravel.Worker`, tests, README,
  `initWorkspace.bat`, `docker-compose.yml` (03, 05, 06); `docs/ClaudeCodingGuide.md`,
  `CLAUDE.md`, and the `command-query-event-tale` skill (04, 05).
- **API delta**: none in the packages. DreamTravel HTTP contract: v1 error bodies change from a
  JSON string with the exception message (400) to ProblemDetails; v1 LimitCost reads its paths
  from the body (decision, SRM-6); `/api/projects/*` route through `IMediator`. Persisted
  contract: the `CitySearched` Hangfire argument type name changes (step 05, H1). CI contract:
  `publish` needs `dreamtravel-tests`.
- **Semver impact**: none for the packages (no `src/` change, no `PackageReference` change in
  `src/`). Dependabot (step 02) can change published dependency floors later; SRM-2 governs that.
- **Consumers in workspace**: DreamTravel only, through `ProjectReference`s. The DreamTravel
  Blazor UI (`DreamTravel.Ui/Services/TspService.cs`, `GraphService.cs`) is an HTTP consumer of the
  API. External: visitors who follow the root and DreamTravel READMEs, contributors who follow the
  Guide and skills, and every NuGet consumer if a Dependabot bump raises a dependency floor.

#### Imagined Failure

A month after the org transfer, the flagship looks finished and does not work. A visitor follows
the new "Run in 2 minutes" section: the Aspire API never becomes ready because the dacpac was never
built; after building it, the city search returns 500 because Google points at an unserved
`localhost:2137`, and the UI that Aspire also started fails every call. A consumer's build breaks
with NU1605 after a 2.0.1 that only contained merged Dependabot PRs. A security researcher, finding
no "Report a vulnerability" button although `SECURITY.md` promises one, opens a public issue. The
next contributor, following the `command-query-event-tale` skill, puts a new event back into
`DreamTravel.Domain/Events/` and re-adds the CQRS reference that step 05 removed.

#### Scenarios

| # | Story (what + mechanism) | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation → step |
|---|---|---|---|---|---|---|---|
| 1 | **Security channel promised before it exists.** `SECURITY.md` names Private Vulnerability Reporting, which release 03 enables only after the transfer; until then a reporter finds no private channel and files a public issue. | step 01 `SECURITY.md`; release 03 "Private Vulnerability Reporting is enabled" | public | M | M | none — gap | SRM-1 → 01 |
| 2 | **Dependabot raises published floors.** The `nuget` updater bumps versions in the "Library and testing-kit dependencies" group, whose versions are the published minimums. A green PR merged by the single maintainer raises a floor (or takes a major) in the next patch; a consumer pinning the lower version gets NU1605, and `canonical-versions.md`, which must change in the same PR, drifts. | `Directory.Packages.props:9` (label "minimum versions in the published packages"); `.github/skills/package-management/references/canonical-versions.md:6-8`; step 02 `dependabot.yml` | public NuGet | H | M | `CLAUDE.md §6`, `package-management` skill (both assume a human author) | SRM-2 → 01, 02 |
| 3 | **Split action bumps break the release only.** Separate Dependabot PRs move `upload-artifact` and `download-artifact` to different majors. `download-artifact` runs only in `publish` (tags), so PR and `master` runs stay green and the break surfaces at the 2.0.0 tag. | `publishPackages.yml:87,102`, `:97` (`if: startsWith(github.ref, 'refs/tags/v')`) | release | M | L | none — gap | SRM-3 → 02 |
| 4 | **Step 02 verification passes falsely or has side effects.** The Polish-comment check uses `grep -rnP`; BSD grep on the owner's macOS rejects `-P`, prints nothing on stdout, and the criterion "prints nothing" passes. A `workflow_dispatch` test of the unlist workflow with `UNLIST` typed unlists the ghost packages; a SHA copied from an annotated tag object fails to resolve. | step 02 acceptance; `unlistDeprecatedPackages.yml:18`; `update-nuget-stats.yml:60` | repository | L | H | the workflow test runs (resolve errors only) | SRM-4 → 02 |
| 5 | **v1 best-path body changes shape.** Step 03 makes `CalculateBestPathV1` return the `Result`; `ResultConversionFilter` unwraps it to `CalculateBestPathResult`, while v1 returned `result.Data.BestPaths` (`List<Path>`). Old v1 clients deserialize an object instead of an array; no test covers v1. | `Controllers/Trips/v2/CalculateBestPathController.cs:35`; `src/SolTechnology.Core.Api/Filters/ResultConversionFilter.cs:65-91` | DreamTravel clients | M | M | Swagger snapshot (schema only) | SRM-5 → 03 |
| 6 | **LimitCost turns from 400 into 500 with LogCritical.** `LimitCostOfPathsController` reads `HttpContext.Session`, but no `AddSession`/`UseSession` exists, so every call throws today and the `catch` returns 400. With the `try/catch` removed, `ExceptionFilter` logs Critical and rethrows: every Swagger click on the deprecated endpoint is a 500 and a Critical log. | `Controllers/Trips/v1/LimitCostOfPathsController.cs:43`; `DreamTravel.Api/Program.cs` (no session) | DreamTravel | M | M | none — gap | SRM-6 → 03 |
| 7 | **Thin controllers still drop the request token** (sweep, `cqrs.md`). The four v2 controllers that already use `IMediator` call `Send(query)` without a token; the acceptance grep looks only for `CancellationToken.None`, so they pass while ignoring client aborts. | `Trips/v2/FindCityByNameController.cs:28`, `FindCityByCoordinatesController.cs:28`, `StatisticsController.cs:22`, `RoadPlanner/TrafficController.cs:19` | DreamTravel | L | H | none — gap | SRM-7 → 03 |
| 8 | **Kept jokes and report-only log lines are "cleaned".** Rewrites in 03–05 touch the `"dupa"` CORS name, `"DUPA."` in `LogEventInfoJob`, the "Fire!" log lines, and concatenated v1 log templates (Guide §15 "report only"); an implementer professionalizes them. | `DreamTravel.Api/Program.cs:33`; `Worker/EventHandlers/OnCitySearched/LogEventInfoJob.cs:12`; v1 controllers `:40-42` | owner rule (Guide §9.10, §15) | M | M | Guide §9.10 text only | SRM-8 → 03, 04, 05 |
| 9 | **The template documents the envelope the filter removes.** Guide §7's template declares `ProducesResponseType(typeof(Result<CalculateBestPathResult>), 200)` and `ProjectsController` returns `ActionResult<Result<…>>`; Swagger advertises `{isSuccess, data}` while the wire carries `T`. Clients built from the docs (the UI is one) expect envelopes. | `docs/ClaudeCodingGuide.md:334`; `Controllers/RoadPlanner/ProjectsController.cs:17,25` | contributors, DreamTravel clients | M | H | none — gap | SRM-9 → 03, 04 |
| 10 | **The UI the README launches cannot talk to the API** (pre-existing, exposed by step 06). `TspService`/`GraphService` read `Result<T>` envelopes (`Result.OnDeserialized` throws on a bare body), call `/api/v2/...` while the API versions by the `X-API-VERSION` header, use `ApiBaseUrl` `https://localhost:2021`, and ship `<SECRET>` as the Google Maps key. | `DreamTravel.Ui/Services/TspService.cs:11-13,65`; `GraphService.cs:19`; `wwwroot/appsettings.json:2-8`; `src/SolTechnology.Core/Result.cs:38-45` | visitors | M | H | none — gap (`TspServiceTests` cover formatting only) | SRM-10 → 06, 08 |
| 11 | **The clean clone never starts.** `dotnet run --project src/DreamTravel.Aspire` does not build `DreamTravelDatabase` (no project references it), so the dacpac hook throws `FileNotFoundException` and the API waits forever on `dbDeployment`. On Apple silicon the amd64 SQL Server image needs emulation. | `DreamTravel.Aspire/Program.cs:91-102,117`; `.gitignore:62` (`artifacts/`) | visitors | M | H | step 06 manual check (only if run from a truly clean clone) | SRM-11 → 06 |
| 12 | **Google-backed endpoints fail locally.** `HTTPClients:Google:BaseAddress` is `http://localhost:2137/google/`, which only the component tests' WireMock serves; city search and best path return 5xx ProblemDetails on a clean clone. | `DreamTravel.Api/appsettings.json:61-65`; `Worker/appsettings.json:45` | visitors | M | H | none — gap | SRM-12 → 06 |
| 13 | **The README rewrite drops the Chronicle section.** Step 06 rewrites the architecture section; the "Chronicle step reports" sample (refreshed in `3b864398`) is lost or edited out of sync with `FindCityChronicleTests`. | `sample-tale-code-apps/DreamTravel/README.md:179-205` | visitors | L | M | none | SRM-13 → 06 |
| 14 | **Old `CitySearched` jobs cannot load** (sweep, `hangfire.md`). After step 05 the API enqueues `DreamTravel.Queries.Events.CitySearched, DreamTravel.Queries`; jobs stored before the pull carry the Domain type name, fail with a job-load error, and go straight to Failed (`[AutomaticRetry(Attempts = 0)]`). The Aspire SQL container is persistent, so local Hangfire tables survive between runs. | `HangfireEventPublisher.cs:32-38`; `DreamTravel.Sql/ModuleInstaller.cs:21-22`; `DreamTravel.Aspire/Program.cs:13` | local DreamTravel databases | L | L | `FindCityAndSaveDetailsTest` (new type end to end on a fresh database) | SRM-14 → 05 (accepted) |
| 15 | **Guidance still sends events to the Domain.** Guide §0 (item 4), §1 (tree comment), §6, and the `command-query-event-tale` skill (`:16`, `:57`, `:166`, `:235`) say `IEvent` records live in `*.Domain/Events/`; after step 05 the example path is dead, and the next contributor re-adds the CQRS reference. | `docs/ClaudeCodingGuide.md:23,53,300`; `.github/skills/command-query-event-tale/SKILL.md:16,57,166,235` | contributors | M | H | none — gap | SRM-15 → 05 |
| 16 | **"Not mutated" holds for the list only.** `Path` is a mutable class; a handler working on `query.Paths.ToList()` still writes `OptimalCost`/`OptimalDistance` into the caller's objects, and a test that checks only order passes. | `LimitCostOfPathsService.cs:10-55`; `DreamTravel.Domain/Paths/Path.cs:5-16` | DreamTravel | L | M | none | SRM-16 → 05 |
| 17 | **The first Linux run blocks the release.** DreamTravel tests have never run on GitHub's Ubuntu runners; a platform failure, an image pull failure, or a flaky component test turns `dreamtravel-tests` red, and with `needs` the tag publish is skipped. The tempting fix (`continue-on-error`, or an `if:` that skips the job, which also skips `publish`) removes the protection. | step 07; `publishPackages.yml:95-97`; `tests/Component/ComponentTestsFixture.cs:30-45` | release | M | M | step 07 "runs green on a PR" | SRM-17 → 07 |
| 18 | **The quick-start check passes only inside the repository.** A throwaway project under the repository tree inherits the root `Directory.Build.props` and Central Package Management, so it builds with defaults a visitor does not have (or fails with NU1008 for the wrong reason). | `Directory.Build.props`, `Directory.Packages.props:6`; step 01 "quick-start snippet builds" | visitors | L | M | none | SRM-18 → 01 |
| 19 | **A v1 log detail never matches** (sweep, `logging.md`). `LogDetail(... endpoints: ["/api/v1/FindLocationOfCity", ...])` uses a URL segment that the header-versioned route `api/FindLocationOfCity` never has, so v1 requests log no `CityName` once they run through the pipeline. | `DreamTravel.Api/Program.cs:117-121`; `Trips/v1/FindLocationOfCityController.cs:17` | DreamTravel logs | L | H | none | SRM-19 → 03 |
| 20 | **Validation now runs for v1/v2 best path** (sweep, `cqrs.md`). Routing through `IMediator` applies `CalculateBestPathQuery`'s validator, which direct handler calls skipped; previously accepted payloads may get 400. | `CalculateBestPathQuery.cs` (validator); `CalculateBestPathController.cs:34,55` | DreamTravel clients | L | L | `CalculateBestPathFeatureTest`, contract snapshot | none (intended, defended) |
| 21 | **Renamed or new handlers not resolved** (sweep, `di.md`). `LimitCostOfPathsHandler`, `GetIntersections`, and `GetStreets` are picked up by `RegisterQueriesFromAssembly(typeof(ModuleInstaller).Assembly)`; repositories come from `InstallGraphDatabase`. | `DreamTravel.Queries/ModuleInstaller.cs:13`; `DreamTravel.Api/Program.cs:107-109` | DreamTravel | L | L | component tests; CQRS missing-handler error | none (defended) |

#### Top 3 Risks

1. #2 — the only `H`: an unattended dependency bump reaches every NuGet consumer as a raised floor.
2. #10, #11, #12 — the feature's headline promise ("runs in two minutes") fails on the first
   visitor's machine in three independent ways.
3. #15 — the feature fixes "Domain references nothing" in code but leaves the Guide and the skill
   teaching the opposite, so the defect returns with the next event.

#### Required Mitigations Before Merge

- **SRM-1** → 01: the owner enables Private Vulnerability Reporting on the current repository
  before `SECURITY.md` lands (admin setting; it survives the transfer). Acceptance:
  owner-confirmed and dated. Release 03 then only re-checks it.
- **SRM-2** → 02, 01: `dependabot.yml` ignores `version-update:semver-major` for `nuget`
  (majors go through the `package-management` skill by hand) and labels nuget PRs
  `dependencies`. Library-floor packages (the "Library and testing-kit dependencies" item group)
  are not grouped with test-only or DreamTravel packages, so each floor raise is a visible PR;
  `Aspire*` packages form one group because `DreamTravel.Aspire.csproj:2` pins the SDK version by
  hand. `CONTRIBUTING.md` and the PR template (step 01) carry a "dependency floor" item: a PR that
  changes a library-floor version updates `canonical-versions.md` and adds a `CHANGELOG.md`
  Unreleased line in the same PR (push to the Dependabot branch before merging). Early warning: a
  Dependabot PR touching `Directory.Packages.props:9-58`.
- **SRM-3** → 02: one `github-actions` group (`patterns: ["*"]`), so artifact actions move
  together; `upload-artifact` and `download-artifact` stay on the same major.
- **SRM-4** → 02: the Polish check uses `grep -rnE` (or `rg`); the unlist test run uses an
  empty `confirm` and must show the job skipped (never type `UNLIST`); SHAs are resolved with
  `git ls-remote https://github.com/<owner>/<action> 'refs/tags/vX.Y.Z^{}'` (the dereferenced
  commit, not the tag object).
- **SRM-5** → 03: v1 keeps its `List<Path>` body — the controller projects a successful
  `Result` to `BestPaths` before returning it. A component test with `X-API-VERSION: 1.0` asserts a
  JSON array on success and ProblemDetails on failure.
- **SRM-6** → 03 (decision): `LimitCostOfPathsController` takes `[FromBody] LimitCostOfPathsQuery`
  and drops the Session read and the double JSON encoding. It never worked without session
  middleware, so no working client breaks. Component test: success and validation failure.
- **SRM-7** → 03: every controller action passes `HttpContext.RequestAborted` to `Send`;
  acceptance: `grep -rn "mediator.Send(" .../Controllers | grep -v RequestAborted` prints nothing.
- **SRM-8** → 03, 04, 05: kept verbatim and checked by grep: `CorsPolicy = "dupa"`
  (`Program.cs`), `DUPA.` (`LogEventInfoJob.cs`), `//I'm just void` (`src/SolTechnology.Core.CQRS/Nothing.cs`),
  the "Fire!" log lines of actions that keep logging. The v1 concatenated log templates stay as
  they are (Guide §15 "report only") and are listed in step 03's Deviations.
- **SRM-9** → 04, 03: Guide §7 template declares `typeof(CalculateBestPathResult)` for 200 and
  `ProblemDetails` for failures; step 03 controllers declare the unwrapped type (including
  `ProjectsController`), and the Swagger snapshot shows no `Result` envelope for these actions.
- **SRM-10** → 06, 08 (decision): the README does not present the UI as working; the two-minute
  path is the Aspire dashboard plus Swagger. The UI is described as needing a Google Maps key.
  Step 08 records the follow-up "align `DreamTravel.Ui` with the API wire format (unwrapped bodies,
  ProblemDetails, header versioning, `X-API-KEY`, base URL from Aspire)". Fixing the UI here would
  need a paid key to verify and grows the feature beyond its scope.
- **SRM-11** → 06: the README builds before running (`dotnet build` of the DreamTravel projects,
  which produces `artifacts/dacpac/`), lists Docker Desktop with amd64 emulation on Apple silicon
  and free ports 1404, 7474, and 7687, and mentions the dashboard login link printed to the
  console. The manual check starts from a fresh `git clone` into a new folder and records date
  and platform.
- **SRM-12** → 06: the two-minute smoke uses Google-free calls (`GET /healthz`;
  `GET /api/statistics/countries` with `X-API-KEY: <SECRET>` and `X-API-VERSION: 2.0`). An
  optional section shows how to supply an own Google key through environment variables
  (`HTTPClients__Google__BaseAddress`, `HTTPClients__Google__Options__Key`), never committed.
- **SRM-13** → 06: `### Chronicle step reports` stays unchanged; acceptance:
  `git diff <base> -- sample-tale-code-apps/DreamTravel/README.md` shows no line removed inside it.
- **SRM-15** → 05: the same PR edits Guide §0 item 4, §1, and §6, and the skill (`:16`, `:57`,
  `:166`, `:235`): records published through CQRS (`IEvent`) live in an `Events/` folder of the
  publishing LogicLayer project; the Domain keeps types that reference nothing. Acceptance:
  `grep -n "Domain/Events" .github/skills/command-query-event-tale/SKILL.md` prints nothing.
- **SRM-16** → 05: the handler copies each `Path` (new instances); the unit test asserts that the
  input list's order and every element's `OptimalCost`/`OptimalDistance` are unchanged.
- **SRM-17** → 07: the job has `timeout-minutes`, no `continue-on-error`, and no `if:`; a
  Linux-only failure is fixed in tests or fixtures, never in production code or by weakening the
  job; `docs/CICD.md` tells the release operator to re-run a failed `dreamtravel-tests` job before
  re-tagging. Early warning: the job's status on every `master` push.
- **SRM-18** → 01: the throwaway quick-start project lives outside the repository tree and uses
  a local `dotnet pack` feed; it is never committed.
- **SRM-19** → 03: the `LogDetail` endpoint becomes `/api/FindLocationOfCity`.

#### Accepted Risks

- #14 (**SRM-14**, decision under H1): no type-name binder for `CitySearched`. The blast radius is
  local developer databases; the cost is a few Failed jobs that can be deleted from the Hangfire
  dashboard. Step 05 records the break in its Deviations and the brief.
- #10 residual: until the SRM-10 follow-up lands, the UI that Aspire starts does not work; the
  README says so instead of hiding it.
- SRM-2 residual: patch-level floor raises are still merged by the owner's judgment; the CHANGELOG
  line makes them visible to consumers.
- #17 residual: a flaky run can delay a tag publish until the job is re-run.
- AR-4 stands unchanged (Azure DevOps pipelines kept without CI coverage, Q9).

#### Decision

**Go with mitigations.** The only `H` scenario (#2) has a concrete mitigation in steps 01 and 02;
the semver classification (none for the packages) matches the plan; every `M` scenario has a
mitigation folded into its step file. The decisions the owner would otherwise be asked (SRM-6,
SRM-10, SRM-14, the step 06 compose deletion, the `€` symbol in step 05) are recorded under the
owner's pre-approval. Owner-only manual actions (admin settings) are listed in the brief under
"Premortem (2026-11-10)"; none blocks the start of step 01 except SRM-1 before `SECURITY.md`
merges.

## Deviations

### Plan resynced before the premortem

**Original plan:** run the premortem against the plan as reviewed on 2026-11-02.

**Actual implementation:** the 13 library features, Chronicle, Redis fail-fast, and foundation
steps 01–05 and 07–10 shipped between planning and this gate. The step files were first compared
with `master@c0eb9482` and corrected in a separate commit (`fa2e55b5`; table in the brief under
"Resync with the shipped 2.0 libraries"). Two items were already delivered (the Guide auth line by
api-auth 01, the `CLAUDE.md §11` rows by auid 03 and tale); no step was removed. The premortem
then ran against the resynced plan, and the checklist set grew by `api.md`, `logging.md`, and
`testing-kits.md` (Frame above).
