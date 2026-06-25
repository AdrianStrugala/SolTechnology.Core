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
- **Testing companion** — `SolTechnology.Core.API.Testing` ships `APIFixture<TEntryPoint>` so test-host
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

For integration tests, additionally reference `SolTechnology.Core.API.Testing`.

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
  "correlationId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "recoverable":   false
}
```

#### `extensions.recoverable` — retry-ability hint

Every `ProblemDetails` response carries `extensions.recoverable` (boolean, **always present** —
absence is never ambiguous). It tells the client whether the failure is worth retrying:

| Source | `recoverable` value |
|---|---|
| `Result.Fail(error)` | `error.Recoverable` — set by the application layer (`Error.Recoverable` init property). |
| Mapped exception → 4xx | `false` — deterministic client/business rejection; retry will produce the same result. |
| Mapped exception → 5xx | `true` — transient server fault; worth retrying. |
| `ValidationException` → 400 | `false` — the input is structurally wrong. |

Use `Recoverable = true` on your `Error` when the failure is transient and retryable:

```csharp
return Result<Trip>.Fail(new Error { Message = "Upstream timeout.", Recoverable = true });
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

Reference `SolTechnology.Core.API.Testing` from your test project. It wraps
`WebApplicationFactory<TEntryPoint>` as `APIFixture<TEntryPoint>` and adds the config-override and
auth-client helpers every suite was hand-rolling.

| Member | Purpose |
|---|---|
| `APIFixture<TEntryPoint>` | Boots the in-memory host. Exposes `TestServer` and a ready `ServerClient` (`HttpClient`). Ctor takes an optional `IConfiguration` and an optional `Action<IServiceCollection>` for service overrides. |
| `TestConfigurationBuilder` | Fluent `appsettings.tests.json` + in-memory overrides → `IConfiguration` (container connection strings, dynamic mock URLs). In-memory overrides win. |
| `CreateAuthorizedClient(scheme, token)` | Client with an `Authorization` header (scheme-agnostic: `Bearer`, a custom test scheme, …). |
| `CreateAnonymousClient()` | Client with no `Authorization` header — for unauthenticated paths. |

```csharp
public class TripsApiTests
{
    [Test]
    public async Task Get_ReturnsTrip()
    {
        // Arrange — compose config, then boot the host (typically in an assembly-level [SetUpFixture]).
        var configuration = new TestConfigurationBuilder()
            .AddJsonFile("appsettings.tests.json")
            .Override("Sql:ConnectionString", sqlFixture.DatabaseConnectionString)
            .Build();

        using var fixture = new APIFixture<Program>(configuration);
        var client = fixture.CreateAuthorizedClient("Bearer", token: "test-token");

        // Act
        var response = await client.GetAsync("/api/trips/42");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

Override services at boot — e.g. swap a background publisher for a deterministic in-process one:

```csharp
var fixture = new APIFixture<Program>(configuration, services =>
{
    services.RemoveAll<IHangfireNotificationPublisher>();
    services.AddSingleton<IHangfireNotificationPublisher, SyncHangfireNotificationPublisher>();
});
```

`APIFixture` is part of the modular testing framework — compose it with `SQLFixture`,
`WireMockFixture`, etc. See [theQuality.md](theQuality.md) for the full component-test harness.

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

---

### Security headers

`UseSecurityHeaders()` stamps a strict baseline of security headers on **every** response — including
error responses produced by the ProblemDetails pipeline. It is a pipeline (`Use…`) concern the host
opts into; it is NOT wired into `AddApiCore` automatically.

```csharp
app.UseSecurityHeaders();
```

| Header | Default value | Purpose |
|---|---|---|
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | No script/style/img loading; no iframe embedding. Strictest possible for a JSON API. |
| `X-Content-Type-Options` | `nosniff` | Prevent MIME-type sniffing. |
| `Referrer-Policy` | `no-referrer` | Never leak the request URL as a `Referer` header. |

Pre-existing headers set by an upstream middleware are **never overwritten** (`TryAdd` semantics).

#### Relaxing for Swagger / Redoc

Swagger UI and Redoc need inline scripts/styles. By default, paths prefixed with `/swagger` or
`/docs` receive a relaxed CSP (`default-src 'self'; script-src 'self' 'unsafe-inline'; …`). The
strict policy remains on all other paths.

```csharp
app.UseSecurityHeaders(o =>
{
    // Add a custom docs path
    o.RelaxedPathPrefixes.Add("/my-docs");

    // Or override the referrer policy
    o.ReferrerPolicy = "strict-origin-when-cross-origin";
});
```

| Option | Type | Default |
|---|---|---|
| `ContentSecurityPolicy` | `string` | `default-src 'none'; frame-ancestors 'none'` |
| `RelaxedContentSecurityPolicy` | `string` | `default-src 'self'; script-src 'self' 'unsafe-inline'; …` |
| `RelaxedPathPrefixes` | `List<string>` | `["/swagger", "/docs"]` |
| `ContentTypeOptions` | `string` | `nosniff` |
| `ReferrerPolicy` | `string` | `no-referrer` |

---

### Health endpoint

`MapCoreHealthChecks(path)` maps an ASP.NET health endpoint that renders the registered checks as
JSON via `HealthReportJsonFormatter`. Health checks live **next to the module they probe** — there
is no foundation package. Compose with the framework `AddHealthChecks()` and chain the per-module
checks, then map the endpoint:

```csharp
builder.Services.AddHealthChecks()
    .AddSqlHealthCheck()            // Core.SQL
    .AddRedisHealthCheck()          // Core.Cache
    .AddServiceBusHealthCheck()     // Core.MessageBus
    .AddUpstreamHttpHealthCheck<MyReport>("payments", "https://payments/health"); // Core.HTTP

app.MapCoreHealthChecks("/health");
```

Status codes follow the framework default: **200** for `Healthy`/`Degraded`, **503** for
`Unhealthy`. The JSON body:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "sql":   { "status": "Healthy", "description": "SQL reachable", "duration": "00:00:00.0050000" },
    "redis": { "status": "Healthy", "duration": "00:00:00.0010000" }
  }
}
```

`HealthReportJsonFormatter.Format(report)` is a **pure** `HealthReport` → JSON formatter (no
`HttpContext`), so it is independently testable and reusable; `MapCoreHealthChecks` is the thin
ASP.NET adapter. This is the only ASP.NET-coupled piece of the health-check feature — the per-module
checks reference the framework-agnostic `Microsoft.Extensions.Diagnostics.HealthChecks` directly.


