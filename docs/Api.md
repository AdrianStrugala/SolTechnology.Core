## SolTechnology.Core.Api

ASP.NET Core integration layer for the SolTechnology stack. Bring it in and your API speaks
**RFC 7807 ProblemDetails**, returns `Result<T>` from handlers without ever touching HTTP types,
versions itself by header, and propagates a correlation id from log to wire — out of the box.

### Features

- **Standard error pipeline** — every failure is RFC 7807 / RFC 9457 `application/problem+json`.
  No custom envelope, no per-controller `try/catch`, no string error bodies.
- **`Result<T>` ↔ HTTP at the boundary** — controllers and handlers stay HTTP-agnostic;
  the filter converts `Result<T>` to raw DTO on success and to `ProblemDetails` on failure.
- **Semantic `Error` → status mapping** — `NotFoundError` → 404, `ValidationError` → 400 (with
  per-field `errors`), `ConflictError` → 409, `UnauthorizedError` → 401, `ForbiddenError` → 403.
  Replace or extend via `IExceptionStatusCodeMapper`.
- **Header-based API versioning** with per-version Swagger documents, deprecation badges and
  newest-first dropdown — one call wires it.
- **Correlation id everywhere** — `extensions.correlationId` on every error, matching the
  `X-Correlation-Id` response header and the request log scope. One token, one search in Seq /
  App Insights.
- **Log-level alignment** — mapped 4xx → `Warning`, mapped 5xx → `Error`, unmapped exceptions →
  `Critical` + rethrow. Smart-detection in Sentry / App Insights sees real faults, not noise.
- **One-call bootstrap** — `AddApiCore` + `AddApiCoreFilters` + `UseSwaggerWithVersioning`
  replaces ~30 lines of plumbing.
- **Testing companion** — `SolTechnology.Core.Api.Testing` ships `ApiFixture` so test-host
  dependencies never leak into production assemblies.

### Registration

Reference the NuGet package and call `AddApiCore` once:

```csharp
builder.Services.AddApiCore(
    o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment(),
    apiTitle: "DreamTravel API",
    defaultMajorVersion: 1);

builder.Services.AddControllers(opts => opts.AddApiCoreFilters());

var app = builder.Build();
app.UseSwaggerWithVersioning("DreamTravel API");
```

If you want finer control, compose the lower-level extensions yourself:

```csharp
builder.Services.AddApiExceptionHandling(o =>
    o.IncludeExceptionDetails = builder.Environment.IsDevelopment());
builder.Services.AddVersioning(defaultMajorVersion: 1, apiTitle: "DreamTravel API");
builder.Services.AddControllers(o => o.AddApiCoreFilters());
```

For integration tests, additionally reference `SolTechnology.Core.Api.Testing`.

### Configuration

No `appsettings.json` binding is required. The only knob is `ApiExceptionOptions`:

| Option | Default | Purpose |
|---|---|---|
| `IncludeExceptionDetails` | `false` | Adds `extensions.exception` (type, message, stack) to `ProblemDetails`. **Keep `false` in Production** — stack traces over the wire are CWE-209. |

```csharp
builder.Services.Configure<ApiExceptionOptions>(o =>
    o.IncludeExceptionDetails = builder.Environment.IsDevelopment());
```

### Usage

#### Returning `Result<T>` from controllers

Handlers and controllers return `Result<T>` from `SolTechnology.Core.CQRS`. The filter does the
wire conversion:

```csharp
[ApiController]
[Route("api/[controller]")]
public class TripsController(IQueryHandler<GetTripQuery, Trip> handler) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<Result<Trip>> Get(int id, CancellationToken ct)
        => handler.Handle(new GetTripQuery { Id = id }, ct);
}
```

Behaviour matrix:

| Action returns / throws | HTTP outcome |
|---|---|
| `Result<T>.Success(data)` | `200 OK`, body = raw DTO |
| `Result.Success()` | `204 No Content` |
| `Result<T>.Fail(error)` | `ProblemDetails` at `error.StatusCode` (default `500`) |
| Throws `FluentValidation.ValidationException` | `400` `ValidationProblemDetails` |
| Throws a mapped exception | `ProblemDetails` at the mapped status |
| Throws an unmapped exception | `LogCritical` + rethrow to host |
| Client aborts (`OperationCanceledException` + `RequestAborted`) | Rethrown silently; `Core.Logging` logs finish at `Warning` |

#### Failure semantics — `Error` subtypes

```csharp
public Task<Result<Trip>> Handle(GetTripQuery q, CancellationToken ct)
{
    var trip = _trips.Find(q.Id);
    return trip is null
        ? Result<Trip>.FailAsTask(new NotFoundError { Message = $"Trip {q.Id} not found." })
        : Result<Trip>.SuccessAsTask(trip);
}
```

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type":          "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title":         "Trip 42 not found.",
  "status":        404,
  "correlationId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

| `Error` subtype | HTTP | Body |
|---|---|---|
| `NotFoundError` | 404 | `ProblemDetails` |
| `ConflictError` | 409 | `ProblemDetails` |
| `ValidationError` | 400 | `ValidationProblemDetails` with `errors` |
| `UnauthorizedError` | 401 | `ProblemDetails` |
| `ForbiddenError` | 403 | `ProblemDetails` |
| bare `Error` / unknown subtype | 500 | `ProblemDetails` |

Validation errors carry the per-field dictionary:

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

#### Mapping project-specific exceptions

Extend `DefaultExceptionStatusCodeMapper` and replace the DI registration:

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

services.Replace(ServiceDescriptor.Singleton<IExceptionStatusCodeMapper, AppExceptionMapper>());
```

Default mapping covers common BCL / FluentValidation types:

| Exception | Status |
|---|---|
| `FluentValidation.ValidationException` | 400 (`ValidationProblemDetails`) |
| `ArgumentException` / `ArgumentNullException` | 400 |
| `UnauthorizedAccessException` | 403 |
| `KeyNotFoundException` | 404 |
| `NotImplementedException` | 501 |
| anything else | `LogCritical` + rethrow |

#### API versioning

Header-based (`X-API-VERSION`); no version prefix in the URL.

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    [HttpGet, MapToApiVersion("1.0")]
    public IActionResult GetV1() => Ok("v1");

    [HttpGet, MapToApiVersion("2.0")]
    public IActionResult GetV2() => Ok("v2");
}
```

Client picks a version (or omits the header to take the default):

```csharp
httpClient.DefaultRequestHeaders.Add("X-API-VERSION", "2.0");
var response = await httpClient.GetAsync("api/trips");
```

`UseSwaggerWithVersioning(...)` exposes one Swagger document per version, newest first, with
deprecation badges on the dropdown.

#### CorrelationId

Every `ProblemDetails` carries `extensions.correlationId`, the same value that:
- is echoed on the response as `X-Correlation-Id`,
- is pushed into the request log scope by `SolTechnology.Core.Logging`,
- the client can quote in a support ticket to find the request in Seq / App Insights.

#### Diagnostic detail (Development only)

`ApiExceptionOptions.IncludeExceptionDetails = true` adds `extensions.exception`:

```json
{
  "type":          "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title":         "Object reference not set to an instance of an object.",
  "status":        500,
  "correlationId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "exception": {
    "type":       "System.NullReferenceException",
    "message":    "Object reference not set to an instance of an object.",
    "stackTrace": "   at MyApp.Foo() in /src/..."
  }
}
```

Off by default. Never enable in Production.

### Testing

Reference `SolTechnology.Core.API.Testing` from your test project and use `ApiFixture`:

```csharp
public class TripsApiTests
{
    private readonly ApiFixture _fixture = new();

    [Test]
    public async Task Get_ReturnsTrip()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/trips/42");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

`ApiFixture` wraps `WebApplicationFactory<Program>` with the conventions used across the
SolTechnology stack — service overrides, correlation propagation, JSON options — so component
tests stay short and focused.

### Conventions

- **Controllers are thin.** Action body ≤ 3 lines: invoke handler, return `Result<T>`. No
  `try/catch` (the filter handles it). No manual error serialization.
- **No custom envelope on the wire.** Success = raw DTO. Failure = `ProblemDetails`. If you
  feel the urge to wrap success in `{ data, error, success }`, you're fighting the framework.
- **`Result<T>` flows through MVC.** Handlers and domain code never see `IActionResult`,
  `HttpStatusCode`, or `ProblemDetails`.
- **Errors carry semantics, not status codes.** Application code returns `NotFoundError` /
  `ConflictError` / `ValidationError`; the API layer (and only the API layer) decides what
  that means on HTTP.
- **One `ApiVersion` per controller class** when versions diverge in shape; share a class with
  `[MapToApiVersion]` per action when they share most of the surface.
- **Always document with XML `<summary>` + `[ProducesResponseType]`** for every status the
  action can return — Swagger consumers and SDK generators depend on it.

### What ships in DI

`AddApiCore` registers (and `AddApiExceptionHandling` registers a subset of) the following:

- `ExceptionFilter` — exception → `ProblemDetails`; rethrows the unmapped.
- `ResultConversionFilter` — `Result<T>` → wire format.
- `IExceptionStatusCodeMapper` (default `DefaultExceptionStatusCodeMapper`, `TryAddSingleton`).
- `ApiExceptionOptions` bound through `IOptions<>`.
- ASP.NET Core's `AddProblemDetails()` for paths that bypass MVC (routing 404, auth challenges,
  `UseStatusCodePages`) — same body shape, same `correlationId`.
- `SolTechnology.Core.Logging` + `ICorrelationIdService`.
- API versioning + per-version Swagger doc generation.

Replace or decorate any of the above; `TryAdd*` registrations mean your custom registration
wins.
