---
status: in-progress
created: 2026-11-02
completed:
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

Steps live in the temporary working folder `2026-11-02-core-2-0-testing-kits/` (see its `summary.md`). Gates: `review` is
`done (2026-11-02)`; `premortem` is `waived` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

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

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] `C-23` and `C-24` shipped; every test project that uses a fixture compiles against the renamed members.
- [ ] A WireMock request without a matching mapping fails the test by default (P-11).
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.HTTP.Tests` and `dotnet test tests/SolTechnology.Core.SQL.Tests` and `dotnet test tests/SolTechnology.Core.Cache.Tests` and `dotnet test tests/SolTechnology.Core.MessageBus.Tests` and `dotnet test tests/SolTechnology.Core.CQRS.Tests` and `dotnet test tests/SolTechnology.Core.Hangfire.Tests` (the last two reference `SolTechnology.Core.Testing`) are green.
- [ ] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green (it uses the fixtures and the HTTP.Testing faker).
- [ ] `find src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Testing.md`, `docs/HTTP.Testing.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md`, `docs/Redis.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

Decisions taken during implementation (owner pre-approved the plan; detail in each step's `Deviations` until the
retrospective promotes them):

- Step 01: HTTP.Tests had no `WireMockStartup` consumer, so it gains a reference to HTTP.Testing and
  `WireMockStartupTests`, which pins P-11. Strict matching exposed DreamTravel's distance-matrix stubs: WireMock splits
  query values on commas, so they never fully matched; the stubs now match the split values. The disposal test boots
  its own `DisposalTestEntryPoint` (API.Tests sets `GenerateProgramFile=false`).
- Step 02: all `ServiceBusFixture` state is per instance (only a per-name start lock stays static), and the fixture
  builds, owns and removes the emulator's MSSQL sidecar and network (messagebus follow-up), also with Ryuk off. Deleting
  the Ryuk override turned out to matter for every fixture: with Ryuk off Testcontainers only stops a disposed
  container. New rows `P-28` (Ryuk on by default) and `P-29` (`ServiceBusFixture` per instance, owned sidecar). The
  Docker.DotNet reuse helpers move into the internal `ReusedEmulator`; `docs/ServiceBus.Testing.md` is corrected here.
  The CI criterion waits for a push.
- Step 03: only the `C-23` and `C-24` members changed; internal `Async` names in SQL.Testing stay, `Flush` gains no
  token, and `WireMockServer` has a private setter for `Run`. The Azurite 3.37.0 pin is unchanged.
- Step 04: forwarding members of `JsonResponseBuilderDecorator` and interface implementations use `<inheritdoc />`.
- Step 05: the pages keep their structure (targeted edits); `HTTP.Testing.md` gains a "Strict matching" section, and
  `API.Testing.md` and `SQL.Testing.md` gain usage snippets. Six other pages that called renamed members are updated
  too. All 16 snippets compile in a throwaway project.
- Step 06: `JsonResponseBuilderDecorator` and `SQLFixture` become partial classes (the SQL split moves the fluent
  configuration, not the engine setup), `ServiceBusFixture` hands its containers to the internal `OwnedEmulator`, and
  the MessageBus `ModuleInstallerTests` split moves the endpoint tests to `ModuleInstallerEndpointTests` (messagebus
  follow-up). Three assertion and client helper classes move into `extension` blocks; `ShouldBeSuccess<T>` stays
  classic (Guide §9.7). ApiCompat shows only the `extension` marker types and the accepted `CP0021` "notnull".

## Follow-ups

Recorded during implementation (the retrospective consolidates this list):

- A shipped Hangfire testing helper. The Hangfire tests keep their own harness (`RecurringJobHost`, the performer and
  state-machine loop, `HangfireGlobalState.Reset`); no step of this feature plans a Hangfire kit. A shipped helper must
  reset Hangfire's process-wide state (the log provider, `GlobalJobFilters`, `JobFilterProviders.Providers`) on
  dispose, otherwise a disposed container leaves Hangfire's global logger pointing at a disposed factory.
- A `*.Testing` test project (Q6). Until then the kits are covered through their consumers.
- A Blob test project with the five Azurite tests from the blob code review (AR-1); it stays a Blob follow-up.
- With `TESTCONTAINERS_RYUK_DISABLED=true`, a disposed SQL, Redis or Azurite fixture only stops its container
  (Testcontainers deletes only with Ryuk on); `WithAutoRemove` on those builders would remove them as
  `ServiceBusFixture` already does.
- Step 02's CI criterion: `runTests.ps1` on ubuntu with Ryuk on, after the push.

## Code review fixes (2026-11-09)

An independent code review of `8e3908f0..032ed22a` found four issues; each was fixed with the user's decision, in its
own commit. Rows: `C-33` (new), P-11 (consumer impact amended); the P-11 release-notes input in the core brief names
both opt-outs.

| # | Finding | Fix | Commit | Test |
|---|---|---|---|---|
| 1 | Medium. `WireMockFixture.Initialize(int)` always ran strict (P-11); the documented opt-out existed only on `WireMockStartup.Run(settings)`, so the release note was wrong for the fixture. | New `WireMockFixture.Initialize(WireMockServerSettings)` (`C-33`); the port overload delegates to it with `DefaultSettings(port)`. `HTTP.Testing.md` documents `AllowPartialMapping = true` and `wireMock.Initialize().WireMockServer.AllowPartialMapping()`; the fixture has no `WireMockServer` property, so the one-liner goes through the `WireMockStartup` that `Initialize` returns instead of adding API. Release note and P-11 row fixed. | `9eba42cf` | `WireMockFixtureTests`: a partial match is served with `AllowPartialMapping = true` and gets 404 without it; `AllowPartialMapping()` on the running server; null settings throw. |
| 2 | Low. The Blob.Testing and Redis.Testing package descriptions and `canonical-versions.md:89` named the pre-`C-23` methods. | Descriptions name `CreateBlobContainer` / `Clear` and `Flush()`; the reference names `RedisFixture.Flush()`. A grep of every `src` csproj, `.github` and the module docs finds no other old name. | `b8c229f4` | — |
| 3 | Low. `TestContainersContext.DisposeAsync` was reported to leak its network with Ryuk off, because `DockerNetwork` deletes only with a reaper session id. | `DisposeAsync` deletes the network before disposing it. The leak did not reproduce on Testcontainers 4.16: every resource gets the reaper-session label by default and `NetworkBuilder` never calls `WithCleanUp`, so only `WithCleanUp(true)` with Ryuk off (as the SQL, Redis and Azurite fixtures do) clears the session id. The explicit delete is kept as decided, with a comment that says so; `Testing.md` now lists what stays without Ryuk. | `25c6d012` | `TestContainersContextTests` (in MessageBus.Tests, the Docker test project that references the kit; Q6) removes the network with Ryuk on and off; it also passes against the old code. |
| 4 | Low. `OwnedEmulator.DisposeAsync` stopped at the first failure, and in `ServiceBusFixture.StartEmulator` a clean-up exception replaced the start exception. | Each of the three removals is attempted; failures are thrown together as one `AggregateException`. A failed start rethrows the start exception, attaches the clean-up failure to its `Data["ServiceBusFixture.CleanupFailure"]` and writes it to the console. The fixture clears its emulator reference in a `finally`. | `8b4e3c6f` | No fault-injection test: `OwnedEmulator` builds real containers and has no seam for a failing removal. The lifecycle tests still pass. |

Verification after the fixes: HTTP.Tests 162/162 and MessageBus.Tests 42/42; `dotnet test SolTechnology.Core.slnx
-c Release` passes 928 with 11 skipped and none failed (922 before the fixes, plus 4 `WireMockFixtureTests` and 2
`TestContainersContextTests`). `docker ps -a` and `docker network ls` show no container or network added by the run.

