# Authentication

`SolTechnology.Core.Auth` ships API-key authentication. JWT, OAuth, OIDC, PKCE, and Keycloak
integration are not currently part of the package. Consumer-facing usage is in
[`docs/Auth.md`](../Auth.md).

## One entry for authentication and authorization

`AddSolAuth(AuthConfiguration)` is the single registration entry. It installs the default `ApiKey`
scheme and adds a global MVC `AuthorizeFilter` that requires an authenticated user, through
`MvcOptions`, so the host does not thread a returned filter into `AddControllers`. The package and
the entry are named for authentication and authorization together: roles and policies, when they
ship, become options of `AddSolAuth` instead of a second package or a second entry.

`AddSolAuth` throws `ArgumentException` at registration when `ApiKey` is null, empty, or
whitespace, so a missing key stops the host before it serves a request. It also registers
`IOptions<AuthConfiguration>` with `ValidateOnStart`.

The package does not bind a configuration section and does not add middleware. Hosts call
`UseAuthentication()` before `UseAuthorization()`.

## Key comparison

`ApiKeyAuthenticationHandler` reads the raw key from `X-API-KEY`. It hashes the header value and the
configured key with SHA-256 and compares the two hashes with
`CryptographicOperations.FixedTimeEquals`. Equal-length hashes make the comparison time independent
of the key's length and content, so response timing does not reveal how much of a guessed key
matched (an ordinary string comparison stops at the first differing character).

A blank configured scheme key (`ApiKeyAuthenticationSchemeOptions.ApiKey`, a settable property)
never authenticates, even when the header is blank too; the handler rejects every request and logs
a Warning. `AddSolAuth` already refuses a blank key, so this guards scheme options configured by
other means.

Successful authentication creates a principal whose `ClaimTypes.Name` is `ApiKey`. A missing header
or a wrong key fails authentication with one Warning that never contains either key. Endpoints with
`[AllowAnonymous]` metadata return no authentication result and log nothing. The handler logs
through the `Logger` inherited from `AuthenticationHandler<TOptions>`.

## Scope

API keys intentionally serve a small machine-to-machine use case. They do not provide expiry,
rotation, delegated user identity, or standards-based login. Work that adds those capabilities
starts as a separate feature and updates this document only after the behavior ships.
