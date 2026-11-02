---
spec: 2026-11-02-core-2-0-http
step: 05
status: to-do
---

# Step 05: HTTP — own namespace for `HttpClientExtensions`; factory-based upstream health check

## Summary
Two HTTP API defects:

- `HttpClientExtensions` is declared in the BCL namespace `System.Net.Http`, which risks
  collisions and confuses API diffing (`http-api#20`, `HttpClientExtensions.cs:4`).
- `AddSolUpstreamHttpHealthCheck<TCheck>` registers the check as a singleton that captures an
  `HttpClient` forever, so DNS never rotates (`http-api#9`,
  `UpstreamHttpHealthCheckExtensions.cs:40`). The base check moves to `IHttpClientFactory` plus a
  client name.

This step implements `C-10` and `C-11`.

Former core step 45; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.HTTP/HttpClientExtensions.cs` — EDIT — `namespace SolTechnology.Core.HTTP;`
- `src/SolTechnology.Core.HTTP/HealthChecks/BaseUpstreamServiceHealthCheck.cs` — EDIT — `:40` primary constructor takes `IHttpClientFactory httpClientFactory, string httpClientName`; `CreateClient` per probe
- `src/SolTechnology.Core.HTTP/HealthChecks/UpstreamHttpHealthCheckExtensions.cs` — EDIT — registration passes the client name
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.GeolocationDataClients/**` — EDIT — usings and any health-check subclasses
- `sample-tale-code-apps/DreamTravel/tests/Component/**` — EDIT — `using SolTechnology.Core.HTTP;` where `.CreateRequest(` relied on the `System.Net.Http` namespace (`grep -rln "CreateRequest(" sample-tale-code-apps/DreamTravel/tests`)
- `tests/SolTechnology.Core.HTTP.Tests/BaseUpstreamServiceHealthCheckTests.cs` — EDIT — `CheckHealth_CreatesClientPerProbe`

## Changes
- Consumers that already import `SolTechnology.Core.HTTP` need no new using.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.
- [ ] `grep -rn "namespace System" src` prints nothing.

## Open questions
- none (`C-10`, `C-11` confirmed by the user on 2026-11-02)

## Deviations
