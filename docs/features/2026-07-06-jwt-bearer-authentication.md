# JWT Bearer authentication + API-key hardening

> **Status:** Proposed
> **Created:** 2026-07-06

## Goal

Implement [ADR-014](../adr/014-jwt-bearer-authentication.md): add a provider-agnostic JWT
Bearer scheme to `SolTechnology.Core.Authentication`, prove it end-to-end against Keycloak
in integration tests, and document the authorization-code + PKCE flow. While touching the
module, harden the existing API-key handler (constant-time comparison, logger field bug).

## Scope

- In: `AddSolJwtAuthentication(JwtAuthenticationConfiguration)` installer + options class.
- In: `Microsoft.AspNetCore.Authentication.JwtBearer@10.0.9` package reference.
- In: API-key hardening — `CryptographicOperations.FixedTimeEquals`, non-static logger.
- In: Keycloak integration tests via `Testcontainers.Keycloak@4.12.0`.
- In: `docs/Auth.md` rewrite — fixes the existing drift (doc shows retired
  `AddAuthenticationAndBuildFilter()`; code has `AddSolAuthentication()`) and adds the
  JWT + PKCE guide.
- Out: wiring JWT into `DreamTravel.Api` (user decision, 2026-07-06).
- Out: multi-key / per-client API-key identity (possible future feature).
- Out: token issuance (OpenIddict/IdentityServer) — see ADR-014 alternative 2.

## Affected modules

- `src/SolTechnology.Core.Authentication`
- `tests/SolTechnology.Core.Authentication.Tests`
- `docs/Auth.md`

## Semver impact

MINOR.

## Related

- Driving decision: [ADR-014](../adr/014-jwt-bearer-authentication.md)
- Steps: [2026-07-06-jwt-bearer-authentication/summary.md](2026-07-06-jwt-bearer-authentication/summary.md)
