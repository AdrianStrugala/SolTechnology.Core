# ADR-014: JWT Bearer authentication in SolTechnology.Core.Authentication

> **Status:** Proposed
> **Decision Date:** 2026-07-06
> **Decision Maker:** Adrian Strugala

## Context

`SolTechnology.Core.Authentication` offers a single mechanism: one static, shared API key
compared in `ApiKeyAuthenticationHandler` ([src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs](<../../src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs>)).
That is acceptable for internal machine-to-machine calls but not for user-facing clients:
no expiry, no per-caller identity, no rotation, and the key transits on every request.

The industry-standard answer for user-facing clients is OAuth 2.0 / OIDC with short-lived
JWT access tokens obtained via authorization code + PKCE (RFC 7636). PKCE is executed
entirely between the client application and the **authorization server (identity
provider)** — the resource server (the API) never sees the `code_verifier`. The library
therefore only needs **JWT Bearer validation** (issuer, audience, lifetime, signature via
the provider's JWKS endpoint); PKCE support is configuration on the identity-provider and
client side, plus documentation.

Constraint: the package currently depends only on `FrameworkReference
Microsoft.AspNetCore.App`. JWT Bearer validation requires the NuGet package
`Microsoft.AspNetCore.Authentication.JwtBearer`, which is **not** part of the ASP.NET
shared framework — this is the package's first external `PackageReference`
(~1,862 downloads affected per [nuget-stats.json](../../nuget-stats.json)).

## Decision

Extend `SolTechnology.Core.Authentication` (same package) with a provider-agnostic JWT
Bearer scheme: a new `AddSolJwtAuthentication(JwtAuthenticationConfiguration)` installer
method wrapping `Microsoft.AspNetCore.Authentication.JwtBearer@10.0.9`, configured by
`Authority` + `Audience`. Any OIDC-compliant identity provider works; **Keycloak** (via
`Testcontainers.Keycloak@4.12.0`) is the reference provider for integration tests and the
documented PKCE walkthrough. Token issuance stays out of scope — the library remains a
resource-server concern.

## Alternatives Considered

1. **Companion package `SolTechnology.Core.Authentication.Jwt`** — Pros: base package stays
   dependency-free for API-key-only consumers; mirrors the `.Testing` companion precedent
   ([ADR-008](008-testing-framework-companions.md)). Cons: a new `src/` top-level folder,
   csproj, publish-pipeline entry, and doc page for a single installer method and one
   options class; consumers needing both schemes reference two packages. Rejected: the
   `Microsoft.IdentityModel.*` dependency tree is small, Microsoft-maintained, and expected
   in an authentication package.
2. **Embedded token issuance (OpenIddict) in the library or sample** — Pros: zero external
   infrastructure. Cons: turns the library/sample into an identity provider — key
   management, consent, token endpoints — a responsibility this repo does not want.
   Rejected.
3. **External SaaS provider (Microsoft Entra ID) as the reference IdP** — Pros: zero
   hosting. Cons: integration tests would need a live tenant and secrets in CI; offline
   dev loop breaks. Rejected for tests; remains fully supported at runtime because the
   library binds only `Authority`/`Audience`.
4. **Chosen: same package + Keycloak reference provider** — standard, provider-agnostic,
   offline-testable via Testcontainers (family already pinned to `4.12.0`).

## Consequences

**Positive:**
- Standards-based auth (OIDC discovery, JWKS, rotation handled by the provider); PKCE
  flows work with zero library code.
- Multi-scheme support: API key for machine-to-machine, JWT for user-facing clients.

**Negative:**
- First external dependency of the package (`Microsoft.AspNetCore.Authentication.JwtBearer`
  → `Microsoft.IdentityModel.*` tree) lands on all existing consumers.
- Keycloak container adds ~1 GB image pull to the integration-test lane.

**Semver impact:** MINOR (additive public API).

## Related

- Implemented via [2026-07-06-jwt-bearer-authentication](../features/2026-07-06-jwt-bearer-authentication.md)
- [ADR-008](008-testing-framework-companions.md) — companion-package precedent weighed in alternative 1.
- [`docs/Auth.md`](../Auth.md) — module documentation (rewritten by the feature).
