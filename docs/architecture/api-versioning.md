# API Versioning

`AddSolVersioning()` configures header-only API version selection through `X-API-VERSION`.
Versions are request metadata rather than part of resource URLs, so application routes remain
stable across versions.

The library default is `1.0`. Requests without a version use the configured default, and
responses report supported and deprecated versions. Applications may override the default;
DreamTravel currently uses `2.0`.

`AddSolApiCore()` includes versioning. `UseSolSwaggerWithVersioning()` creates one Swagger
endpoint per discovered version, ordered newest first, and labels deprecated versions. API
Explorer uses group format `'v'V`, grouping documents by major version.

Controllers may be separate per version or share a route with `[MapToApiVersion]`. Do not embed
versions in URLs such as `/api/v2/...` unless the architecture itself is deliberately changed.

Header versioning keeps resource identity stable and centralizes defaults, reporting, discovery,
and deprecation behavior. It requires clients and gateways to preserve `X-API-VERSION`.
