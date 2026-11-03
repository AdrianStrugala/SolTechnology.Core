---
spec: 2026-11-02-core-2-0-api-auth
step: 04
status: to-do
---

# Step 04: Auth — constant-time API-key comparison and per-instance logger

## Summary
`ApiKeyAuthenticationHandler` compares the key with `string.Equals`, which leaks timing (`S5`,
`src/SolTechnology.Core.Auth/APIKeyAuthenticationHandler.cs:36`; the path is `src/SolTechnology.Core.Authentication/` on `4097d47f`). It also stores the
logger in a mutable `private static` field that every request's constructor overwrites
(`http-api#4`, `:13`). The same work is planned in
[`2026-07-06-jwt-bearer-authentication` step 02](../../2026-07-06-jwt-bearer-authentication/steps/02-api-key-hardening.md).
Q5 (user, 2026-11-02): this feature owns the fix; the JWT feature stays JWT-only, and its files are
not edited here.

Former core step 07; moved here by the per-library regroup (user, 2026-11-02). Renumbered from 03 on 2026-11-03 (new step 01, `C-28`).

## Affected components
- `src/SolTechnology.Core.Auth/APIKeyAuthenticationHandler.cs` — EDIT — comparison and logger field
- `docs/architecture/authentication.md` — not edited here (the retrospective replaces "exact string equality")
- `tests/SolTechnology.Core.Auth.Tests/` — EDIT — handler behaviour tests

## Changes
- Compare `SHA256.HashData(Encoding.UTF8.GetBytes(...))` of both values with `CryptographicOperations.FixedTimeEquals`.
- `private static ILogger<ApiKeyAuthenticationHandler> _logger` → `private readonly` instance field. Constructor signature unchanged; dropping the parameter is `C-21` in step 06.
- Tests (`[TestCase]` matrix on `HandleAuthenticateAsync`): missing header → `Fail`; wrong key → `Fail`; key with matching prefix and different length → `Fail`; exact key → `Success`.
- Premortem RM-19 (core step 00, 2026-11-02, scenario 19): a null, empty, or whitespace configured `Options.ApiKey` never authenticates. The handler returns `Fail` before comparing, logs a Warning without any key material, and the matrix gains `configured key "" + header "" → Fail`.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Auth.Tests` is green with the 4 cases (functional regression; a timing difference is not unit-testable).
- [ ] RM-19: the empty-configured-key case returns `Fail` and is green.
- [ ] Source check for `S5`: `grep -n "CryptographicOperations.FixedTimeEquals" src/SolTechnology.Core.Auth/APIKeyAuthenticationHandler.cs` matches, and `grep -rnE "ApiKey\.Equals|== *apiKey" src/SolTechnology.Core.Auth` prints nothing.
- [ ] `grep -rn "static ILogger" src/SolTechnology.Core.Auth` prints nothing.
- [ ] No public or protected signature changed.

## Open questions
- none (Q5 resolved by the user on 2026-11-02: this feature owns `S5`/`http-api#4`)

## Deviations
