---
spec: 2026-11-02-core-2-0-http
step: 05
status: done
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
- [x] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [x] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.
- [x] `grep -rn "namespace System" src` prints nothing.

## Open questions
- none (`C-10`, `C-11` confirmed by the user on 2026-11-02)

## Deviations

### The health check stays a singleton; no new registration parameter

**Original plan:** `UpstreamHttpHealthCheckExtensions.cs` — registration passes the client name; `C-11` says `AddSolUpstreamHttpHealthCheck<TCheck>` stops registering `TCheck` as a singleton that captures an `HttpClient`.

**Actual implementation:** The subclass passes the client name to the new base constructor `(IHttpClientFactory httpClientFactory, string httpClientName, string healthPath, …)`, and `CheckHealthAsync` calls `CreateClient(httpClientName)` per probe. `AddSolUpstreamHttpHealthCheck<TCheck>` keeps its signature and still registers `TCheck` as a singleton, because the result cache lives on the instance; the singleton now holds only the singleton `IHttpClientFactory`, so no `HttpClient` is captured. `C-11` lists no new parameter on the registration method, so none was added.

### DreamTravel UI call site

**Original plan:** DreamTravel usings change in `DreamTravel.GeolocationDataClients/**` and `tests/Component/**`.

**Actual implementation:** `DreamTravel.Ui/Services/GraphService.cs` also calls `CreateRequest` and gained `using SolTechnology.Core.HTTP;`. DreamTravel has no `BaseUpstreamServiceHealthCheck` subclass, so only usings changed (nine files). `docs/Clients.md` still shows the 1.x health-check constructor; step 08 rewrites it.
