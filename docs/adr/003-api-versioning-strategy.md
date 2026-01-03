# ADR-003: API Versioning Strategy for DreamTravel

> **Status:** Accepted (Amended)
> **Decision Date:** 2026-01-02
> **Amendment Date:** 2026-01-02 (Changed to Header Versioning)
> **Decision Maker:** Development Team
> **Stakeholders:** DreamTravel users, API consumers, frontend team

---

## Context

### Problem Statement

DreamTravel API has inconsistent versioning, leading to the following problems:

1. **Inconsistent URLs**:
   - v1 endpoints use URLs without version prefix (e.g., `api/CalculateBestPath`)
   - v2 endpoints use URLs with prefix (e.g., `api/v2/CalculateBestPath`)
   - Violates RESTful conventions - no clear pattern

2. **Missing Swagger Documentation**:
   - Swagger UI shows only v1 endpoints
   - v2 endpoints are invisible in documentation
   - Developers cannot discover or test v2 API

3. **Lack of Centralized Version Management**:
   - Manual route constants definition in each controller
   - No formal deprecation policy
   - Difficult scalability for v3+

### Goals

1. **Unify versioning** for all API versions
2. **Add visibility for v2** endpoints in Swagger UI
3. **Introduce centralized version management** with dedicated library
4. **Prepare infrastructure** for future versions (v3+)
5. **Ensure smooth deprecation** of old versions with grace period
6. **Automatic deprecation warnings** in response headers

---

## Decision

We implement **Header Versioning with Asp.Versioning.Mvc**:

### Key Decision Elements

1. **Versioning Strategy**: Header Versioning (header `X-API-VERSION`)
2. **Library**: `Asp.Versioning.Mvc` v8.1.0 + `Asp.Versioning.Mvc.ApiExplorer` v8.1.0
3. **Default Version**: v2.0 (v1.0 deprecated)
4. **Header Name**: `X-API-VERSION` (values: `1.0`, `2.0`)
5. **All Controllers**: `[ApiVersion]` attributes without version in URL
6. **Swagger**: Automatic grouping by version with dropdown selector
7. **Centralized Configuration**: Extracted to `SolTechnology.Core.API` package via `AddVersioning()` extension method

### Implementation Details

#### 1. API Versioning Configuration

The versioning configuration is centralized in `SolTechnology.Core.API` package and exposed via `AddVersioning()` extension method:

```csharp
// Program.cs (before builder.Services.AddControllers())
builder.Services.AddVersioning(apiTitle: "DreamTravel API");
```

**Behind the scenes** (in `SolTechnology.Core.API.Versioning.APIVersioningInstaller`):
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;  // No header = v2
    options.ReportApiVersions = true;  // Adds api-supported-versions header
    options.ApiVersionReader = new HeaderApiVersionReader("X-API-VERSION");
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";  // Major version only
    options.SubstituteApiVersionInUrl = true;
});
```

#### 2. Controller Configuration

Controllers are consolidated to support both v1 and v2 using `[MapToApiVersion]` on action methods:

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/[controller]")]
public class CalculateBestPathController : ControllerBase
{
    /// <summary>
    /// V1 - DEPRECATED: Returns only list of paths
    /// </summary>
    [HttpPost]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CalculateBestPathV1([FromBody] CalculateBestPathQuery query)
    {
        var result = await handler.Handle(query, CancellationToken.None);
        return Ok(result.Data.BestPaths);  // Direct data
    }

    /// <summary>
    /// V2 - Returns full Result wrapper
    /// </summary>
    [HttpPost]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> CalculateBestPathV2([FromBody] CalculateBestPathQuery query)
    {
        return Ok(await handler.Handle(query, CancellationToken.None));  // Result wrapper
    }
}
```

#### 3. Swagger Configuration

Swagger documentation is automatically configured by `AddVersioning()` using `ConfigureSwaggerOptions`:

```csharp
// Swagger UI configuration (Program.cs after app.Build())
var apiVersionDescriptionProvider = app.Services
    .GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwaggerUI(c =>
{
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
    {
        c.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"DreamTravel API {description.GroupName.ToUpperInvariant()}" +
            $"{(description.IsDeprecated ? " (Deprecated)" : "")}");
    }
});
```

**Note**: The `ConfigureSwaggerOptions` class in `SolTechnology.Core.API.Versioning` automatically generates SwaggerDoc for each API version with appropriate titles and deprecation warnings.

#### 4. Client Usage

```csharp
// v1 (deprecated)
httpClient.DefaultRequestHeaders.Add("X-API-VERSION", "1.0");
var response = await httpClient.PostAsync("api/CalculateBestPath", content);

// v2 (current)
httpClient.DefaultRequestHeaders.Add("X-API-VERSION", "2.0");
var response = await httpClient.PostAsync("api/CalculateBestPath", content);

// No header = default v2
var response = await httpClient.PostAsync("api/CalculateBestPath", content);
```

---

## Rationale

### Versioning Strategy Comparison

#### 1. URL Path Versioning (`api/v1/resource`)
- ✅ Explicit and readable - version visible in URL
- ✅ Widely adopted (Stripe, GitHub, Twitter)
- ✅ Browser-friendly
- ❌ URL proliferation - multiple URLs for the same resource
- ❌ **Routing issues in ASP.NET Core** - conflicts with `{version:apiVersion}` token
- ❌ Breaking changes in URLs

#### 2. Query String Versioning (`?api-version=1`)
- ✅ Clean base URLs
- ❌ Not RESTful - query params for filtering, not identification
- ❌ Cache and routing complexity

#### 3. Header Versioning (`X-API-VERSION: 1.0`) ⭐ **SELECTED**
- ✅ **Clean URLs** - no proliferation
- ✅ **Separation of concerns** - version is metadata, not part of the resource
- ✅ **Seamless integration with Microsoft.AspNetCore.Mvc.Versioning**
- ✅ **Swagger UI automatically adds header** when selecting version
- ✅ **Automatic deprecation headers** (`api-deprecated-versions`)
- ✅ **One URL per resource** - clean architecture
- ❌ Requires header in requests (mitigation: `AssumeDefaultVersionWhenUnspecified`)
- ❌ Harder to test in browser (mitigation: Swagger UI)

#### 4. Media Type Versioning (`Accept: application/vnd.dreamtravel.v1+json`)
- ✅ True REST with content negotiation
- ❌ Complex implementation
- ❌ Rarely used in practice

### Why Header Versioning?

**Main reasons**:
1. **Technical necessity**: URL Path Versioning with `{version:apiVersion}` tokens caused routing conflicts - endpoints were not registered
2. **Clean URLs**: One URL per logical resource (`api/CalculateBestPath`)
3. **Library works out-of-the-box**: No fighting with ASP.NET Core routing
4. **Swagger integration**: Seamless - dropdown automatically sets header
5. **Default version**: Clients without header get v2 (latest)
6. **Service-to-service**: Header easy to add in HttpClient

---

## Consequences

### Positive

✅ **Clean URLs**
- One URL per resource: `api/CalculateBestPath`
- No route proliferation
- RESTful design

✅ **Automatic deprecation warnings**
- Response header: `api-deprecated-versions: 1.0`
- Response header: `api-supported-versions: 1.0, 2.0`
- Clients know which versions are deprecated

✅ **Full visibility in Swagger**
- Dropdown with v1 and v2
- Each version has separate SwaggerDoc
- Swagger UI automatically adds `X-API-VERSION` header

✅ **Scalability**
- Adding v3 is just `[ApiVersion("3.0")]` on controllers
- Centralized configuration in `Program.cs`

✅ **Backward compatibility with grace period**
- v1 marked as deprecated but still works
- 12-month migration window for clients
- Default v2 for new clients

### Negative

⚠️ **Breaking changes for v2 clients**
- Clients using `api/v2/CalculateBestPath` must:
  1. Change URL to `api/CalculateBestPath`
  2. Add header `X-API-VERSION: 2.0`
- **Mitigation**:
  - Update Blazor UI
  - Update component tests
  - Update documentation

⚠️ **Header required for explicit versioning**
- Clients must remember to add header for v1
- **Mitigation**: `AssumeDefaultVersionWhenUnspecified = true` (no header = v2)

⚠️ **Harder to test in browser**
- Cannot just type URL in address bar and select version
- **Mitigation**: Use Swagger UI for testing

### Breaking Changes

**v2 Route Changes**:
- `api/v2/CalculateBestPath` → `api/CalculateBestPath` + `X-API-VERSION: 2.0`
- `api/v2/FindCityByName` → `api/FindCityByName` + `X-API-VERSION: 2.0`
- `api/v2/FindCityByCoordinates` → `api/FindCityByCoordinates` + `X-API-VERSION: 2.0`
- `api/v2/statistics` → `api/statistics` + `X-API-VERSION: 2.0`

**v1 Route Changes**: None (URLs remain unchanged)
- `api/CalculateBestPath` works with `X-API-VERSION: 1.0` (or defaults to v2)

**Mitigation strategy**:
1. **AssumeDefaultVersionWhenUnspecified = true**: No header = v2
2. **Deprecated attribute** on v1 controllers
3. **api-deprecated-versions** header in response
4. **Sunset header**: `Sunset: Sat, 31 Dec 2026 23:59:59 GMT` (for v1)
5. **Release notes** documenting the strategy change
6. **Blazor UI update**: Add `X-API-VERSION: 2.0` header
7. **Component tests update**: Add header to requests

---

## Implementation

### Files to Modify

#### 1. NuGet Packages ✅
**File**: `DreamTravel.Api/DreamTravel.Api.csproj`
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.1.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer" Version="5.1.0" />
```

#### 2. Program.cs Configuration
**File**: `DreamTravel.Api/Program.cs`

API Versioning (before `AddControllers()`):
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("X-API-VERSION");
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
});
```

Swagger configuration:
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DreamTravel API", Version = "1.0", Description = "Deprecated" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "DreamTravel API", Version = "2.0", Description = "Current" });
    // ... security
});
```

After `app.Build()`:
```csharp
var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerUI(c => {
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
    {
        c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
            $"DreamTravel API {description.GroupName.ToUpperInvariant()}{(description.IsDeprecated ? " (Deprecated)" : "")}");
    }
});
```

#### 3. v1 Controllers (4 files)
**Files**:
- `Controllers/Trips/v1/CalculateBestPathController.cs`
- `Controllers/Trips/v1/FindLocationOfCityController.cs`
- `Controllers/Trips/v1/FindNameOfCityController.cs`
- `Controllers/Trips/v1/LimitCostOfPathsController.cs`

**Change routes**: `api/v1/...` → `api/...`

Example:
```csharp
[ApiVersion("1.0", Deprecated = true)]
[Route("api/CalculateBestPath")]
[ApiController]
public class CalculateBestPathController : Controller
{
    public const string Route = "api/CalculateBestPath";
    // ...
}
```

#### 4. v2 Controllers (4 files)
**Files**:
- `Controllers/Trips/v2/CalculateBestPathController.cs`
- `Controllers/Trips/v2/FindCityByCoordinatesController.cs`
- `Controllers/Trips/v2/FindCityByNameController.cs`
- `Controllers/Trips/v2/StatisticsController.cs`

**Change routes**: `api/v2/...` → `api/...`

Example:
```csharp
[ApiVersion("2.0")]
[Route("api/CalculateBestPath")]
[ApiController]
public class CalculateBestPathController : ControllerBase
{
    public const string Route = "api/CalculateBestPath";
    // ...
}
```

#### 5. Component Tests
**File**: `tests/Component/Trips/CalculateBestPathFeatureTest.cs`

Add header to all v2 requests:
```csharp
var apiResponse = await _apiClient
    .CreateRequest("/api/CalculateBestPath")
    .WithHeader("X-API-KEY", "<SECRET>")
    .WithHeader("X-API-VERSION", "2.0")  // NEW
    .WithBody(new { Cities = cities })
    .PostAsync<Result<CalculateBestPathResult>>();
```

#### 6. Contract Test
**File**: `tests/Component/ContractTests.ContractTest_reviewChangesToTheApi.verified.txt`

Accept the new snapshot after running tests.

---

## Monitoring & Success Metrics

### Adoption Metrics (6 months)

| Metric | Target | Measurement |
|--------|--------|-------------|
| v2 header usage | >90% | Telemetry - `X-API-VERSION: 2.0` |
| v1 usage drop | <10% | Request analytics |
| Default version (no header) | >50% | Requests without `X-API-VERSION` |
| Breaking change complaints | 0 | Support tickets |

### Technical Metrics

| Metric | Target | Status |
|--------|--------|--------|
| Build success | 100% | CI/CD |
| Test pass rate | 100% | Automated tests |
| Performance regression | <1% | Benchmarks |
| Security vulnerabilities | 0 | Dependency scan |

---

## Future Enhancements

📅 **Planned for v3** (when needed):
- Consider adding Query String fallback: `?api-version=3.0`
- Auto-migration tools for clients
- OpenTelemetry integration to track version usage
- API Gateway integration (Azure APIM) for enterprise clients

---

## References

- Microsoft Docs: [API Versioning in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/versioning)
- GitHub: [Microsoft.AspNetCore.Mvc.Versioning](https://github.com/dotnet/aspnet-api-versioning)
- RFC 8594: [Sunset HTTP Header](https://www.rfc-editor.org/rfc/rfc8594.html)
- Roy Fielding: [REST APIs must be hypertext-driven](https://roy.gbiv.com/untangled/2008/rest-apis-must-be-hypertext-driven)

---

## Amendment History

### 2026-01-02: Changed from URL Path to Header Versioning

**Reason**: Technical issues with `Microsoft.AspNetCore.Mvc.Versioning` routing when using URL path tokens (`{version:apiVersion}`). Controllers were not being registered, causing 404 errors on all endpoints.

**Root cause**: Library's `UrlSegmentApiVersionReader` conflicts with hardcoded `/v1/`, `/v2/` prefixes in routes.

**Solution**: Switch to `HeaderApiVersionReader("X-API-VERSION")` which:
- Works seamlessly with library
- Provides cleaner URL structure
- Maintains all benefits of centralized versioning
- Swagger UI automatically handles header injection

**Impact**:
- v2 clients must add `X-API-VERSION: 2.0` header
- v1 clients can add `X-API-VERSION: 1.0` (or rely on default v2)
- URLs remain clean without version prefix
- Component tests require header updates
- Blazor UI requires header injection

---

**Document Version**: 2.0 (Amended)
**Last Updated**: 2026-01-02
**Status**: ✅ **ACCEPTED - HEADER VERSIONING**
