---
status: completed
created: 2026-11-02
completed: 2026-11-09
---

# SolTechnology.Core 2.0 — Testing kits

> Historical delivery record. It may not describe the current system.

## Goal

Deliver the 2.0 hardening of `.Testing`, `.HTTP.Testing`, `.API.Testing`, `.SQL.Testing`, `.Redis.Testing`, `.Blob.Testing`, `.ServiceBus.Testing` end-to-end, as one library of the SolTechnology.Core 2.0
flagship release. Success means:

- The API testing kit disposes its test host, and `WireMockStartup` is strict by default (P-11).
- Container fixtures keep per-instance state and honour the Ryuk settings.
- The 2.0 surface: fixture methods drop the `Async` suffix, `ServiceBusFixture.Initialize` accepts a `CancellationToken`, `WireMockStartup.WireMockServer` is a get-only property, and the unused `FakeApiBase.BaseUrl` is gone (`C-23`, `C-24`).
- Every testing kit ships XML docs and a module doc that compiles against 2.0, and conforms to the Coding Guide.

## Context

**Depends on:**

- Core 02, 03, and 04 of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) are `done` (CI fails on any test failure, tag-only publishing, version 2.0.0 on `master`).
- [`2026-11-02-core-2-0-http`](2026-11-02-core-2-0-http.md) is `completed`.
- [`2026-11-02-core-2-0-api-auth`](2026-11-02-core-2-0-api-auth.md) is `completed`.
- [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md) is `completed`.
- [`2026-11-02-core-2-0-sql`](2026-11-02-core-2-0-sql.md) is `completed`.
- [`2026-11-02-core-2-0-blob`](2026-11-02-core-2-0-blob.md) is `completed`.
- [`2026-11-02-core-2-0-messagebus`](2026-11-02-core-2-0-messagebus.md) is `completed`.

Delivery order position 13 of 13 in the umbrella brief's "Per-library regroup" section. The
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) feature
depends on this feature being `completed`.

### Origin

Regrouped on 2026-11-02 from [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (user: "podziel implementację feature 1 na
każdą bibliotekę osobno"). Each step names its former core step in a "Former core step" line; the
umbrella brief's "Per-library regroup" table maps every former number. The evidence is the
full-repository code review of 2026-11-02 against `master@4097d47f`; finding IDs follow the
umbrella brief's "Evidence" table. Findings acted on here: `P1-1`, `P1-4`, `cqrs-tale#16`, `http-api#10`, `http-api#21`, `http-api#24`, `infra#15`, `infra#16`, `infra#18-19`, `infra#24`, `logging-cache#26`.

### Decisions owned by this feature

The umbrella brief keeps the shared decision tables and the user's confirmations (`C-01`…`C-24`,
`C-26`, `C-27` confirmed, `C-25` rejected, 2026-11-02). The rows below are copied from it. Step
numbers in copied `A`, `AR`, `Q`, and `RQ` rows are former core numbers.

Public API changes (`CLAUDE.md §2`, confirmed by the user on 2026-11-02):

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-23 | Testing kits (optional) | Drop `Async`: `AzuriteFixture.InitializeAsync/ClearAsync/CreateBlobContainerAsync`, `RedisFixture.InitializeAsync/FlushAsync`, `SQLFixture.InitializeAsync/ResetAsync`, `ServiceBusFixture.InitializeAsync` (also gains `CancellationToken ct = default`), `ContainerLifecycleHelper.EnsureRunningAsync/WaitForAmqpReadyAsync/WaitForHealthyAsync` | testing-kits 03 |
| C-24 | HTTP.Testing (optional) | `WireMockStartup.WireMockServer` public field → get-only property; DELETE unused abstract `FakeApiBase.BaseUrl` | testing-kits 03 |
| C-33 | HTTP.Testing (additive, code review 2026-11-09) | NEW `WireMockFixture.Initialize(WireMockServerSettings settings)`; the port overload delegates to it | testing-kits code review |

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-11 | `WireMockStartup` `AllowPartialMapping` defaults to `false` | Loose tests start failing (intended) | testing-kits 01 |
| P-28 | The kits no longer disable Ryuk; it follows the Testcontainers settings (on by default) | Disposed containers are removed, not only stopped; ECI users set `TESTCONTAINERS_RYUK_DISABLED=true` | testing-kits 02 (added during implementation) |
| P-29 | `ServiceBusFixture` state per instance; the fixture owns and removes the MSSQL sidecar and network | Same-name fixtures no longer share an emulator; no sidecar left after dispose | testing-kits 02 (added during implementation) |

User decisions (2026-11-02):

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| Q6 | New test project `tests/SolTechnology.Core.Blob.Tests` (and `*.Testing` tests)? | **No new test projects.** No existing test project references Blob, so steps 16 and 50 validate with build and source checks; accepted risk AR-1. | 11, 16, 50 |

Accepted risks:

| ID | Risk | Source | Mitigation in this plan |
|---|---|---|---|
| AR-1 | `B6`, `infra#6`, and `infra#7` fixes in Blob ship without automated tests, and Blob has no DI resolution test. | Q6 | Steps 16 and 50 gate on a Release build and source greps; a manual Azurite check is recommended in the PR. Follow-up: a Blob test project. |

Premortem mitigations carried verbatim from core 00: none.

No test project exists for the `*.Testing` packages, and none is created (Q6); the kits are exercised by the library test projects that use them (SQL.Tests, Cache.Tests, MessageBus.Tests, API.Tests, HTTP.Tests, CQRS.Tests, Hangfire.Tests) and by the DreamTravel component tests. The kits have no L-03 template row; step 06 runs the library-scoped `[{Placeholder}]` check. Delivered last: step 03 renames fixture methods that the SQL, Cache, and MessageBus features start using.

Azurite image bumped by blob code review 2026-11-06: `AzuriteFixture`'s default image is
`mcr.microsoft.com/azure-storage/azurite:3.37.0`, the first release that accepts the service version `2026-06-06` of
`Azure.Storage.Blobs` 12.29.0 (`3.35.0` answered 400 `InvalidHeaderValue`). Steps of this feature keep that pin or move
it forward, never back; see [`2026-11-02-core-2-0-blob`](2026-11-02-core-2-0-blob.md).

## Scope

### In scope

- Source: `src/SolTechnology.Core.Testing`, `src/SolTechnology.Core.HTTP.Testing`, `src/SolTechnology.Core.API.Testing`, `src/SolTechnology.Core.SQL.Testing`, `src/SolTechnology.Core.Redis.Testing`, `src/SolTechnology.Core.Blob.Testing`, `src/SolTechnology.Core.ServiceBus.Testing`, and the matching test projects.
- Module docs: `docs/Testing.md`, `docs/HTTP.Testing.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md`, `docs/Redis.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md`.
- Phases, in order: correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- A test project for the `*.Testing` packages (Q6).
- WireMock 2.13 → 2.21 (outdated-package sweep).
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps lived in the temporary working folder `2026-11-02-core-2-0-testing-kits`, which the retrospective deleted on
2026-11-09. Gates: `review` was `done (2026-11-02)`; `premortem` was `waived` — the core premortem verdict
`go-with-mitigations (2026-11-02)` covered these steps.

Plan review (2026-11-02): the regrouping was checked against the pre-regroup plan (`dddac21c`) and
found complete. User answers recorded, not re-asked: the delivery order with AUID before Tale is
confirmed (Tale references AUID); the per-library split, the premortem reuse, and the `C-01`…`C-24`,
`C-26`, `C-27` confirmations stand (`C-25` rejected). Review fixes applied here: step 06 regains the former step 65 DreamTravel component-test criterion; the brief's test list adds CQRS.Tests, Hangfire.Tests (both reference `SolTechnology.Core.Testing`), and the DreamTravel component tests, with the tracked `API` casing; the retrospective's DreamTravel test command is runnable.

Notation: an unqualified step number is a step of this feature; `core NN` names step `NN` of
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); other short names (`result`, `logging`, `cqrs`, `auid`, `tale`, `http`, `api-auth`,
`cache`, `sql`, `blob`, `messagebus`, `hangfire`, `testing-kits`) name the library features listed
in the umbrella brief; `release NN` names a step of
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

| Step | Title | Former core step(s) |
|---|---|---|
| 01 | API and HTTP testing kits — dispose the test host; strict WireMock by default | 36 |
| 02 | Container fixtures — per-instance state; honour Ryuk settings | 37 |
| 03 | Testing kits — 2.0 surface (optional `Async` renames, WireMock fields) | 53 |
| 04 | XML docs shipped — Testing, HTTP.Testing, API.Testing, SQL.Testing, Redis.Testing, Blob.Testing, ServiceBus.Testing | 56, 58 |
| 05 | Module docs — testing kits | 59, 60, 61 |
| 06 | Guide conformance — testing kits | 65, 68, 69, 70 (library-scoped check) |
| 07 | Retrospective | 71 |

Steps run in number order; each is one PR.

### Follow-ups inherited from closed features (2026-11-09)

The closed library briefs name this feature as the owner of these follow-ups. The owner pre-approved the plan; the
implementer folded each into the step that fits it or kept it deferred, without growing the scope.

| Follow-up | Source | Where |
|---|---|---|
| `ServiceBusFixture` leaves the emulator's MSSQL sidecar (and its network) behind when Ryuk is off | [`messagebus`](2026-11-02-core-2-0-messagebus.md) Follow-ups | Step 02, with the Ryuk change |
| Keep `AzuriteFixture`'s default image at `azurite:3.37.0` | [`blob`](2026-11-02-core-2-0-blob.md) code review | Step 03 renames the fixture's methods and keeps the pin; no step changes the image |
| Split `tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` (222 lines, Guide §9) | `messagebus` Follow-ups | Step 06, with the other size-cap splits |
| A Blob test project starting with five Azurite tests | `blob` Follow-ups, AR-1 | Deferred: Q6 rules out new test projects; it stays a Blob follow-up |
| A shipped Hangfire testing helper (`RecurringJobHost`, the performer loop, `HangfireGlobalState.Reset`) | [`hangfire`](2026-11-02-core-2-0-hangfire.md) Follow-ups | Deferred: no step plans a Hangfire kit; recorded under `Follow-ups` with the reset requirement |
| Dedicated tests for the companion packages | [`2026-05-30-testing-framework-companions`](2026-05-30-testing-framework-companions.md) | Deferred with the `*.Testing` test project (Q6) |
| Remove the `System.Security.Cryptography.Pkcs` override and the `Microsoft.Identity.Client` reference from `SQL.Testing` | [`sql`](2026-11-02-core-2-0-sql.md) Follow-ups | Deferred: both wait on upstream packages |

## Acceptance criteria

- [x] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations` (step 02's ubuntu CI run waits for the push; see `Follow-ups`).
- [x] `C-23` and `C-24` shipped; every test project that uses a fixture compiles against the renamed members.
- [x] A WireMock request without a matching mapping fails the test by default (P-11).
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.HTTP.Tests` and `dotnet test tests/SolTechnology.Core.SQL.Tests` and `dotnet test tests/SolTechnology.Core.Cache.Tests` and `dotnet test tests/SolTechnology.Core.MessageBus.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` and `dotnet test tests/SolTechnology.Core.Hangfire.Tests` (the last two reference `SolTechnology.Core.Testing`) are green.
- [x] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green (it uses the fixtures and the HTTP.Testing faker).
- [x] `find src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [x] Every C# snippet in `docs/Testing.md`, `docs/HTTP.Testing.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md`, `docs/Redis.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [x] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

Delivered on `master` from `05d1ceb4` (step 01) to `032ed22a` (step 06), plus the code review fixes `9eba42cf` to
`4fb71f8e`; this retrospective ran on 2026-11-09. The owner pre-approved the plan and asked on 2026-11-09 to run the
retrospective and close the feature (`CLAUDE.md §2`).

- **`http-api#10`, `http-api#21`, P-11, `C-33` (step 01, code review 1):** `APIFixture` disposes its
  `WebApplicationFactory`, so the in-memory host and its hosted services stop with the fixture. WireMock matches
  strictly by default; `WireMockFixture.Initialize(settings)` (new, `C-33`) and `WireMockStartup.Run(settings)` opt back
  into partial matching. DreamTravel's distance-matrix stubs, which only ever matched partially, match the comma-split
  values.
- **`infra#15`, `infra#16`, P-28, P-29, messagebus follow-up (step 02, code review 3, 4):** `ServiceBusFixture` keeps its
  state per instance and owns the emulator's MSSQL sidecar and network (`OwnedEmulator`), removing all three on dispose,
  each even when another fails; a failed start keeps its own exception. The kits no longer switch Ryuk off, so it is on
  by default and disposed containers are removed; ECI users set `TESTCONTAINERS_RYUK_DISABLED=true`.
  `TestContainersContext` deletes its network explicitly.
- **`C-23`, `C-24` (step 03):** fixture methods drop `Async`, `ServiceBusFixture.Initialize` takes a token,
  `WireMockStartup.WireMockServer` is a get-only property, and `FakeApiBase.BaseUrl` is gone. Every test project and the
  DreamTravel component tests use the new names, and so do the package descriptions (code review 2).
- **`P1-4`, `cqrs-tale#16` (step 04):** all seven kits ship XML docs in their packages.
- **`P1-1`, `infra#18-19` (step 05):** the seven module pages describe the 2.0 kits and every C# block compiles against
  them; six other pages that called renamed members are updated too.
- **`http-api#24`, `infra#24`, `logging-cache#26` (step 06):** no kit file is over 150 lines, HTTP.Testing file names
  match their types, primary constructors and `extension` blocks are in place, and the MessageBus
  `ModuleInstallerTests` is split (messagebus follow-up). The kits have no log templates, so the `[{Placeholder}]` check
  found nothing and L-03 gains no entry.
- **Rows and release notes:** `C-23`, `C-24` shipped as written; `C-33` is new (additive); P-11's consumer impact names
  the opt-outs; `P-28` and `P-29` are new. The core brief's "Release-notes inputs" has a testing-kits 01 entry (strict
  matching, its opt-outs, the comma-split hint, `APIFixture` disposal) and a testing-kits 02 entry (Ryuk, the Service
  Bus sidecar). No `L-nn` row changed.
- **Architecture:** [`architecture/testing.md`](../architecture/testing.md) describes the 2.0 kits: per-instance
  state, Ryuk on by default and what stays without it, the owned Service Bus sidecar, strict WireMock matching and
  `APIFixture` disposal, and that the kits have no test project (Q6). The premortem testing-kits checklist gains four
  lessons.

Final validation: `dotnet build SolTechnology.Core.slnx -c Release` is green with no NU190x/NU1605 and no warning in a
file of this feature (the remaining warnings are pre-existing, in the DreamTravel sample, Tale.Tests and Logging.Tests).
`dotnet test SolTechnology.Core.slnx -c Release` passes 928 with 11 skipped (DreamTravel geolocation) and none failed,
917 before this feature; it includes the DreamTravel component tests (31/31) and every DreamTravel unit project except
`DreamTravel.Commands.UnitTests`, which has no tests. `docker ps -a` and `docker network ls` show nothing left by the
runs.

## Deviations

### Strict matching exposed a stub bug; the disposal test boots its own host (step 01)

**Original plan:** keep the HTTP.Tests consumers of `WireMockStartup` green; fix only the DreamTravel stubs that relied
on partial matching; a minimal entry point for the disposal test.

**Actual implementation:** HTTP.Tests had no `WireMockStartup` consumer, so it gained `WireMockStartupTests` (and, after
the code review, `WireMockFixtureTests`). DreamTravel's distance-matrix stubs never matched strictly, because WireMock
splits query values on commas; they now match the split values. `DisposalTestEntryPoint.Main` is the API.Tests entry
point (`GenerateProgramFile=false`) and sets its content root to the output folder.

### The Service Bus fixture owns its sidecar; Ryuk mattered for every fixture (step 02)

**Original plan:** per-instance state in `ServiceBusFixture`; delete the Ryuk override.

**Actual implementation:** besides per-instance state (only a per-name start lock stays static), the fixture builds and
removes the MSSQL sidecar and network that `Testcontainers.ServiceBus` created but never disposed, absorbing the
messagebus follow-up. With Ryuk off, `WithCleanUp(true)` clears the reaper session id and dispose only stops a container,
so the old override had left every SQL, Redis and Azurite container stopped on the host; this became `P-28`, and the
Service Bus change `P-29`. The reuse helpers moved into `ReusedEmulator`. The ubuntu CI run of the step's third
criterion waits for the push.

### Renames kept to the listed members (step 03)

**Original plan:** the `C-23` and `C-24` members.

**Actual implementation:** exactly those; internal `Async` names in SQL.Testing stay, `Flush` gains no token, and
`WireMockServer` has a private setter for `Run`.

### Forwarding members inherit their docs (step 04)

**Original plan:** a `<summary>` on every member `CS1591` reports.

**Actual implementation:** the 45 forwarding members of `JsonResponseBuilderDecorator` and interface implementations use
`<inheritdoc />`.

### Targeted page edits and more pages (step 05)

**Original plan:** edits to the seven kit pages.

**Actual implementation:** the pages keep their structure; usage snippets were added to `API.Testing.md` and
`SQL.Testing.md`, a "Strict matching" section to `HTTP.Testing.md`, and six other pages that called renamed members
were updated. The `Ct.Any` snippet awaits the NSubstitute check, because without it CS4014 fails a consumer that treats
warnings as errors.

### Splits by partial file and collaborator (step 06)

**Original plan:** split the three oversized files; for `SQLFixture`, extract the engine-specific container setup.

**Actual implementation:** `JsonResponseBuilderDecorator` and `SQLFixture` became partial classes (the SQL split moves
the fluent configuration, because the engine selection is a four-line switch), and `ServiceBusFixture` hands its
containers to `OwnedEmulator`. `ResultAssertions`, `InMemorySinkAssertions` and `AuthClientExtensions` moved into
`extension` blocks; `ShouldBeSuccess<T>` stays classic (Guide §9.7). ApiCompat showed only the extension marker types
and the accepted `CP0021` "notnull".

### Code review finding 3 did not reproduce

The reported network leak of `TestContainersContext` with Ryuk off does not happen on Testcontainers 4.16, because its
network never calls `WithCleanUp` and keeps the default reaper-session label. The decided explicit delete shipped
anyway, with a test that removes the network with Ryuk on and off.

## Follow-ups

- Run `runTests.ps1` on ubuntu CI with Ryuk on after the push (step 02, third criterion).
- A shipped Hangfire testing helper. The Hangfire tests keep their own harness (`RecurringJobHost`, the performer and
  state-machine loop, `HangfireGlobalState.Reset`). A shipped helper must reset Hangfire's process-wide state (the log
  provider, `GlobalJobFilters`, `JobFilterProviders.Providers`) on dispose, otherwise a disposed container leaves
  Hangfire's global logger pointing at a disposed factory.
- A `*.Testing` test project (Q6). Until then the kits are covered through their consumers; the
  `OwnedEmulator` removal-failure path has no automated test.
- A Blob test project with the five Azurite tests from the blob code review (AR-1); it stays a Blob follow-up.
- With `TESTCONTAINERS_RYUK_DISABLED=true`, a disposed SQL, Redis or Azurite fixture only stops its container, because
  `WithCleanUp(true)` without Ryuk clears the reaper session id. Dropping that call (the default label keeps deletion
  on) or adding `WithAutoRemove` would remove them, as `ServiceBusFixture` does.
- Remove the `System.Security.Cryptography.Pkcs` override and the `Microsoft.Identity.Client` reference from
  `SQL.Testing` when the upstream packages allow it (SQL feature follow-ups).
- WireMock 2.13 → 2.21 (outdated-package sweep, out of scope here).

## Code review fixes (2026-11-09)

An independent code review of `8e3908f0..032ed22a` found four issues; each was fixed with the user's decision, in its
own commit. Rows: `C-33` (new), P-11 (consumer impact amended); the P-11 release-notes input in the core brief names
both opt-outs. The retrospective ran after these fixes.

| # | Finding | Fix | Commit | Test |
|---|---|---|---|---|
| 1 | Medium. `WireMockFixture.Initialize(int)` always ran strict (P-11); the documented opt-out existed only on `WireMockStartup.Run(settings)`, so the release note was wrong for the fixture. | New `WireMockFixture.Initialize(WireMockServerSettings)` (`C-33`); the port overload delegates to it with `DefaultSettings(port)`. `HTTP.Testing.md` documents `AllowPartialMapping = true` and `wireMock.Initialize().WireMockServer.AllowPartialMapping()`; the fixture has no `WireMockServer` property, so the one-liner goes through the `WireMockStartup` that `Initialize` returns instead of adding API. Release note and P-11 row fixed. | `9eba42cf` | `WireMockFixtureTests`: a partial match is served with `AllowPartialMapping = true` and gets 404 without it; `AllowPartialMapping()` on the running server; null settings throw. |
| 2 | Low. The Blob.Testing and Redis.Testing package descriptions and `canonical-versions.md:89` named the pre-`C-23` methods. | Descriptions name `CreateBlobContainer` / `Clear` and `Flush()`; the reference names `RedisFixture.Flush()`. A grep of every `src` csproj, `.github` and the module docs finds no other old name. | `b8c229f4` | — |
| 3 | Low. `TestContainersContext.DisposeAsync` was reported to leak its network with Ryuk off, because `DockerNetwork` deletes only with a reaper session id. | `DisposeAsync` deletes the network before disposing it. The leak did not reproduce on Testcontainers 4.16: every resource gets the reaper-session label by default and `NetworkBuilder` never calls `WithCleanUp`, so only `WithCleanUp(true)` with Ryuk off (as the SQL, Redis and Azurite fixtures do) clears the session id. The explicit delete is kept as decided, with a comment that says so; `Testing.md` now lists what stays without Ryuk. | `25c6d012` | `TestContainersContextTests` (in MessageBus.Tests, the Docker test project that references the kit; Q6) removes the network with Ryuk on and off; it also passes against the old code. |
| 4 | Low. `OwnedEmulator.DisposeAsync` stopped at the first failure, and in `ServiceBusFixture.StartEmulator` a clean-up exception replaced the start exception. | Each of the three removals is attempted; failures are thrown together as one `AggregateException`. A failed start rethrows the start exception, attaches the clean-up failure to its `Data["ServiceBusFixture.CleanupFailure"]` and writes it to the console. The fixture clears its emulator reference in a `finally`. | `8b4e3c6f` | No fault-injection test: `OwnedEmulator` builds real containers and has no seam for a failing removal. The lifecycle tests still pass. |

Verification after the fixes: HTTP.Tests 162/162 and MessageBus.Tests 42/42; `dotnet test SolTechnology.Core.slnx
-c Release` passes 928 with 11 skipped and none failed (922 before the fixes, plus 4 `WireMockFixtureTests` and 2
`TestContainersContextTests`). `docker ps -a` and `docker network ls` show no container or network added by the run.

