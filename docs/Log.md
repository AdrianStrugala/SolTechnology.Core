## SolTechnology.Core.Logging

Production-ready logging primitives for ASP.NET Core on top of
`Microsoft.Extensions.Logging`. Pull it in and your app gets a W3C-compliant
correlation id on every request, status-aware request envelope logs, declarative
log-scope enrichment, allocation-free operation lifecycle events, and an
OpenTelemetry-friendly `ActivitySource` — with zero ambient state and no Serilog
or Application Insights dependency required.

### Features

- **W3C correlation id** — `traceparent` + `X-Correlation-Id` resolved from the
  inbound request and echoed on the response. Same id flows through the log
  scope, downstream HTTP calls, and `Activity.Current`.
- **Request envelope logs** — one `Started` / `Finished` pair per request with
  log levels aligned to the status code (`Information` < 400, `Warning` 4xx,
  `Error` 5xx) and `Warning` for client aborts.
- **Declarative scope enrichment** — promote a header, route value, query
  parameter, or JSON body field into the per-request log scope with one DI line:
  `services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header)`.
- **Custom enrichers** — drop in `ILogScopeEnricher` for anything the
  declarative form can't express (claims, async lookups, multi-source values).
- **Request-header scope with PII masking** — opt-in dump of every inbound
  header into the scope; sensitive values (`Authorization`, `Cookie`, anything
  starting with `Bearer `) are masked before they reach any sink.
- **Operation lifecycle events** — `OperationStarted` / `OperationSucceeded` /
  `OperationFailed` with stable `EventId`s (2137–2140) and `[LoggerMessage]`
  source generators (allocation-free, short-circuits when disabled).
- **OpenTelemetry tracing** — `CoreLoggingActivitySources.OperationsName` ready
  to plug into `WithTracing(...).AddSource(...)`. Zero cost when no listener
  attaches.
- **Skip-paths for liveness / metrics noise** — silences envelope logs for
  `/health`, `/metrics`, `/swagger`, etc., while still propagating correlation.

### Registration

```csharp
// Program.cs
builder.Services.AddCoreLogging();                       // or .AddCoreLogging(opts => { ... })
                                                         // or .AddCoreLogging(builder.Configuration)

var app = builder.Build();
app.UseCoreLogging();   // EARLY — before UseRouting / UseEndpoints
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
```

`AddCoreLogging` is idempotent; safe to call from multiple module installers.
Place `UseCoreLogging` before `UseRouting` so requests that fail authentication
still get a correlation id and an envelope log entry.

### Configuration

`LoggingOptions` bind from `Logging:Core`:

| Option | Default | Purpose |
|---|---|---|
| `MaxLoggedJsonBodyBytes` | `65536` | Cap on JSON body buffered for `LogDetailSource.Body` enrichment. |
| `LogClientCorrelationParseErrors` | `true` | Emit a `Warning` when an inbound `X-Correlation-Id` header is malformed. |
| `LogRequestHeaders` | `false` | Project every inbound header into the scope as `RequestHeaders` (masked). |
| `SkipPaths` | `[]` | Path prefixes for which the request envelope logs and enrichers are skipped. Correlation is still set and echoed. |
| `MaskedHeaders` | `LoggingDefaults.SensitiveHeaders` | Header names whose values are replaced with `***MASKED***`. Case-insensitive. |

```jsonc
{
  "Logging:Core": {
    "MaxLoggedJsonBodyBytes": 65536,
    "LogRequestHeaders": false,
    "SkipPaths": [ "/health", "/alive", "/metrics", "/swagger" ],
    "MaskedHeaders": [ "Authorization", "Cookie", "X-Api-Key" ]
  }
}
```

```csharp
builder.Services.AddCoreLogging(builder.Configuration);
// or:
builder.Services.AddCoreLogging(o => o.SkipPaths = LoggingDefaults.InfrastructurePaths.ToList());
```

### Usage

#### Correlation id from anywhere

Inbound resolution order: current `Activity.TraceId` → inbound
`X-Correlation-Id` (only when no Activity is in scope) → a freshly generated
32-character hex id. Both `traceparent` and `X-Correlation-Id` are echoed on the
response.

```csharp
public sealed class AuditWriter(ICorrelationIdService correlation, ILogger<AuditWriter> logger)
{
    public Task WriteAsync(string action, CancellationToken ct)
    {
        var id = correlation.GetOrGenerate();   // creates one for non-HTTP entry points
        logger.LogInformation("Audit [{Action}] correlation [{Correlation}]", action, id);
        return Task.CompletedTask;
    }
}
```

#### Declarative scope enrichment — `LogDetail`

```csharp
// Header → scope["TenantId"]
builder.Services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header);

// Query / route key → scope["UserId"]
builder.Services.LogDetail("userId", source: LogDetailSource.Url);

// JSON body field "name" → scope["CityName"], only on these endpoints
builder.Services.LogDetail(
    "name",
    asName: "CityName",
    source: LogDetailSource.Body,
    endpoints: ["/api/v1/FindLocationOfCity", "/api/FindCityByName"]);
```

Body parsing is opt-in (only when a `LogDetailSource.Body` registration exists),
bounded by `MaxLoggedJsonBodyBytes`, runs only on `application/json`, restores
the request stream so MVC model binding still works, and silently no-ops on
malformed JSON.

#### Custom enricher — `ILogScopeEnricher`

```csharp
public sealed class UserScopeEnricher : ILogScopeEnricher
{
    public void Enrich(HttpContext context, IDictionary<string, object?> scope)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
            scope["UserId"] = context.User.FindFirst("sub")?.Value;
    }
}

builder.Services.AddLogScopeEnricher<UserScopeEnricher>();
```

A faulty enricher cannot take a request down — the middleware catches and warns
on enricher failures.

#### Request headers in the scope (opt-in, PII-safe)

```csharp
builder.Services.AddCoreLogging(o =>
{
    o.LogRequestHeaders = true;
    o.MaskedHeaders = LoggingDefaults.SensitiveHeaders
        .Concat(new[] { "X-Internal-Token" })
        .ToList();
});
```

Masking rules:

| Trigger | Outcome |
|---|---|
| Header name listed in `MaskedHeaders` (case-insensitive) | Value → `***MASKED***`. |
| Any value starting with `Bearer ` (regardless of header name) | Value → `***MASKED***`. |

When `LogRequestHeaders = false` the enricher short-circuits in O(1) without
iterating headers.

#### Operation lifecycle events

| EventId | Method               | Level       | Template |
|---:|---|---|---|
| 2137 | `OperationStarted`   | Information | `Operation: [{OperationName}]. Status: [START]` |
| 2138 | `OperationSucceeded` | Information | `Operation: [{OperationName}]. Status: [SUCCESS]. Duration: [{DurationMs} ms]` |
| 2139 | `OperationFailed`    | Error       | `Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. Message: [{Message}]` |
| 2140 | (user message)       | Information | `[{Message}]` |

```csharp
var sw = ValueStopwatch.StartNew();
logger.OperationStarted(nameof(ImportInvoices), message: "batch=123");
try
{
    await ImportAsync(ct);
    logger.OperationSucceeded(nameof(ImportInvoices), sw.ElapsedMilliseconds);
}
catch (Exception ex)
{
    logger.OperationFailed(nameof(ImportInvoices), sw.ElapsedMilliseconds, ex);
    throw;
}
```

Inside a CQRS pipeline these are emitted automatically via the `[LogScope]`
attribute — see `SolTechnology.Core.CQRS`.

#### OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource(CoreLoggingActivitySources.OperationsName)   // MediatR operations
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

#### Result in App Insights / structured sink

| Timestamp | Message | CorrelationId | CityName |
|---|---|---|---|
| 2026-05-06T08:00:10.738Z | Started request [POST] [/api/v1/FindLocationOfCity] | 8fa1… | Warsaw |
| 2026-05-06T08:00:10.745Z | Operation: [FindLocationOfCity]. Status: [START] | 8fa1… | Warsaw |
| 2026-05-06T08:00:12.859Z | Operation: [FindLocationOfCity]. Status: [SUCCESS]. Duration: [2114 ms] | 8fa1… | Warsaw |
| 2026-05-06T08:00:12.860Z | Finished request [POST] [/api/v1/FindLocationOfCity] -> [200] in [2122 ms] | 8fa1… | Warsaw |

### Testing

No dedicated fixture — use `Microsoft.Extensions.Logging.Testing.FakeLogger` (or
NSubstitute on `ILogger<T>`) and assert on the captured entries. For component
tests, `APIFixture<TEntryPoint>` from `SolTechnology.Core.API.Testing` already wires
`AddCoreLogging` + `UseCoreLogging`, so correlation flows end-to-end out of the
box.

```csharp
[Test]
public async Task Endpoint_AcceptsCorrelationId_AndEchoesItBack()
{
    // Arrange
    var client = _fixture.CreateClient();
    var correlation = "0123456789abcdef0123456789abcdef";
    client.DefaultRequestHeaders.Add("X-Correlation-Id", correlation);

    // Act
    var response = await client.GetAsync("/api/trips/42");

    // Assert
    response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be(correlation);
}
```

### Conventions

- **`UseCoreLogging` runs before `UseRouting`.** Otherwise pre-routing failures
  (auth challenges, 404s) leave production without a correlation id.
- **`IncludeScopes: true` in the console formatter** — any sink that should see
  correlation / enrichment properties (Console JSON, App Insights, Loki,
  Datadog) requires it.
- **`{PascalCase}` placeholders** — matches MEL / Serilog / App Insights and the
  KQL queries consumers will write. See `ClaudeCodingGuide.md` §11.
- **Wrap values in `[]`** in every placeholder. Empty becomes `[]` instead of
  invisible.
- **Never log secrets.** Add the header to `MaskedHeaders`; never craft a
  bespoke log line that bypasses the enricher.
- **Production levels:** `Default: Warning`,
  `Microsoft.Hosting.Lifetime: Information`,
  `SolTechnology.Core.Logging.Middleware.LoggingMiddleware: Information`,
  `SolTechnology.Core.CQRS.PipelineBehaviors.LoggingPipelineBehavior: Information`.

### What ships in DI

`AddCoreLogging` registers:

- `ICorrelationIdService` — ambient correlation accessor (singleton, no
  `AsyncLocal` traps).
- `LoggingOptions` — bound and validated on application start.
- `LoggingMiddleware` — request envelope + scope composition, activated by
  `UseCoreLogging`.
- `ILogScopeEnricher` set — declarative `LogDetail` registrations plus any
  custom enricher you add via `AddLogScopeEnricher<T>()`.
- `CoreLoggingActivitySources` — `OperationsName` activity source for
  OpenTelemetry plumbing.

All registrations are `TryAdd*` so a consumer's custom registration always wins.
