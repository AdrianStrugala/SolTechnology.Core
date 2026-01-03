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

#### 2. Exception Handling Middleware

Add the exception handler middleware to automatically handle exceptions in your API:

```csharp
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

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
