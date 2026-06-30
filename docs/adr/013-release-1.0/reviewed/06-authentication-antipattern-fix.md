---
adr: 013-release-1.0
step: 06 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): de-contingented this step (answer 3 = fix); resolved the missing-test-host
     blocker (B2) by spelling out a new SolTechnology.Core.Authentication.Tests project wired into
     .slnx (runTests.ps1 auto-discovers it); switched the DreamTravel edits to symbol greps (M2). -->

# Step 06: Fix the Authentication `BuildServiceProvider` anti-pattern

## Summary
`AddSolAuthentication` (renamed in step 05) currently calls `services.BuildServiceProvider()` mid-
registration — building a throwaway container that drops singletons and hides options validation —
and returns a non-chainable `AuthorizeFilter`. This step removes both smells. It is **logic**, not a
mechanical rename, so it is isolated from step 05 per the "no plumbing + logic in one PR" rule. The
maintainer chose **fix in `1.0`** (step 00, answer 7), so this step **runs** — it is no longer
contingent.

## Affected components
- `src/SolTechnology.Core.Authentication/ModuleInstaller.cs` — EDIT — remove `BuildServiceProvider`; change return type to `IServiceCollection`.
- `tests/SolTechnology.Core.Authentication.Tests/` — **NEW PROJECT** (resolves the missing test host — see below).
- `tests/SolTechnology.Core.Authentication.Tests/ModuleInstallerTests.cs` — NEW — negative test (missing `ApiKey` fails fast without building a provider).
- `SolTechnology.Core.slnx` — EDIT — add the new test project under the `/Tests/` folder so CI builds it.
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — drop the `var authFilter = …` threading (locate by symbol, not line number).

## Test home (B2 — there is no `SolTechnology.Core.Authentication.Tests` today)
`src/SolTechnology.Core.Authentication` has **no** companion test project (`tests/` has Api, AUID,
Cache, CQRS, Guards, HTTP, Hangfire, Logging, MessageBus, SQL, Story, Tests — but **not**
Authentication). The new negative test needs a home. **Decision: create
`tests/SolTechnology.Core.Authentication.Tests/`** rather than fold the test into an unrelated project:
- New minimal csproj (`<Project Sdk="Microsoft.NET.Sdk.Web">`) with a single
  `<ProjectReference Include="..\..\src\SolTechnology.Core.Authentication\SolTechnology.Core.Authentication.csproj" />`.
  Test packages (NUnit, NSubstitute, FluentAssertions, `Microsoft.NET.Test.Sdk`, `<IsPackable>false>`)
  are inherited from `tests/Directory.Build.props` — do **not** re-declare them.
- Use the repo's standard test stack to match its siblings: **NUnit + NSubstitute + FluentAssertions**
  (`[TestFixture]` / `[Test]`, `act.Should().Throw<…>().WithMessage("…")`).
- **Add the project to `SolTechnology.Core.slnx`** (under `/Tests/`) so `dotnet build SolTechnology.Core.slnx`
  compiles it. `.github/runTests.ps1` runs `dotnet test --no-build` over **every** directory under
  `tests/`, so it picks up the new project automatically — **no `runTests.ps1` edit is required**
  (the build that feeds `--no-build` is what depends on the slnx entry).

## Changes
- Replace the `services.BuildServiceProvider().GetRequiredService<IOptions<AuthenticationConfiguration>>().Value`
  read with direct use of the `authenticationConfiguration.ApiKey` argument already passed in (the
  value is in hand — no provider needed to read it back).
- Change the signature from `static AuthorizeFilter AddSolAuthentication(...)` to return
  `IServiceCollection` (chainable). Register the `AuthorizeFilter` via an `IConfigureOptions<MvcOptions>`
  (post-configure) so the consumer no longer threads a returned `authFilter` into `opts.Filters.Add(authFilter)`.
- Update DreamTravel: drop `var authFilter = …`; rely on the new registration so `AddControllers`
  picks up the global authorization filter.
- Preserve behaviour: API-key scheme (`ApiKeyAuthenticationSchemeOptions` / `ApiKeyAuthenticationHandler`),
  `RequireAuthenticatedUser` policy, fail-fast `ArgumentException` on missing `ApiKey`.

## Acceptance criteria
- [ ] No `BuildServiceProvider()` call remains in `SolTechnology.Core.Authentication`.
- [ ] `AddSolAuthentication` returns `IServiceCollection` and is chainable.
- [ ] `tests/SolTechnology.Core.Authentication.Tests` exists, is in `SolTechnology.Core.slnx`, and its
      negative test asserts missing `ApiKey` throws at registration **without** building a provider.
- [ ] DreamTravel authorizes requests exactly as before (component/E2E auth tests green).
- [ ] `dotnet build SolTechnology.Core.slnx` green; `runTests.ps1` runs the new test project.

## Open questions
- none — the fix decision (answer 7) and the test-host decision (B2) are resolved.

