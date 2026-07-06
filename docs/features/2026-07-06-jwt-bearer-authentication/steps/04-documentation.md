---
spec: 2026-07-06-jwt-bearer-authentication
step: 04
status: to-do
---

# Step 04: Rewrite docs/Auth.md — API key, JWT Bearer, PKCE walkthrough

## Summary

The module doc is stale (it documents the retired `AddAuthenticationAndBuildFilter()` and
a `Key` config property; code has `AddSolAuthentication()` and `ApiKey`). Rewrite it to
match the code and document the new JWT scheme, including a Keycloak-based PKCE
walkthrough for user-facing clients. Docs-only PR.

## Affected components

- `docs/Auth.md` — EDIT — full rewrite per `ClaudeCodingGuide §18` structure

## Changes

- Fix drift: registration section shows `services.AddSolAuthentication(new
  AuthenticationConfiguration { ApiKey = ... })`; config section key is
  `Configuration:Authentication:ApiKey` — verify against the actual binding in
  `ModuleInstaller.cs` before writing.
- Verify and correct the header contract section (`X-Auth`, scheme prefix, base64) against
  `ApiKeyAuthenticationHandler` actual comparison semantics.
- NEW section **JWT Bearer**: `AddSolJwtAuthentication` registration snippet;
  `JwtAuthenticationConfiguration` keys (`Authority`, `Audience`, `RequireHttpsMetadata`);
  note that any OIDC-compliant provider works (Keycloak, Entra ID, Auth0).
- NEW section **Authorization Code + PKCE**: sequence — SPA/mobile client ↔ identity
  provider (PKCE happens here) → API validates the resulting JWT; Keycloak setup mirroring
  `tests/.../keycloak-realm.json` (`sol-spa` public client, `S256`); example token request;
  explicit statement that the API needs no PKCE-specific code.
- NEW section **Choosing a scheme**: API key = internal machine-to-machine; JWT = anything
  user-facing or cross-organization; both schemes side by side supported.
- Mermaid sequence diagram for the PKCE flow — route through the
  [diagram agent](../../../../.github/agents/diagram.agent.md) (CLAUDE.md §2), file under
  `docs/diagrams/`, linked from `Auth.md`.

## Acceptance criteria

- [ ] Every code snippet in `Auth.md` compiles against the step 01 API (manual check).
- [ ] Every relative link in `Auth.md` resolves on disk.
- [ ] Structure follows `ClaudeCodingGuide §18`; diagram authored by the diagram agent.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
