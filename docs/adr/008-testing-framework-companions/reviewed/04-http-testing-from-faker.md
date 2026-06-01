---
adr: 008-testing-framework-companions
step: 04 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/03-http-testing-from-faker.md. Faker rename is an
     ACCEPTED breaking change; type-forwarding dropped (impossible across a namespace
     change); consumer migration is step 10. -->

# Step 04: Migrate the WireMock DSL to `SolTechnology.Core.HTTP.Testing`

## Summary
Move the WireMock fixture + fluent fake-API DSL from `SolTechnology.Core.Faker` into a new
companion of the HTTP module, and unify the three divergent dialects (`Faker` `Fake<T>`, MTS
`IApiMock`/`Mock<T>`, KYC `FakeServer` `IFakeApi`) into one surface. The namespace change from
`SolTechnology.Core.Faker` to `SolTechnology.Core.HTTP.Testing` is an **accepted breaking change**
(ADR-008). Single PR: the fixture, startup and DSL base types are one cohesive mocking framework
(plumbing + DSL ship together).

## Affected components
- `src/SolTechnology.Core.HTTP.Testing/SolTechnology.Core.HTTP.Testing.csproj` — new package. Full metadata mirroring the companion shape (`Description`, `PackageTags`, `Product`, `PackageId` `SolTechnology.Core.HTTP.Testing`, `AssemblyName`, `PackageIcon`, `PackageReadmeFile`, `RepositoryUrl`, version `0.1.0`). References `WireMock.Net` 1.6.8, `WireMock.Net.StandAlone` 1.6.8, **and `System.Linq.Dynamic.Core` 1.6.0** (carried over from Faker). Depends on `SolTechnology.Core.Testing`.
- `src/SolTechnology.Core.HTTP.Testing/WireMockFixture.cs` — from `SolTechnology.Core.Faker/WireMockFixture.cs`.
- `src/SolTechnology.Core.HTTP.Testing/WireMockStartup.cs` — from Faker.
- `src/SolTechnology.Core.HTTP.Testing/FakesBase/` — `IFakeApi`, `IFakeApiBuilderWithRequest`, `IFakeApiBuilderWithResponse`, `FakeApiBase` (from Faker `FakesBase/`).
- `src/SolTechnology.Core.HTTP.Testing/WireMock/` — extension/response helpers from Faker `WireMock/`.
- `src/SolTechnology.Core.Faker/` — **breaking change.** The namespace changes, so type-forwarding is technically impossible. Ship **either** a thin `[Obsolete]` wrapper shim in the old `SolTechnology.Core.Faker` package that points consumers to `SolTechnology.Core.HTTP.Testing`, **or** delete the `Faker` package outright after step 10 migrates the in-repo consumers — implementer's call on which is cleaner. Document the breaking change with a migration note either way.
- `docs/Clients.md` / `docs/HTTP-Production-Checklist.md` — note the mock companion (full doc pass in step 11).

## Details
- Canonical DSL: `fixture.Fake<TClient>().WithRequest(x => x.Method, args).WithResponse(x => x.WithSuccess().WithBodyAsJson(...))` — keyed off the generated HTTP client interface (the MTS `Mock<T>` naming is dropped in favour of `Fake<T>`).
- Preserve `RegisterFakeApi(IFakeApi)` and `Fake<T>()` resolution-by-generic-base-type from `WireMockStartup`.
- The namespace rename `SolTechnology.Core.Faker` → `SolTechnology.Core.HTTP.Testing` is intentional and breaking; record the migration in step 10 notes.
- Do **not** pull the MTS `WithAccessPayAuthentication` app-specific seeding into the package — that stays app-side.
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.HTTP.Testing.Tests`; validation is build-based plus a documented manual smoke (a fake API registers and responds). Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.HTTP.Testing` succeeds.
- A documented manual smoke registers a sample fake API and gets a response via `WireMockServer`.
- The `Faker` package is **either** reduced to a thin `[Obsolete]` shim pointing at `HTTP.Testing` **or** scheduled for deletion in step 10, with a breaking-change migration note recorded.

## Open questions
- Keep `Faker` as a thin `[Obsolete]` compatibility shim, or delete it once step 10 migrates all in-repo consumers? Decide during implementation; document the choice as a breaking change.

