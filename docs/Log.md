# SolTechnology.Core.Logging

Production-ready logging primitives for ASP.NET Core: W3C-compliant correlation,
request envelope logs with status-aware levels, declarative log-scope enrichment,
allocation-free operation lifecycle events, and OpenTelemetry-friendly
`ActivitySource`s — all on top of `Microsoft.Extensions.Logging`. No Serilog or
Application Insights dependency required; works seamlessly with both.

> NuGet: **`SolTechnology.Core.Logging`** · target: `net10.0` /
> `Microsoft.AspNetCore.App` · zero ambient state, no `AsyncLocal` traps.

---

## What you get

| Feature | API |
|---|---|
| Correlation id (W3C `traceparent` + `X-Correlation-Id`) | `ICorrelationIdService`, `CorrelationId` |
| Request envelope logs (start / finish, status-aware levels) | `LoggingMiddleware` (auto-wired by `UseCoreLogging`) |
| Declarative scope enrichment from header / url / body | `services.LogDetail(...)` |
| Request-headers scope (PII-safe, opt-in)              | `LoggingOptions.LogRequestHeaders` + `MaskedHeaders` |
| Custom enrichers | `services.AddLogScopeEnricher<T>()`, `ILogScopeEnricher` |
| Operation lifecycle events (allocation-free) | `ILogger.OperationStarted/Succeeded/Failed` |
| OpenTelemetry tracing | `CoreLoggingActivitySources.OperationsName` |

---

## Getting started

### 1. Install

```bash
dotnet add package SolTechnology.Core.Logging
```

### 2. Register

```csharp
// Program.cs
builder.Services.AddCoreLogging();          // or AddCoreLogging(opts => { ... })
                                            // or AddCoreLogging(builder.Configuration)
```

`AddCoreLogging` is idempotent and registers the `ICorrelationIdService` and
`LoggingOptions` (validated on application start).

### 3. Wire the middleware

```csharp
var app = builder.Build();

app.UseCoreLogging();   // EARLY in the pipeline, before UseRouting / UseEndpoints
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
```

Place `UseCoreLogging` early so every request — including ones that fail
authentication — gets a correlation id and a request-envelope log entry.

### 4. (Optional) Configure via `appsettings.json`

```jsonc
{
  "Logging:Core": {
    "MaxLoggedJsonBodyBytes": 65536,
    "LogClientCorrelationParseErrors": true,
    "LogRequestHeaders": false,
    "SkipPaths": [ "/health", "/alive", "/metrics", "/swagger" ],
    "MaskedHeaders": [ "Authorization", "Cookie", "X-Api-Key" ]
  }
}
```

```csharp
builder.Services.AddCoreLogging(builder.Configuration);
```

`SkipPaths` silences request-envelope logs for liveness / readiness / metrics
scrapes — correlation is still propagated, only the noise is gone. A curated
starter list is available as `LoggingDefaults.InfrastructurePaths`:

```csharp
builder.Services.AddCoreLogging(o =>
{
    o.SkipPaths = LoggingDefaults.InfrastructurePaths.ToList();
});
```

---

## Logging request headers (with PII masking)

Opt in to dump every inbound HTTP header into the per-request log scope under
the property name `RequestHeaders`. Sensitive values are masked **before**
they reach any sink:

```csharp
builder.Services.AddCoreLogging(o =>
{
    o.LogRequestHeaders = true;
    // Defaults to LoggingDefaults.SensitiveHeaders
    // (Authorization, Cookie, X-Api-Key, X-Auth-Token, X-Csrf-Token, …).
    // Replace or extend for app-specific headers:
    o.MaskedHeaders = LoggingDefaults.SensitiveHeaders
        .Concat(new[] { "X-Internal-Token" })
        .ToList();
});
```

Masking rules:

- Header name listed in `MaskedHeaders` → value replaced with `***MASKED***`
  (case-insensitive match).
- Any value starting with `Bearer ` is masked regardless of header name —
  catches tokens forwarded via custom proxies (e.g. `X-Forwarded-Authorization`).

When `LogRequestHeaders = false` (the default) the enricher pays zero cost —
it short-circuits in O(1) without iterating headers.

---

## Correlation IDs

`CorrelationId` aligns with the **W3C Trace Context** standard and integrates
with `System.Diagnostics.Activity`, so OpenTelemetry, Application Insights and
ASP.NET Core 6+ all see the same id.

Inbound resolution order:

1. The current `Activity.TraceId` (populated by ASP.NET Core from the
   `traceparent` header).
2. A custom override via `X-Correlation-Id` (only when no Activity is in scope).
3. A freshly generated 32-character hex string.

Outbound: both `traceparent` and `X-Correlation-Id` are echoed on the response
so support can quote a single short id without parsing W3C trace context.

Use it from anywhere — background jobs, message-bus handlers, outbound
`HttpClient` handlers — via DI:

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

---

## Per-request scope enrichment

### Declarative — `LogDetail`

Promote a property from header, query/route, or JSON body into the
per-request log scope without writing any code:

```csharp
builder.Services.AddCoreLogging();

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

Body parsing is opt-in (driven by `LogDetailSource.Body` registrations), bounded
by `LoggingOptions.MaxLoggedJsonBodyBytes` (default 64 KB), only runs on
`application/json`, restores the request stream so MVC model binding still
works, and silently no-ops on malformed JSON.

### Programmatic — `ILogScopeEnricher`

When `LogDetail` is not expressive enough (composing values from claims,
multiple sources, async lookups, header masking), implement `ILogScopeEnricher`:

```csharp
public sealed class UserScopeEnricher : ILogScopeEnricher
{
    public void Enrich(HttpContext context, IDictionary<string, object?> scope)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            scope["UserId"] = context.User.FindFirst("sub")?.Value;
        }
    }
}

builder.Services.AddLogScopeEnricher<UserScopeEnricher>();
```

The middleware catches and warns on enricher failures so a faulty enricher
cannot take a request down.

---

## Operation lifecycle events

Three canonical events with stable `EventId`s for dashboard queries:

| EventId | Method               | Level       | Template |
|---------|----------------------|-------------|----------|
| 2137    | `OperationStarted`   | Information | `Operation: [{OperationName}]. Status: [START]` |
| 2138    | `OperationSucceeded` | Information | `Operation: [{OperationName}]. Status: [SUCCESS]. Duration: [{DurationMs} ms]` |
| 2139    | `OperationFailed`    | Error       | `Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. Message: [{Message}]` |
| 2140    | (user message)       | Information | `[{Message}]` |

Backed by `[LoggerMessage]` source generators — allocation-free, short-circuit
when the level is disabled.

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

Inside a CQRS pipeline these are emitted automatically — see
`SolTechnology.Core.CQRS` and the `[LogScope]` attribute.

---

## OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource(CoreLoggingActivitySources.OperationsName)   // MediatR operations
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

When no listener is attached, `ActivitySource.StartActivity` returns `null`
and per-request overhead collapses to one null-conditional access — apps that
don't opt in pay nothing.

---

## Recommended log levels (production)

```jsonc
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "SolTechnology.Core.Logging.Middleware.LoggingMiddleware": "Information",
      "SolTechnology.Core.CQRS.PipelineBehaviors.LoggingPipelineBehavior": "Information"
    },
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": {
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-ddTHH:mm:ss.fffZ",
        "UseUtcTimestamp": true
      }
    }
  }
}
```

`IncludeScopes: true` is required for any sink that should see correlation /
enrichment properties (Console JSON formatter, App Insights provider, Loki,
Datadog, etc.).

---

## Result in App Insights / structured sink

| Timestamp                  | Message                                                                  | CorrelationId | CityName |
|----------------------------|--------------------------------------------------------------------------|---------------|----------|
| 2026-05-06T08:00:10.738Z   | Started request [POST] [/api/v1/FindLocationOfCity]                      | 8fa1...       | Warsaw   |
| 2026-05-06T08:00:10.745Z   | Operation: [FindLocationOfCity]. Status: [START]                         | 8fa1...       | Warsaw   |
| 2026-05-06T08:00:12.859Z   | Operation: [FindLocationOfCity]. Status: [SUCCESS]. Duration: [2114 ms]  | 8fa1...       | Warsaw   |
| 2026-05-06T08:00:12.860Z   | Finished request [POST] [/api/v1/FindLocationOfCity] -> [200] in [2122 ms] | 8fa1...     | Warsaw   |


