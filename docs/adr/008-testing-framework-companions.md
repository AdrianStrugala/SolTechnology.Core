# ADR-008: Modular `.Testing` companion packages for the SolTechnology.Core testing framework

> **Status:** Accepted
> **Decision Date:** 2026-05-30
> **Decision Maker:** Adrian Strugala / Core maintainers

## Context

Four production-grade applications drive the SolTechnology.Core test conventions:
TaleCode and DreamTravel (in `sample-tale-code-apps/`), plus KYC (`tests/tests-kyc/`) and
MTS (`tests/tests-mts/`). A detailed survey of all four shows the **same component / integration
testing recipe re-implemented three to four times**, with drift between copies:

- **`WireMockFixture` exists 4×** — `SolTechnology.Core.Faker`, MTS `ApiMocks` (`IApiMock`,
  `Mock<T>`), KYC `FakeServer` (`IFakeApi`, `Fake<T>`). Same idea, divergent DSL.
- **`SqlFixture` exists 3×** — the core dacpac-based `SQLFixture` (MSSQL only, lives *inside*
  `SolTechnology.Core.Sql`), MTS Postgres fixture, KYC MSSQL + Postgres fixtures.
- **`Redis`, `RabbitMQ`, `ServiceBus`, `Blob`/`LocalStack` container fixtures** are hand-rolled
  per app.
- **`Retry.UntilConditionMet` exists 3×**; AutoFixture `[AutoMoqData]` attributes + customizations
  (UTC, NodaTime, DateOnly, DbContext) exist 2×.

Two reusable seeds already ship as companion packages and prove the convention:
`SolTechnology.Core.API.Testing` (`APIFixture<TEntryPoint>`, v0.6.0) and the `SQLFixture` currently
buried in `SolTechnology.Core.Sql/Testing/`. The repo convention is **one `.Testing` companion
NuGet package per core package**, referenced from test projects only.

Constraints fixed by the maintainer for this work:

- **NUnit only.** xUnit is explicitly rejected for the framework surface (the `[SetUpFixture]`
  orchestration model used by three of four apps wins; KYC's xUnit `AssemblyFixture` approach is
  not carried forward).
- **Extract `SQLFixture`** out of `SolTechnology.Core.Sql` into its own companion package, named
  with capital **`SQL`**: `SolTechnology.Core.SQL.Testing`.
- **Support both MSSQL and Postgres.**
- **Rename the WireMock DSL** `SolTechnology.Core.Faker` → `SolTechnology.Core.HTTP.Testing`.
- **Blob testing is Azure-specific** — Azurite only, no LocalStack/AWS.
- **Add `SolTechnology.Core.ServiceBus.Testing`** (Azure Service Bus emulator), promoted from
  follow-up into this ADR.
- **Lifetime/reuse is a first-class concern.** The container/host/fixture lifetime optimisations
  recently introduced in KYC (container reuse across runs, restart-if-stopped, AMQP readiness probe,
  one-time semaphore-guarded init, Ryuk disabling for ECI) are part of the framework, not per-app.

## Decision

Ship **seven modular `.Testing` companion packages**, each a self-contained set of NUnit fixtures
with no inter-package coupling beyond a shared foundation. An application composes them in a single
`[SetUpFixture]` orchestrator.

| Package | Action | Responsibility |
|---|---|---|
| `SolTechnology.Core.Testing` | **new** | Foundation: `[AutoMoqData]` / `[InlineAutoMoqData]`, AutoFixture customizations (UTC, NodaTime, DateOnly), `Retry.UntilConditionMet`, **`TestContainersContext` (shared docker network + reuse policy + lifecycle helper)**, `InMemorySink` log assertions. NUnit. |
| `SolTechnology.Core.API.Testing` | **extend** | Existing `APIFixture<TEntryPoint>` + auth-client helpers (User / NoAuth) + configuration-override builder. |
| `SolTechnology.Core.SQL.Testing` | **new (extract)** | `SqlFixture` moved out of `Sql`. Engine abstraction `IDatabaseEngine` → MSSQL + Postgres. Pluggable schema provisioning: dacpac (`.sqlproj`), EF migrations, raw scripts. Respawn-based reset. **ORM-agnostic** (serves Dapper and EF alike). |
| `SolTechnology.Core.HTTP.Testing` | **new (migrate, breaking)** | The unified WireMock DSL migrated from `SolTechnology.Core.Faker`. Single `Fake<TClient>().WithRequest(...).WithResponse(...)` surface; retires MTS `IApiMock` and KYC `FakeServer` dialects. The namespace change `SolTechnology.Core.Faker` → `SolTechnology.Core.HTTP.Testing` is an **accepted breaking change** — type-forwarding is impossible across a namespace change, so the old `Faker` package becomes a thin `[Obsolete]` shim or is deleted after consumers migrate. |
| `SolTechnology.Core.Redis.Testing` | **new** | Redis Testcontainer fixture + connection-string wiring. |
| `SolTechnology.Core.BlobStorage.Testing` | **new** | **Azurite only** (Azure-specific blob storage). No LocalStack. |
| `SolTechnology.Core.ServiceBus.Testing` | **new** | Azure Service Bus emulator fixture with AMQP readiness probe + reuse-aware lifecycle (port of KYC `ServiceBusFixture` + `ServiceBusInstanceBuilder`). |

### Data generation: keep AutoFixture, do not replace with Bogus

AutoFixture and Bogus are **not interchangeable**. AutoFixture provides the anonymous-object engine,
the `AutoMoq` mocking integration, and — critically — the NUnit `[AutoMoqData]` / `[InlineAutoMoqData]`
attribute model that both MTS and KYC depend on. Bogus is a *deterministic realistic-value builder*
with no NUnit-attribute or auto-mock integration; it would replace neither `[AutoMoqData]` nor
`AutoMoqCustomization`. Decision: **AutoFixture stays as the foundation engine.** Bogus may be added
later as an *optional, complementary* builder for realistic domain data (a customization or builder
helper), but it does not replace AutoFixture and is out of scope here.

### Container / fixture lifetime (cross-cutting)

Every container-backed fixture in this framework MUST follow the KYC-proven lifetime model, centralised
in `SolTechnology.Core.Testing`:

- **Reuse across runs** gated by `TESTCONTAINERS_REUSE` — `.WithReuse(true)` + stable container name +
  shared, named docker network; dispose is a **no-op when reuse is enabled**.
- **Restart-if-stopped** — a `ContainerLifecycleHelper.EnsureRunningAsync` that detects an externally
  stopped container (e.g. Docker Desktop) and restarts it, re-reading mapped ports.
- **One-time, thread-safe init** — semaphore-guarded `Initialized` flags + cached connection strings so
  parallel fixtures don't double-provision.
- **Real readiness probes, not TCP-accept** — host-side login probe (SQL) and AMQP SASL-echo probe
  (Service Bus), carried over verbatim from KYC.
- **Ryuk disabled** (`ResourceReaperEnabled = false`) for Docker Desktop Enhanced Container Isolation.

**On the `EF.Testing` naming question:** rejected. The fixture's contract is *start container →
provision schema → return connection string → reset*. Whether the consumer reads via Dapper or EF
is invisible to the fixture — both consume a connection string. The real axes are **engine**
(MSSQL vs Postgres: image + connection-string builder + Respawn adapter) and **schema provisioning**
(dacpac vs EF migrations vs raw scripts). Both are modelled inside one ORM-agnostic
`SolTechnology.Core.SQL.Testing`, named as the companion of the existing `SolTechnology.Core.Sql`
package (capital `SQL` per the maintainer). EF appears only as one provisioning strategy (a migration
runner); Dapper needs nothing extra.

`SolTechnology.Core.MessageBus.Testing` covering **RabbitMQ** remains out of scope (deferred); only the
Azure Service Bus emulator is in scope via `ServiceBus.Testing`.

## Alternatives Considered

1. **Framework-agnostic fixtures + thin NUnit *and* xUnit adapters.**
   *Pros:* serves both NUnit consumers and the xUnit core test-suite style; future-proof.
   *Cons:* doubles the adapter surface and CI matrix; KYC is the only xUnit consumer and is an
   external snapshot, not an in-repo sample. Rejected per the explicit NUnit-only constraint.

2. **One `EF.Testing` package keyed on the ORM, with a separate Postgres package.**
   *Pros:* superficially matches the maintainer's first instinct.
   *Cons:* companion to a non-existent `Core.EF` package; misleads Dapper users; splits a 30-line
   engine difference across two packages and two release cadences. Rejected in favour of one
   ORM-agnostic `SolTechnology.Core.SQL.Testing` with an `IDatabaseEngine` seam.

3. **Keep `SQLFixture` inside `SolTechnology.Core.Sql`; add Postgres there.**
   *Pros:* no new package, no consumer churn.
   *Cons:* forces every runtime `Sql` consumer to drag Testcontainers + Respawn + DacFx into
   production output; violates the "test-only, reference from test projects" rule the other
   companions follow. Rejected.

4. **Replace AutoFixture with Bogus.**
   *Pros:* realistic, deterministic domain data; readable builders.
   *Cons:* Bogus has no NUnit `[AutoMoqData]` attribute integration and no auto-mock; it cannot
   replace `AutoMoqCustomization` or the inline-data attribute model both MTS and KYC rely on.
   Rejected as a *replacement*; allowed later as an optional complementary builder.

5. **Per-app container lifetime (status quo).**
   *Pros:* no shared abstraction.
   *Cons:* the KYC reuse/restart/readiness improvements would stay trapped in one app; everyone else
   keeps paying full container-boot cost every run. Rejected — lifetime model is centralised in
   `SolTechnology.Core.Testing`.

## Consequences

**Positive:**
- One canonical fixture per concern; four divergent copies collapse to one DSL each for HTTP mocks
  and SQL.
- Test-only dependencies (Testcontainers, Respawn, WireMock) leave production assemblies.
- New apps compose a full component-test harness from NuGet instead of copy-paste.
- MSSQL + Postgres parity from a single fixture; Dapper and EF both served.
- The KYC container-reuse / restart / readiness-probe wins become available to every consumer,
  cutting repeated-run test time.

**Negative:**
- `SQLFixture` moves out of `SolTechnology.Core.Sql` — existing references in TaleCode / DreamTravel
  must add a package reference (namespace preserved to minimise churn).
- `SolTechnology.Core.Faker` is superseded by `SolTechnology.Core.HTTP.Testing` via an **accepted
  breaking change**: the namespace changes, so no type-forwarding is possible. Faker consumers must
  switch package **and** update `using` directives; the old package ships only as a thin `[Obsolete]`
  shim or is deleted after in-repo consumers migrate.
- Seven packages to version and release; the CI publish workflow (`publishPackages.yml`) must be
  extended to pack all seven companions (including `API.Testing`, currently unpacked) and stop
  packing `Faker`.
- The `.Testing` companions ship **without dedicated test projects** — verification is build-based
  plus documented manual smoke checks, so the core CI lane and PR builds are unaffected (no
  Docker-dependent integration tests run in CI).

**Semver impact:** MINOR for the in-repo runtime packages. All affected packages are pre-1.0
(Sql 0.5.0 → next MINOR, API.Testing 0.6.0 → 0.7.0); new companions ship at 0.1.0. The `Sql`
type-removal is breaking in principle but test-only and pre-1.0, handled as MINOR with namespace
and type-name (`SQLFixture`) preservation. The `Faker` rename is an **accepted breaking change**
(namespace move, no type-forwarding) documented with a migration note; pre-1.0 and test-only.

## Related

- [ADR-006](006-implementation-plan-workflow.md) — plan layout and folder-state model this plan follows.
- [ADR-005](005-http-production-defaults.md) — HTTP module the `HTTP.Testing` companion accompanies.
- [ADR-007](007-cqrs-production-hardening.md) — prior multi-step package-hardening precedent.
- [`docs/theQuality.md`](../theQuality.md) — testing pyramid + Component test guidance updated by this plan.
- Surveyed apps: `sample-tale-code-apps/TaleCode`, `sample-tale-code-apps/DreamTravel`,
  `tests/tests-kyc`, `tests/tests-mts`.

## Implementation summary

Partially collapsed 2026-06-06. Ten of the eleven steps shipped; the per-step working folder
(`docs/adr/008-testing-framework-companions/`) was pruned to **only the one outstanding step** — the
publish workflow — per a maintainer call (a variation on the ADR-006 collapse-on-completion rule, which
normally deletes the whole folder once *every* step ships).

| # | Step | Shipped |
|---|---|---|
| 01 | Premortem (plan gate) | Verdict *Go with mitigations*; HTTP.Testing hardening pass recorded (dynamic port, `Dispose`/`Reset` split, type-safe `Fake<T>`). |
| 02 | `SolTechnology.Core.Testing` (foundation) | `Retry`, `AutoNSubstituteData` / `InlineAutoNSubstituteData` / `AutoBogusData`, `BogusCustomization`, `DateOnlyCustomization`, `TestContainersContext`, `ContainerLifecycleHelper` (AMQP probe), `InMemorySinkAssertions`. |
| 03 | `SolTechnology.Core.SQL.Testing` | `SQLFixture` (MSSQL + Postgres via `IDatabaseEngine`; dacpac / EF / scripts provisioning; Respawn `SQLReset`; `ISharedSQLContainer`). `SolTechnology.Core.SQL` keeps DacFx, drops the Testcontainers runtime dep. |
| 04 | `SolTechnology.Core.HTTP.Testing` | WireMock DSL migrated from `Faker`; `WireMockFixture` + `Fake<T>` + `FakeApiBase`. **Breaking** namespace change. |
| 05 | `SolTechnology.Core.API.Testing` (extended) | `AuthClientExtensions` (`CreateAuthorizedClient` / `CreateAnonymousClient`), `TestConfigurationBuilder`; `0.6.0 → 0.7.0`. |
| 06 | `SolTechnology.Core.Redis.Testing` | `RedisFixture` (`HostName` / `ConnectionString` / `FlushAsync` / `WithNetwork`). |
| 07 | `SolTechnology.Core.BlobStorage.Testing` | `AzuriteFixture` (Azure-only; `CreateBlobContainerAsync` / `ClearAsync`). |
| 08 | `SolTechnology.Core.ServiceBus.Testing` | `ServiceBusFixture` (emulator + AMQP readiness probe + stable-name reuse). |
| 10 | Dogfood DreamTravel | Component suite runs on the packages (5/5; ~4× faster on warm reuse); local `Retry` removed; orphan `src/SolTechnology.Core.Faker` deleted. |
| 11 | Documentation | Per-package readmes + `theQuality.md` framework story + README testing-companions table. |

### Preserved deviations

- **02** — `Moq` is on the repo anti-stack → shipped `AutoNSubstituteData`, not `AutoMoqData`. Bogus is an
  *optional* complement (`BogusCustomization`); AutoFixture stays the engine. The `ContainerReuse` helper
  was dropped — an assembly-level `[OneTimeSetUp]` already gives within-run reuse for free.
- **04** — `Faker` was **deleted outright** (no `[Obsolete]` shim): one in-repo consumer, pre-1.0,
  fix-at-source. `FakeApiBase` is non-generic and `WithRequest` uses a direct call (no reflection →
  IntelliSense + compile-time arg checks).
- **06** — `RedisFixture.FlushAsync` needs `AllowAdmin = true` (`FLUSHALL` is a server/admin command).
- **07** — pinned the **Testcontainers 3.9.0** family (KYC's 4.3.0 would conflict with the other companions).
- **08** — `Testcontainers.ServiceBus` 4.x makes the emulator **self-manage its MSSQL sidecar**; the
  `ISharedSQLContainer` contract is therefore **not** consumed by `ServiceBus.Testing` (the seam still
  ships in `SQL.Testing` for any future direct-MSSQL consumer).
- **10** — TaleCode has **no tracked in-repo source** (only stale `obj/`), so it could not be migrated;
  only DreamTravel was dogfooded.

### Remaining — the one open TODO

- **Step 09 — wire the publish workflow.** `.github/workflows/publishPackages.yml` must `dotnet pack` all
  seven companions (including `API.Testing`, currently unpacked) and stop packing `Faker`; otherwise the
  packages build but never reach NuGet. Plan preserved at
  [`008-testing-framework-companions/reviewed/09-publish-workflow.md`](008-testing-framework-companions/reviewed/09-publish-workflow.md).

