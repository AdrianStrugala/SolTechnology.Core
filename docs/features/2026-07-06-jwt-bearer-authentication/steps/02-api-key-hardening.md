---
spec: 2026-07-06-jwt-bearer-authentication
step: 02
status: to-do
---

# Step 02: API-key handler hardening

## Summary

Behaviour-preserving security fixes in the existing API-key handler: the key comparison is
vulnerable to timing attacks, and the logger is stored in a static field overwritten by
every instance construction. No public-API change.

## Affected components

- `src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs` — EDIT — comparison + logger field
- `tests/SolTechnology.Core.Authentication.Tests/` — EDIT — handler behaviour tests

## Changes

- `APIKeyAuthenticationHandler.cs:36` — replace `Options.ApiKey.Equals(apiKey.ToString())`
  with `CryptographicOperations.FixedTimeEquals` over `Encoding.UTF8.GetBytes` of both
  values (`using System.Security.Cryptography;`).
- `APIKeyAuthenticationHandler.cs:13` — `private static ILogger<ApiKeyAuthenticationHandler> _logger`
  → non-static `private readonly` field (or drop the extra parameter and use the base
  `AuthenticationHandler.Logger`); constructor updated accordingly.
- Tests (NUnit): `HandleAuthenticateAsync` matrix via `[TestCase]` — missing header ⇒
  `Fail`, wrong key ⇒ `Fail`, exact key ⇒ `Success`, `[AllowAnonymous]` endpoint ⇒
  `NoResult`.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green.
- [ ] `dotnet test tests/SolTechnology.Core.Authentication.Tests` green.
- [ ] No public/protected symbol renamed, moved, or removed (CLAUDE.md §1).

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
