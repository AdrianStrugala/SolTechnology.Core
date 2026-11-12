---
status: planning
created: 2026-07-06
approved:
plan-review: pending
premortem: pending
code-review: pending
completed:
---

# JWT Bearer Authentication and API-Key Hardening

> Historical delivery record. It may not describe the current system. Current authentication
> behavior lives in [`../architecture/authentication.md`](../architecture/authentication.md).

## Goal

Add provider-agnostic JWT Bearer validation to `SolTechnology.Core.Authentication`, prove it
against Keycloak in integration tests, document authorization code with PKCE, and harden the
existing API-key handler.

## Context

The package currently supports one static API key. That is sufficient for small machine-to-machine
integrations but provides no expiry, rotation, delegated user identity, or user-facing OAuth/OIDC
flow.

JWT validation belongs to the resource server. PKCE runs between the client and authorization
server; the library validates issuer, audience, lifetime, and signatures through discovery and
JWKS endpoints but does not issue tokens.

The planned implementation adds the package's first external dependency:
`Microsoft.AspNetCore.Authentication.JwtBearer@10.0.9`. Keycloak is the offline reference provider
for tests; runtime behavior remains provider-agnostic through `Authority` and `Audience`.

Alternatives considered:

1. A companion JWT package keeps API-key consumers dependency-free but adds package and publishing
  overhead for one installer and one options type.
2. Embedded token issuance turns the library into an identity provider and is out of scope.
3. A live SaaS identity provider prevents offline, secret-free integration tests.
4. The planned approach uses the existing package with Keycloak only as the test provider.

## Planned decision

Extend the existing Authentication package with a provider-neutral JWT Bearer scheme configured by
`Authority` and `Audience`. The installer delegates discovery, issuer, audience, lifetime, and
signature validation to `Microsoft.AspNetCore.Authentication.JwtBearer`. Any compatible OIDC
provider may be used at runtime.

Keycloak is only the integration-test and documentation reference because it permits a local,
secret-free OAuth/OIDC flow. Token issuance, authorization-server hosting, consent, and PKCE code
exchange remain outside the resource-server library.

API-key and JWT schemes are intended to coexist: static API keys for constrained machine-to-machine
integrations and short-lived bearer tokens for user-facing clients.

## Expected consequences

### Positive

- Consumers gain standards-based issuer discovery, JWKS rotation, token expiry, and per-user claims.
- Runtime behavior remains independent of a specific identity provider.
- Existing API-key authentication remains available.

### Negative

- Every consumer of the existing package receives its first external dependency and the associated
  Microsoft.IdentityModel graph.
- Keycloak integration tests require a large container image and a functioning container runtime.
- Adding validation does not provide token issuance or configure PKCE for clients.

## Semver expectation

The planned public API is additive and therefore a MINOR package change. This classification must
be revalidated against the actual diff before delivery.

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
- Out: token issuance through OpenIddict or IdentityServer.

## Implementation plan

1. Add the JWT Bearer scheme and validated configuration.
2. Harden the API-key handler.
3. Add Keycloak integration tests.
4. Rewrite authentication documentation.
5. Review delivery, update architecture documentation, and collapse temporary steps.

Step status lives in the table below.

| Step | Outcome | Status |
| --- | --- | --- |
| 01 | JWT Bearer scheme + installer | to-do |
| 02 | API-key handler hardening | to-do |
| 03 | Keycloak integration tests | to-do |
| 04 | Rewrite docs/Auth.md (drift + JWT + PKCE) | to-do |

## Acceptance criteria

- JWT validation is provider-agnostic and validates issuer, audience, lifetime, and signature.
- Existing API-key consumers remain supported.
- Keycloak integration tests cover valid and rejected tokens.
- `docs/Auth.md` describes only supported APIs.
- [`../architecture/authentication.md`](../architecture/authentication.md) is updated after the
  behavior ships.

## Premortem

Pending. Input prepared by the planner:

- **Modules touched:** `src/SolTechnology.Core.Authentication` (public API +
  `ModuleInstaller.cs`), `tests/SolTechnology.Core.Authentication.Tests`, `docs/Auth.md`.
- **API delta (additive, MINOR):**
  - NEW public class `JwtAuthenticationConfiguration` (`Authority`, `Audience`,
    `RequireHttpsMetadata`).
  - NEW public method `ModuleInstaller.AddSolJwtAuthentication(IServiceCollection,
    JwtAuthenticationConfiguration)`.
  - NEW dependency `Microsoft.AspNetCore.Authentication.JwtBearer@10.0.9` — first external
    `PackageReference` of this package; lands on all consumers
    (~1,862 downloads, [nuget-stats.json](../../nuget-stats.json)).
  - Internal: shared global `AuthorizeFilter` registration across both installer methods.
  - Behaviour change inside existing API: constant-time API-key comparison (step 02).
- **Checklists:** per-module review templates were removed from the repo in commit
  `ec7a1810` (2026-06-19, docs consolidation) — no module checklist applies; review
  against `ClaudeCodingGuide.md` only.
- **Suggested failure areas to probe:** default-scheme collision when both installers run;
  double `AuthorizeFilter`; `RequireHttpsMetadata=false` leaking into production guidance;
  JwtBearer package version drifting from the `net10.0` framework line; Keycloak container
  flakiness in CI.
- **Steps under review:** 01-04.

## Retrospective notes

- Update `docs/architecture/authentication.md` from verified code and tests; remove obsolete
  API-key-only claims only after JWT ships.
- Record residual tech debt as follow-ups (multi-key API-key identity is out of scope).

## Completion summary


## Deviations


## Follow-ups
