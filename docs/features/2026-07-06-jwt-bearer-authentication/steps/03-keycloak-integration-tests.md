---
spec: 2026-07-06-jwt-bearer-authentication
step: 03
status: to-do
---

# Step 03: Keycloak integration tests

## Summary

Proves the JWT Bearer scheme end-to-end against a real OIDC provider: a Keycloak container
issues tokens, a TestHost API validates them via `AddSolJwtAuthentication`. Automated
tests use the `client_credentials` grant — token **validation** is identical for
PKCE-obtained tokens; PKCE itself is a browser-interactive flow covered by the step 04
documentation walkthrough.

## Affected components

- `tests/SolTechnology.Core.Authentication.Tests/SolTechnology.Core.Authentication.Tests.csproj` — EDIT — packages
- `tests/SolTechnology.Core.Authentication.Tests/Integration/KeycloakJwtTests.cs` — NEW — test fixture
- `tests/SolTechnology.Core.Authentication.Tests/Integration/keycloak-realm.json` — NEW — realm import

## Changes

- csproj: add `Testcontainers.Keycloak@4.12.0` (family pinned — see
  `canonical-versions.md` row for `Testcontainers`) and
  `Microsoft.AspNetCore.Mvc.Testing@10.0.9`.
- `keycloak-realm.json`: realm `sol-test`; confidential client `sol-api-tests`
  (`client_credentials` enabled) with an audience mapper emitting `aud: sol-api`; public
  client `sol-spa` with PKCE (`S256`) required — referenced by the step 04 walkthrough.
- `KeycloakJwtTests`: `KeycloakBuilder` fixture (`[OneTimeSetUp]`/`[OneTimeTearDown]`,
  realm imported at startup); TestHost app with one `[Authorize]` endpoint registered via
  `AddSolJwtAuthentication(new JwtAuthenticationConfiguration { Authority = <realm URL>,
  Audience = "sol-api", RequireHttpsMetadata = false })`.
- Test matrix (`[TestCase]` where data-only differences):
  | scenario | expected |
  |---|---|
  | valid token from Keycloak | 200 |
  | no `Authorization` header | 401 |
  | token with tampered signature | 401 |
  | wrong audience (`aud` ≠ `sol-api`) | 401 |
  | expired token | 401 |

## Acceptance criteria

- [ ] `dotnet test tests/SolTechnology.Core.Authentication.Tests` green locally with
      Docker running.
- [ ] Integration tests marked with an NUnit category consistent with existing
      container-backed tests in `tests/`, so CI lanes without Docker can exclude them.
- [ ] `dotnet build SolTechnology.Core.slnx` green.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
