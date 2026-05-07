### Overview

The SolTechnology.Core.Api library provides API utilities and filters for ASP.NET Core applications. It includes exception handling middleware, response envelope filters, and testing utilities for API integration tests.

### Registration

For installing the library, reference **SolTechnology.Core.Api** nuget package.

### Configuration

No configuration is needed.

### Usage

#### 1. API Versioning

Configure header-based API versioning with automatic Swagger documentation:

```csharp
// In Program.cs, before builder.Services.AddControllers()
builder.Services.AddVersioning(
    defaultMajorVersion: 2,     // Default: 2
    defaultMinorVersion: 0,     // Default: 0
    apiTitle: "My API"          // For Swagger docs
);

// Configure Swagger UI (after app.Build())
var apiVersionDescriptionProvider = app.Services
    .GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwaggerUI(c =>
{
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
    {
        c.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"My API {description.GroupName.ToUpperInvariant()}" +
            $"{(description.IsDeprecated ? " (Deprecated)" : "")}");
    }
});
```

**Controller Configuration**:

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    /// <summary>
    /// V1 - DEPRECATED
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult GetV1()
    {
        return Ok("Version 1");
    }

    /// <summary>
    /// V2 - Current
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2.0")]
    public IActionResult GetV2()
    {
        return Ok("Version 2");
    }
}
```

**Client Usage**:

```csharp
// Request specific version
httpClient.DefaultRequestHeaders.Add("X-API-VERSION", "2.0");

// No header = default version (configured in AddVersioning)
var response = await httpClient.GetAsync("api/mycontroller");
```

**Features**:
- Header-based versioning using `X-API-VERSION` header
- Automatic Swagger documentation for each version
- Deprecation warnings in response headers (`api-deprecated-versions`, `api-supported-versions`)
- Default version for clients without header (`AssumeDefaultVersionWhenUnspecified = true`)
- Clean URLs without version prefix

#### 2. Exception Handling

Register the exception filter (and its options) once in DI:

```csharp
// Map known exceptions to a Result envelope; rethrow unmapped ones to the host pipeline.
// Enable IncludeExceptionDetails ONLY in Development — stack traces in Production responses
// are an information disclosure (CWE-209).
builder.Services.AddApiExceptionHandling(o =>
    o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

builder.Services.AddControllers(o => o.Filters.Add<ExceptionFilter>());
```

For non-MVC requests (auth, routing, other middleware), wire the safety-net middleware:

```csharp
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

Behavior:

| Exception | Outcome |
|---|---|
| Mapped (e.g. `FluentValidation.ValidationException`) | Wrapped in `Result` + appropriate status code, logged at `Error` |
| Client-abort (`OperationCanceledException` + `RequestAborted`) | Rethrown silently — `Core.Logging` finishes the request log at `Warning` |
| Unmapped | Logged at `Critical` with `ExceptionType`, then rethrown to the host pipeline (DeveloperExceptionPage in Development, generic 500 in Production, or your `UseExceptionHandler`) |

`ApiExceptionOptions.IncludeExceptionDetails = true` augments `Error.Description` with the exception type and stack trace. **Off by default**; enable in Development only.

Every error response carries `Error.CorrelationId` matching the `X-Correlation-Id` response header and the `CorrelationId` property on the request log scope (provided by `SolTechnology.Core.Logging`). Clients can quote it in support tickets and the value resolves to the same logs in Seq / Application Insights.

#### 3. Response Envelope Filter

Add the response envelope filter to wrap your API responses in a consistent format:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ResponseEnvelopeFilter>();
});
```

#### 4. API Testing

Use the ApiFixture class for integration testing your API:

```csharp
public class MyApiTests
{
    private readonly ApiFixture _fixture;

    public MyApiTests()
    {
        _fixture = new ApiFixture();
    }

    [Test]
    public async Task TestMyEndpoint()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/myendpoint");
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}
```
