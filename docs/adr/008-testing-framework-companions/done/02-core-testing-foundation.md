---
adr: 008-testing-framework-companions
step: 02 of 11
status: done
---

<!-- Reviewed: renumbered from to-do/01-core-testing-foundation.md (premortem now leads). -->

<!-- IMPLEMENTATION DEVIATIONS (done):
  1. Mocking library: the plan said "port MTS `AutoMoqDataAttribute`". Porting it literally would pull
     `Moq` into a published `src/` package, which the repo anti-stack
     (package-management/references/canonical-versions.md) forbids ("Moq → use NSubstitute"). Shipped
     `AutoNSubstituteDataAttribute` / `InlineAutoNSubstituteDataAttribute` on `AutoFixture.AutoNSubstitute`.
  2. Data generation (maintainer request, post-review): removed `UtcDateTimeSpecimen` and
     `NodaTimeCustomization` ("not needed for now"). Added opt-in **Bogus** integration instead —
     `BogusCustomization` (member-aware realistic strings, optional seed), `BogusCustomization<T>`
     (user-supplied `Faker<T>`), and the `AutoBogusData` convenience attribute. `DateOnlyCustomization`
     kept. AutoFixture remains the engine; Bogus complements it (ADR-008 Alternative 4 unchanged).
  Versions: NUnit 4.2.2; AutoFixture + AutoFixture.AutoNSubstitute 4.18.1; AutoFixture.NUnit4 4.19.0;
  NSubstitute 5.3.0; Bogus 35.6.1; Testcontainers 3.9.0; Docker.DotNet 3.125.15;
  Serilog.Sinks.InMemory 0.11.0. Build: `dotnet build -c Release` → 0 warnings, 0 errors. -->

# Step 02: Foundation package `SolTechnology.Core.Testing`

## Summary
Create the shared NUnit foundation that every other companion and every consumer app depends on.
Collapses the duplicated `Retry`, AutoFixture attributes/customizations, shared docker network and
log-assertion helpers found across MTS `TestsInfrastructure`, KYC `TestUtilities` and the sample
apps into one package. **Also home of the cross-cutting container-lifetime/reuse model** so every
fixture in steps 03–08 inherits the KYC speedups instead of re-implementing them. Separate PR
because it is the dependency root for steps 03–08.

## Affected components
- `src/SolTechnology.Core.Testing/SolTechnology.Core.Testing.csproj` — new package (NUnit, AutoFixture, AutoFixture.AutoMoq, AutoFixture.NUnit4, Testcontainers, Docker.DotNet, Serilog.Sinks.InMemory). Use canonical versions via the `package-management` skill.
- `src/SolTechnology.Core.Testing/AutoMoqDataAttribute.cs` — port of MTS `AutoMoqDataAttribute` (`AutoDataAttribute` + `AutoMoqCustomization` + customization params).
- `src/SolTechnology.Core.Testing/InlineAutoMoqDataAttribute.cs` — port of MTS variant.
- `src/SolTechnology.Core.Testing/Customizations/` — `UtcDateTimeSpecimen`, `NodaTimeCustomization`, `DateOnlyCustomization` (deduped from KYC `TestUtilities` + MTS).
- `src/SolTechnology.Core.Testing/Retry.cs` — single `Retry.UntilConditionMet(func, predicate, attempts, delay)` (unify the 3 copies; keep both sync and async overloads observed in MTS/KYC).
- `src/SolTechnology.Core.Testing/Containers/TestContainersContext.cs` — port of KYC `Infrastructure/Containers/TestContainersContext.cs`: shared docker `INetwork` + alias, **`TESTCONTAINERS_REUSE`-gated reuse policy** (`.WithReuse(true)`, stable network name, dispose no-op when reuse on), and **Ryuk disabled** (`ResourceReaperEnabled = false`) for Docker Desktop ECI.
- `src/SolTechnology.Core.Testing/Containers/ContainerLifecycleHelper.cs` — port of KYC helper: `EnsureRunningAsync(containerId)` (restart externally-stopped containers, re-read mapped ports), Docker health polling, and `WaitForAmqpReadyAsync(port)` AMQP SASL-echo probe reused by `ServiceBus.Testing`.
- ~~`Containers/ContainerReuse.cs`~~ — **DROPPED (maintainer call, post-review).** Redundant under the assembly-level `[SetUpFixture]`/`[OneTimeSetUp]` model: a single one-time setup already boots each container exactly once per run, so the semaphore-guarded one-time-init cache added nothing for within-run reuse. Across-run reuse is provided by Testcontainers-native `.WithReuse(true)` in `TestContainersContext`; the one fixture that needs bespoke name-based detection (`ServiceBus.Testing`, step 08, due to the unstable reuse hash) keeps that logic privately.
- `src/SolTechnology.Core.Testing/Logging/InMemorySinkAssertions.cs` — wrap `Serilog.Sinks.InMemory` log assertions used by MTS.
- `docs/Testing.md` — new package readme target (wired in step 11; create stub here so csproj `PackageReadmeFile` resolves).
- `SolTechnology.Core.slnx` — add project.

## Details
- NUnit only. No xUnit types.
- No dependency on any other `.Testing` package — this is the root.
- **Lifetime model is mandatory and centralised here.** Fixtures in later steps are booted once by the
  consumer's assembly-level `[SetUpFixture]` `[OneTimeSetUp]` (within-run reuse is free) and consume
  `TestContainersContext` (+ `.WithReuse` for across-run reuse) and `ContainerLifecycleHelper`; they
  must not re-implement reuse/restart/readiness logic. There is no shared `ContainerReuse` helper.
- Reuse is opt-in via `TESTCONTAINERS_REUSE=true` (test `.runsettings` / env), default off so CI stays
  hermetic; document both modes.
- Keep public API minimal and documented with XML comments (consumers reference from test projects only).
- AutoFixture stays the data engine; do **not** introduce Bogus as a replacement (see ADR-008 decision).
- Mirror the existing companion csproj shape (`SolTechnology.Core.API.Testing.csproj`): `Description`, `PackageTags`, `PackageId`, `PackageIcon`, `PackageReadmeFile`, version `0.1.0`.
- This package inherits the production `Microsoft.Extensions.*` references from `src/Directory.Build.props` (Configuration, Configuration.Binder, DependencyInjection, Options). That is a **conscious choice** — the foundation needs DI/config types to wire test hosts — not an accidental leak.
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.Testing.Tests`; acceptance is build-based plus documented manual smoke checks. Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.Testing` succeeds.
- `[AutoMoqData]` produces a fixture with UTC `DateTime`, NodaTime and `DateOnly` specimens registered (verified via a documented manual smoke snippet, not an automated test project).
- `Retry.UntilConditionMet` exposes one async and one sync overload (verified by build/signature, no test project).
- With `TESTCONTAINERS_REUSE=true`, `TestContainersContext` builds a named, reusable network and dispose is a no-op; with it unset, the network is disposed — confirmed by a documented manual smoke.
- `ContainerLifecycleHelper.EnsureRunningAsync` restarts a stopped container in a **documented manual** check.
- Package contains zero references to MTS/KYC/sample-app namespaces.

## Open questions
- none

