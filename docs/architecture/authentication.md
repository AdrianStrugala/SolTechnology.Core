# Authentication

The shipped `SolTechnology.Core.Authentication` architecture is API-key authentication. JWT,
OAuth, OIDC, PKCE, and Keycloak integration are not currently part of the package.

`AddSolAuthentication(AuthenticationConfiguration)` requires a non-empty `ApiKey` and fails
during registration when it is absent. It installs the default `ApiKey` scheme and a global
`AuthorizeFilter` that requires an authenticated user.

`APIKeyAuthenticationHandler` reads the raw key from `X-API-KEY` and compares it using exact
string equality. Successful authentication creates a principal whose `ClaimTypes.Name` is
`ApiKey`. Missing or invalid headers fail authentication; `[AllowAnonymous]` endpoints return no
authentication result.

The package does not bind a configuration section and does not add middleware. Hosts must call
`UseAuthentication()` before `UseAuthorization()`.

API keys intentionally serve a small machine-to-machine use case. They do not provide expiry,
rotation, delegated user identity, or standards-based login. Work that adds those capabilities
starts as a separate feature and updates this document only after the behavior ships.
