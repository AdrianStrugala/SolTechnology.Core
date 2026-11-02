---
status: planned
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

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-11 | `WireMockStartup` `AllowPartialMapping` defaults to `false` | Loose tests start failing (intended) | testing-kits 01 |

User decisions (2026-11-02):

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| Q6 | New test project `tests/SolTechnology.Core.Blob.Tests` (and `*.Testing` tests)? | **No new test projects.** No existing test project references Blob, so steps 16 and 50 validate with build and source checks; accepted risk AR-1. | 11, 16, 50 |

Accepted risks:

| ID | Risk | Source | Mitigation in this plan |
|---|---|---|---|
| AR-1 | `B6`, `infra#6`, and `infra#7` fixes in Blob ship without automated tests, and Blob has no DI resolution test. | Q6 | Steps 16 and 50 gate on a Release build and source greps; a manual Azurite check is recommended in the PR. Follow-up: a Blob test project. |

Premortem mitigations carried verbatim from core 00: none.

No test project exists for the `*.Testing` packages, and none is created (Q6, AR-1); the kits are exercised by the library test projects that use them (SQL.Tests, Cache.Tests, MessageBus.Tests, Api.Tests, HTTP.Tests). The kits have no L-03 template row; step 06 runs the library-scoped `[{Placeholder}]` check. Delivered last: step 03 renames fixture methods that the SQL, Cache, and MessageBus features start using.

## Scope

### In scope

- Source: `src/SolTechnology.Core.Testing`, `src/SolTechnology.Core.HTTP.Testing`, `src/SolTechnology.Core.API.Testing`, `src/SolTechnology.Core.SQL.Testing`, `src/SolTechnology.Core.Redis.Testing`, `src/SolTechnology.Core.Blob.Testing`, `src/SolTechnology.Core.ServiceBus.Testing`, and the matching test projects.
- Module docs: `docs/Testing.md`, `docs/HTTP.Testing.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md`, `docs/Redis.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md`.
- Phases, in order: correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- A test project for the `*.Testing` packages (Q6, AR-1).
- WireMock 2.13 → 2.21 (outdated-package sweep).
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps live in the temporary working folder `2026-11-02-core-2-0-testing-kits/` (see its `summary.md`). Gates: `review` is
`pending` (breaking public API or persisted contract, so the planner cannot waive it; the user may
skip it); `premortem` is `waived` — the core premortem verdict `go-with-mitigations (2026-11-02)`
covers these steps.

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

## Acceptance criteria

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] `C-23` and `C-24` shipped; every test project that uses a fixture compiles against the renamed members.
- [ ] A WireMock request without a matching mapping fails the test by default (P-11).
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Api.Tests` and `dotnet test tests/SolTechnology.Core.HTTP.Tests` and `dotnet test tests/SolTechnology.Core.SQL.Tests` and `dotnet test tests/SolTechnology.Core.Cache.Tests` and `dotnet test tests/SolTechnology.Core.MessageBus.Tests` are green.
- [ ] `find src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Testing.md`, `docs/HTTP.Testing.md`, `docs/API.Testing.md`, `docs/SQL.Testing.md`, `docs/Redis.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

## Follow-ups
