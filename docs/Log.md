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
  `Error` 5xx). An unhandled exception writes exactly one `Error` entry
  (`failed after`) instead of `Finished`; a client abort writes one `Warning`.
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
- **Per-request timing diagnostics** — `ITimingService.StartContext("name")`
  records named sub-context durations; the aggregated map is emitted in the
  "Finished request" log automatically. Lightweight "where did the time go?"
  without a full APM.
- **OpenTelemetry tracing** — `CoreLoggingActivitySources.OperationsName` ready
  to plug into `WithTracing(...).AddSource(...)`. Zero cost when no listener
  attaches.
- **Skip-paths for liveness / metrics noise** — silences envelope logs for
  `/health`, `/metrics`, `/swagger`, etc., while still propagating correlation.
  Paths match per segment: `/health` covers `/health/ready`, not `/healthcare`.

### Registration

```csharp
// Program.cs
builder.Services.AddSolLogging();                       // or .AddSolLogging(opts => { ... })
                                                         // or .AddSolLogging(builder.Configuration)

var app = builder.Build();
app.UseSolLogging();   // EARLY — before UseRouting / UseEndpoints
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
```

Both `AddSolLogging` overloads register the same services; the `IConfiguration` overload
also binds `LoggingOptions`. `AddSolLogging` is idempotent; safe to call from multiple module installers.
Place `UseSolLogging` before `UseRouting` so requests that fail authentication
still get a correlation id and an envelope log entry.

### Configuration

`LoggingOptions` bind from `Logging:Core`:

| Option | Default | Purpose |
|---|---|---|
| `MaxLoggedJsonBodyBytes` | `65536` | Cap on JSON body buffered for `LogDetailSource.Body` enrichment. |
| `LogClientCorrelationParseErrors` | `true` | Emit a `Warning` when an inbound `X-Correlation-Id` header is rejected (longer than 128 characters or outside `[A-Za-z0-9._:-]`). |
| `LogRequestHeaders` | `false` | Project every inbound header into the scope as `RequestHeaders` (masked). |
| `SkipPaths` | `[]` | Path prefixes, matched per segment and case-insensitively, for which the request envelope logs and enrichers are skipped. Correlation is still set and echoed. |
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
builder.Services.AddSolLogging(builder.Configuration);
// or:
builder.Services.AddSolLogging(o => o.SkipPaths = LoggingDefaults.InfrastructurePaths.ToList());
```

### Usage

#### Correlation id from anywhere

Inbound resolution order:

1. The current `Activity.TraceId` when the request carried trace context (a
   `traceparent` header, or an `Activity` with a remote parent).
2. A valid inbound `X-Correlation-Id`.
3. The current `Activity.TraceId` (the trace the host started for this request).
4. A freshly generated 32-character hex id.

`X-Correlation-Id` is always echoed on the response; `traceparent` is echoed only
when the correlation id is the current trace id, so a caller's own id never gets
an invented span id.

| Inbound `X-Correlation-Id` | Outcome |
|---|---|
| Up to 128 characters from `[A-Za-z0-9._:-]` | Used as the correlation id and echoed. |
| Longer, or any other character (spaces, `,`, CR/LF, `<`) | Replaced by a generated id; `Warning` without the rejected value when `LogClientCorrelationParseErrors` is on. |

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

`endpoints` match per segment: `/api/v1/Trips` covers `/api/v1/Trips/42`, not
`/api/v1/TripsArchive`. Body parsing is opt-in (only when a `LogDetailSource.Body` registration exists),
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

builder.Services.AddSolLogScopeEnricher<UserScopeEnricher>();
```

A faulty enricher cannot take a request down — the middleware catches and warns
on enricher failures.

#### Request headers in the scope (opt-in, PII-safe)

```csharp
builder.Services.AddSolLogging(o =>
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

`LoggerExtensions` adds these members to every `ILogger`:

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

Inside a CQRS pipeline these are emitted automatically, with `[LogScope]`
properties in the scope — see `SolTechnology.Core.CQRS`. A handler that returns a
failed `Result` writes EventId 2149 at Warning instead of `OperationSucceeded`:
`Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. ErrorType: [{ErrorType}]. ErrorMessage: [{ErrorMessage}]`.
It carries only the `Error` type name and `Error.Message` (never validation
details or data), so `Error.Message` must not contain personal data — including
the inner messages an `AggregateError` builds into its own.

#### OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource(CoreLoggingActivitySources.OperationsName)   // CQRS pipeline operations
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

`SolTechnology.Core.CQRS` emits one `Activity` per request on its own `ActivitySource` named
`OperationsName`, so this one `AddSource` call covers it.

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
tests, `APIFixture<TEntryPoint>` from `SolTechnology.Core.API.Testing` boots your API
in memory, so the `AddSolLogging` + `UseSolLogging` calls in `Program.cs` make
correlation flow end-to-end.

```csharp
[Test]
public async Task Endpoint_AcceptsCorrelationId_AndEchoesItBack()
{
    // Arrange
    var client = _fixture.ServerClient;
    var request = new HttpRequestMessage(HttpMethod.Get, "/api/trips/42");
    request.Headers.Add("X-Correlation-Id", "support-ticket-42");   // no traceparent

    // Act
    var response = await client.SendAsync(request);

    // Assert
    response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be("support-ticket-42");
    response.Headers.Contains("traceparent").Should().BeFalse();
}
```


### Scope helpers

```csharp
using SolTechnology.Core.Logging;

using var tenantScope = logger.PushToScope("TenantId", tenantId);
// or multiple:
using var requestScope = logger.PushToScope(("TenantId", tenantId), ("Region", region));
```

### PII masking

```csharp
using SolTechnology.Core.Logging.Masking;

PiiMask.Full("secret@email.com");        // "***MASKED***"
PiiMask.Partial("secret@email.com", 3);  // "sec***com"
PiiMask.Partial("1234567", 3);           // "***MASKED***" — 7 is not longer than 4 * 3
```

`Partial` reveals nothing unless the value is longer than `4 * keepChars`, so at most half of
it is shown; a negative `keepChars` throws `ArgumentOutOfRangeException`.

On CQRS requests, combine `[LogScope]` with `[Masked]`:

```csharp
[LogScope, Masked(MaskMode.Partial, keepChars: 4)]
public string Email { get; set; } = null!;
// scope["Email"] = "john***.com"
```

### Conventions

- **`UseSolLogging` runs before `UseRouting`.** Otherwise pre-routing failures
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

`AddSolLogging` registers:

- `ICorrelationIdService` — ambient correlation accessor (singleton, no
  `AsyncLocal` traps).
- `ITimingService` — per-request timing diagnostics (scoped). Handlers wrap
  sub-operations in `StartContext("name")` and the map is emitted on finish.
- `TimeProvider` — `TimeProvider.System` (singleton, TryAdd — your custom
  registration wins for testing).
- `LoggingOptions` — bound and validated on application start.
- `LoggingMiddleware` — request envelope + scope composition, activated by
  `UseSolLogging`.
- `ILogScopeEnricher` set — declarative `LogDetail` registrations plus any
  custom enricher you add via `AddSolLogScopeEnricher<T>()`.

All registrations are `TryAdd*` so a consumer's custom registration always wins.

---

### Per-request timing diagnostics (`ITimingService`)

A lightweight "where did the time go in this request?" breakdown — without a full APM. Handlers
wrap sub-operations in `using (timingService.StartContext("name"))` and the aggregated
`{ name → elapsed ms }` map is emitted automatically in the "Finished request" log:

```
Finished request [GET] [/api/v2/cities/find] -> [200] in [156 ms] — timings: [{"http": 120, "cache": 2}]
```

#### Registration

Registered automatically by `AddSolLogging()` — no extra call needed.

#### Usage (in a handler or service)

```csharp
public class FindCityByNameHandler(
    ICityDomainService cityDomainService,
    ITimingService timingService) : IQueryHandler<FindCityByNameQuery, City>
{
    public async Task<Result<City>> Handle(FindCityByNameQuery query, CancellationToken ct)
    {
        City result;

        using (timingService.StartContext("http"))
        {
            result = await cityDomainService.Get(query.Name);
        }

        using (timingService.StartContext("cache"))
        {
            result = await cityDomainService.Get(query.Name);
        }

        return result;
    }
}
```

#### API

| Method | Behaviour |
|---|---|
| `StartContext(name)` | Starts a named timer. Returns `IDisposable` — disposing stops and accumulates. Multiple calls with the same name **sum** (e.g. two DB calls both named `"db"` aggregate into one total). |
| `GetTimings()` | Returns a read-only `IDictionary<string, long>` snapshot — name → total elapsed ms. Called by the middleware on request finish. |
| `Reset()` | Clears all accumulated timings. Called by the middleware at request start. |

#### Design notes

- **Scoped** — each HTTP request gets its own `TimingService` instance (no cross-request leakage).
- **`TimeProvider`-sourced** — all timing reads go through `TimeProvider` (testable with `FakeTimeProvider`).
- **Idempotent dispose** — double-disposing a handle is safe (second dispose is a no-op).
- **Zero cost when unused** — if no handler calls `StartContext`, the "Finished request" log omits
  the `timings` field entirely (no empty `{}` noise).
- **Aggregates, not traces** — this is a request-level summary, not a span tree. For full
  distributed tracing, wire `CoreLoggingActivitySources` into OpenTelemetry.

---

### Recipe: Singleton→scoped correlation bridge (background workers)

> **This is a pattern, not shipped code.** The shape is too tenant-model-coupled to generalise
> into a library primitive. Copy and adapt.

A singleton background worker (Hangfire job, `BackgroundService`) has no HTTP request scope — so
`ICorrelationIdService` is empty and DI scoped services aren't available. The recipe spins a DI
scope manually and attaches correlation + principal context:

```csharp
public class SettlementPollerJob(IServiceScopeFactory scopeFactory, ICorrelationIdService correlation)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // 1. Generate a correlation for this background unit-of-work
        var correlationId = CorrelationId.Generate();
        correlation.Set(correlationId);

        // 2. Spin a DI scope (so scoped services like DbContext are fresh)
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ISettlementHandler>();

        // 3. Push a log scope so every log in this UoW carries the correlation
        using var logScope = scope.ServiceProvider
            .GetRequiredService<ILogger<SettlementPollerJob>>()
            .BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId.Value,
                ["JobName"] = "SettlementPoller"
            });

        // 4. Do the work
        await handler.ProcessAsync(ct);
    }
}
```

**Key points:**
- `ICorrelationIdService` is singleton — set it before starting work so downstream HTTP calls
  (`CorrelationPropagatingHandler`) pick it up.
- Dispose the scope when done — `DbContext`, `IIdempotencyStore`, etc. are released.
- For Hangfire jobs: `Core.Hangfire`'s `CorrelationIdJobFilter` does steps 1+3 automatically.
  Use this pattern only for raw `BackgroundService` or custom polling loops.


