### Overview

`SolTechnology.Core.Api` is the ASP.NET Core integration layer for the SolTechnology stack. It
ships:

- **RFC 7807 / RFC 9457 ProblemDetails error pipeline** (`application/problem+json`) — no
  custom envelope on the wire; success returns the raw DTO, failure returns `ProblemDetails`.
- **`Result<T>` ↔ HTTP conversion** in MVC — handlers stay HTTP-agnostic; `ResultConversionFilter`
  translates `Result<T>` into the wire format at the controller boundary.
- **`Error` subtype → status code mapping** — `NotFoundError` → 404, `ValidationError` → 400
  (with structured per-field `errors`), `ConflictError` → 409, `UnauthorizedError` → 401,
  `ForbiddenError` → 403; default 500 for the bare `Error` type. Replaceable through
  `IExceptionStatusCodeMapper`.
- **Header-based API versioning** (`X-API-VERSION`) with Swagger docs per version, deprecation
  badges, newest-first dropdown.
- **Correlation id propagation** — every error response carries
  `extensions.correlationId` matching the `X-Correlation-Id` response header and the log
  scope from `SolTechnology.Core.Logging`.
- **Log-level alignment with `Core.Logging`** — mapped 4xx → `Warning`, mapped 5xx → `Error`,
  unmapped exceptions → `Critical` + rethrow to host. PagerDuty / Sentry / App Insights
  smart-detection sees only real server faults, not validation noise.
- **One-call bootstrap** — `services.AddApiCore(...)` + `opts.AddApiCoreFilters()` +
  `app.UseSwaggerWithVersioning(...)` replaces ~30 lines of boilerplate.

Integration tests use a separate companion package, **`SolTechnology.Core.Api.Testing`**, so
that test-host dependencies do not leak into production assemblies.

### Registration

Reference the `SolTechnology.Core.Api` NuGet package. For integration tests, additionally
reference `SolTechnology.Core.Api.Testing`.

### Configuration

No appsettings binding is required. The optional `ApiExceptionOptions.IncludeExceptionDetails`
flag (default `false`) is set in code, typically to `IsDevelopment()`.

### Usage

#### 1. API Versioning

Configure header-based API versioning with automatic Swagger documentation:

```csharp
// In Program.cs, before builder.Services.AddControllers()
builder.Services.AddVersioning(
    defaultMajorVersion: 1,     // Default: 1
    defaultMinorVersion: 0,     // Default: 0
    apiTitle: "My API"          // For Swagger docs
);

// In the request pipeline:
app.UseSwaggerWithVersioning("My API");
```

> Tip: `AddApiCore` (see section 2) wraps both `AddVersioning` and the error pipeline in a
> single call.

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

#### 2. Errors and Result conversion (RFC 7807 ProblemDetails)

`SolTechnology.Core.Api` follows the .NET 7+ standard for HTTP errors: every failure response is
[RFC 7807 / RFC 9457 ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) served as
`application/problem+json`. There is **no custom envelope** on the wire — successful responses
carry the raw payload, failures carry `ProblemDetails`.

##### One-call bootstrap

```csharp
builder.Services.AddApiCore(
    o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment(),
    apiTitle: "DreamTravel API",
    defaultMajorVersion: 1);

builder.Services.AddControllers(opts => opts.AddApiCoreFilters());

var app = builder.Build();
app.UseSwaggerWithVersioning("DreamTravel API");
```

`AddApiCore` is a rollup that registers:
- `ExceptionFilter` + `ResultConversionFilter` (in DI; added to MVC by `AddApiCoreFilters`)
- `IExceptionStatusCodeMapper` (`DefaultExceptionStatusCodeMapper`, replaceable)
- `ApiExceptionOptions` bound through `IOptions<>`
- ASP.NET Core's `AddProblemDetails()` for non-MVC paths
- `Core.Logging` + `ICorrelationIdService` (used as `ProblemDetails.Extensions["correlationId"]`)
- API versioning (header `X-API-VERSION`) + per-version Swagger docs

For finer-grained control, the underlying `AddApiExceptionHandling` and `AddVersioning`
extensions remain available and compose freely.

##### Manual setup (when AddApiCore is too coarse)

```csharp
// Registers ExceptionFilter + ResultConversionFilter, binds ApiExceptionOptions, calls
// AddProblemDetails(), and ensures Core.Logging's ICorrelationIdService is available.
// IncludeExceptionDetails MUST stay false in Production — stack traces over the wire
// are an information disclosure (CWE-209).
builder.Services.AddApiExceptionHandling(o =>
    o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

builder.Services.AddControllers(o => o.AddApiCoreFilters());
```

##### Behaviour matrix

| Action returns / throws | HTTP outcome |
|---|---|
| `Result<T>.Success(data)` | `200 OK`, body = raw `data` (DTO is not wrapped in any envelope) |
| `Result.Success()` (non-generic) | `204 No Content`, no body |
| `Result<T>.Fail(error)` / `Result.Fail(error)` | `ProblemDetails` at `error.StatusCode` (defaults to `500`); `application/problem+json` |
| `BadRequest(error)` (action returned a bare `Error`) | `ProblemDetails` at the explicit status code or `error.StatusCode`, fallback `500` |
| Throws `FluentValidation.ValidationException` | `400` `ValidationProblemDetails` with strongly-typed `errors` per field |
| Throws any other mapped type | `ProblemDetails` at the mapped status |
| Throws an unmapped type | `LogCritical` + rethrow → host pipeline (`DeveloperExceptionPage` / `UseExceptionHandler`) |
| Client aborted the request (`OperationCanceledException` + `RequestAborted`) | Rethrown silently; `Core.Logging` logs the finish at `Warning` |

Application-layer code (CQRS handlers, services) continues to return `Result<T>` and never
references HTTP types. The boundary conversion lives in `ResultConversionFilter`.

##### Exception → status mapping (extension point)

The default mapper covers the most common BCL / FluentValidation exception types:

| Exception | Status |
|---|---|
| `FluentValidation.ValidationException` | `400` (`ValidationProblemDetails` with per-field errors) |
| `ArgumentException` (and `ArgumentNullException`) | `400` |
| `UnauthorizedAccessException` | `403` (per RFC 7235 — `403` means "identified, forbidden") |
| `KeyNotFoundException` | `404` |
| `NotImplementedException` | `501` |
| anything else | unmapped → `LogCritical` + rethrow to host |

Extend the map for project-specific exception types:

```csharp
public sealed class AppExceptionMapper : DefaultExceptionStatusCodeMapper
{
    public override bool TryMap(Exception exception, out int statusCode)
    {
        if (exception is OptimisticConcurrencyException) { statusCode = 409; return true; }
        if (exception is PaymentDeclinedException)        { statusCode = 402; return true; }
        return base.TryMap(exception, out statusCode);
    }
}

services.AddApiExceptionHandling();
services.Replace(ServiceDescriptor.Singleton<IExceptionStatusCodeMapper, AppExceptionMapper>());
```

The deliberate refusal to default unknown types to `500` keeps every unrecognized exception
visible in the operations log (`LogCritical` + `ExceptionType`) so the team can decide
whether it is a bug, a transient infrastructure error, or a missing mapping.

##### Failure semantics — `Error` subtypes

Application-layer failures use semantic subtypes from `SolTechnology.Core.CQRS.Errors`. The
API layer maps each subtype to a status code; other transports (gRPC, message bus) can map
to their own codes from the same source of truth.

| Error subtype | HTTP status | Body shape |
|---|---|---|
| `NotFoundError` | `404 Not Found` | `ProblemDetails` |
| `ConflictError` | `409 Conflict` | `ProblemDetails` |
| `ValidationError` | `400 Bad Request` | `ValidationProblemDetails` (with `errors`) |
| `UnauthorizedError` | `401 Unauthorized` | `ProblemDetails` |
| `ForbiddenError` | `403 Forbidden` | `ProblemDetails` |
| Bare `Error` (or any other subtype) | `500 Internal Server Error` | `ProblemDetails` |

```csharp
// Handler — pure domain semantics, no HTTP types.
public Task<Result<Trip>> Handle(GetTripQuery q, CancellationToken ct)
{
    var trip = _trips.Find(q.Id);
    return trip is null
        ? Result<Trip>.FailAsTask(new NotFoundError { Message = $"Trip {q.Id} not found." })
        : Result<Trip>.SuccessAsTask(trip);
}
```

Producing:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type":   "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title":  "Trip 42 not found.",
  "status": 404,
  "correlationId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

For validation:

```csharp
return Result<CreatedOrder>.Fail(new ValidationError
{
    Message = "Invalid order.",
    Errors = new Dictionary<string, string[]>
    {
        ["email"] = ["'Email' is not a valid email address."],
        ["age"]   = ["'Age' must be greater than 0."]
    }
});
```

##### CorrelationId

Every `ProblemDetails` carries `extensions.correlationId` matching the `X-Correlation-Id`
response header and the `CorrelationId` property on the request log scope (provided by
`SolTechnology.Core.Logging`). Clients quote the value in support tickets and it resolves
to the same logs in Seq / Application Insights.

##### Diagnostic detail

`ApiExceptionOptions.IncludeExceptionDetails = true` adds `extensions.exception` with the
exception type, message, and stack trace. **Off by default**; enable in Development only.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Object reference not set to an instance of an object.",
  "status": 500,
  "correlationId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "exception": {
    "type": "System.NullReferenceException",
    "message": "Object reference not set to an instance of an object.",
    "stackTrace": "   at MyApp.Foo() in /src/..."
  }
}
```

#### 3. API Testing

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
