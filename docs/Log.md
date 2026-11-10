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
  `Error` 5xx). The `Finished` entry is the request's Chronicle report; an
  unhandled exception makes it the one `Error` entry, with the exception.
- **Chronicle step reports** — `IChronicle` records a bounded tree of steps per
  request, command, job, or message (CQRS, Tale, HTTP client and SQL steps
  without user code) and writes one entry per root, with N+1 hints, level
  escalation, sampling, and an opt-in flight recorder.
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
  to plug into `WithTracing(...).AddSource(...)`; every Chronicle step is a span.
  Zero cost when no listener attaches.
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
        .AddSource(CoreLoggingActivitySources.OperationsName)   // Chronicle steps
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

Every Chronicle step starts an `Activity` on `OperationsName` when the source has listeners: manual
steps, CQRS requests (the CQRS span is the step's span), Tale runs and chapters, HTTP client calls
(logical spans around `HttpClient`'s own), and job and message roots. The HTTP request root starts
none, because ASP.NET Core owns the request activity, and SQL steps start none.
`EmitActivities = false` turns the spans off.

#### Result in App Insights / structured sink

| Timestamp | Message | CorrelationId | CityName |
|---|---|---|---|
| 2026-05-06T08:00:10.738Z | Started request [POST] [/api/v1/FindLocationOfCity] | 8fa1… | Warsaw |
| 2026-05-06T08:00:10.745Z | Operation: [FindLocationOfCity]. Status: [START] | 8fa1… | Warsaw |
| 2026-05-06T08:00:12.859Z | Operation: [FindLocationOfCity]. Status: [SUCCESS]. Duration: [2114 ms] | 8fa1… | Warsaw |
| 2026-05-06T08:00:12.860Z | Finished request [POST] [/api/v1/FindLocationOfCity] -> [200] in [2122 ms]. Chronicle: [Ok] [HTTP POST …] | 8fa1… | Warsaw |

### Chronicle

Chronicle records a bounded tree of named steps for every root operation and writes **one** log
entry per root when it completes.

```csharp
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Masking;

public sealed class CityImporter(IChronicle chronicle, ICityRepository repository)
{
    public async Task Import(IReadOnlyList<City> cities, CancellationToken cancellationToken)
    {
        using var root = chronicle.Begin("ImportCities");   // a root here; a child step inside a request or job
        root.Set("Count", cities.Count);
        foreach (var city in cities)
        {
            using var step = chronicle.Step("SaveCity");     // constant name: IDs go into attributes
            step.SetMasked("OwnerEmail", city.OwnerEmail, MaskMode.Partial);
            await repository.Save(city, cancellationToken);
        }
    }
}
```

The report renders as an ASCII tree, so every sink and encoding shows it:

```text
GET /cities [@0 ms] [25 ms] [ok]
|- FindCity [@2 ms] [14 ms] [ok] City=Krakow Limit=10
|  `- SQL SELECT Cities [@5 ms] [10 ms] [ok]
`- Http [@20 ms] [5 ms] [ok]
```

#### API

| Member | Behavior |
|---|---|
| `IChronicle.Begin(name)` | Starts a root when none is open in the current async flow; otherwise a child of the current step. |
| `IChronicle.BeginRoot(name)` | Always starts a new root, for work that outlives its caller (`Task.Run`, timers); records `DetachedFrom` when another root is open. |
| `IChronicle.Step(name)` | Starts a child of the current step; outside a root it records nothing. |
| `IChronicle.Current` | Non-owning handle to the current step; its `Dispose` does nothing. |
| `IChronicle.IsActive` | `true` when Chronicle is enabled and a root is open. |
| `ChronicleStep.Set(key, value)` | Adds an attribute with the raw value (invariant culture). |
| `ChronicleStep.SetMasked(key, value, mode, keepChars)` | Adds a masked attribute through `PiiMask`; the raw value is never kept. |
| `ChronicleStep.Fail(exception)` / `Fail(errorType, message)` | Marks the step `Failed`; an exception on a root is attached to its log entry. |
| `ChronicleStep.Cancel()` | Marks the step `Cancelled`. |

Every handle is a `readonly struct`: when Chronicle is disabled or no root is open, a step costs one
`AsyncLocal` read and one `ActivitySource.HasListeners()` check and allocates nothing. Parallel
branches (`Task.WhenAll`) build isolated subtrees.

#### What becomes a root or a step

| Source | Root | Step | Details |
|---|---|---|---|
| HTTP request (`UseSolLogging`) | `HTTP {Method}` with `Route` and `StatusCode` | — | Its report is the request's finish entry (2155); WebSocket, upgraded and server-sent-event requests get none. |
| CQRS command or query | outside a root | `{RequestType}` | [CQRS](CQRS.md#chronicle) |
| CQRS fire-and-forget event | `Event {EventType}` | one per handler | [CQRS](CQRS.md#chronicle) |
| Tale run and chapter | outside a root | `Tale {Handler}`, `Chapter {ChapterId}` | [Tale](Tale.md#chronicle) |
| HTTP client call | — | `HTTP {Method} {client}` | [Clients](Clients.md#chronicle) |
| SqlClient command | — | `SQL {OPERATION} {table}`, `SQL EXEC {procedure}` | [SQL](SQL.md#chronicle) |
| MessageBus message | `Message {MessageType}` | — | [Bus](Bus.md#chronicle) |
| Hangfire job | `Job {TJob}`, `Event {EventType}` | — | [Hangfire](Hangfire.md#chronicle) |

#### Options

`ChronicleOptions` bind from `Logging:Core:Chronicle` through `AddSolLogging(IConfiguration)` or
`AddSolChronicle(IConfiguration)`; the parameterless `AddSolChronicle()` calls that other installers
make keep the defaults and never override a binding.

| Option | Default | Range | Purpose |
|---|---|---|---|
| `Enabled` | `true` | — | `false` turns every call into a non-recording handle; the request middleware then writes 2144–2146. |
| `MaxSteps` | `256` | 1–10000 | Recorded steps per root, the root included; later steps only feed their parent's aggregate line. |
| `MaxDepth` | `12` | 1–64 | Depth below the root; deeper steps are counted as `+{n} deeper steps`. |
| `MaxAttributesPerStep` | `8` | 0–64 | Later keys are counted as `+{n} attributes dropped`. |
| `MaxAttributeValueLength` | `200` | 16–4096 | Cut for attribute values, step names and failure messages. |
| `AggregateThreshold` | `3` | ≥ 2 | Siblings with the same name (SQL: the same statement) collapse into one line. |
| `NPlusOneThreshold` | `10` | ≥ `AggregateThreshold` | An aggregated line this large is marked `N+1 suspected`. |
| `SlowThreshold` | `00:00:01` | > 0 | A successful root at least this slow is reported at Warning. |
| `SampleRate` | `1.0` | 0.0–1.0 | Share of fast, successful roots that are reported. |
| `EmitActivities` | `true` | — | Starts an `Activity` per step on `OperationsName` when it has listeners; `false` also removes the CQRS operation spans. |
| `CaptureSQLCommands` | `true` | — | Records SqlClient commands as `SQL` steps. |
| `CaptureSQLCommandText` | `false` | — | Records the statement text and SqlClient error messages, which can carry literal values. |
| `FlightRecorder:Enabled` | `false` | — | Attaches buffered Debug entries to the report of a failed root. |
| `FlightRecorder:MinimumLevel` | `Debug` | `Trace`, `Debug` | Lowest buffered level. |
| `FlightRecorder:MaxEntries` | `100` | 1–1000 | Buffered entries per root; later ones are counted. |
| `FlightRecorder:MaxMessageLength` | `500` | 32–4096 | Cut for a buffered message. |

```jsonc
{
  "Logging:Core:Chronicle": {
    "SlowThreshold": "00:00:02",
    "SampleRate": 0.25,
    "FlightRecorder": { "Enabled": true }
  }
}
```

Invalid values fail the host on start (`ValidateOnStart`).

#### Levels and sampling

| Root outcome | Level |
|---|---|
| `Failed`, or an HTTP 5xx | Error |
| `Cancelled`, an HTTP 4xx, `Ok` with a failed step below the root, or `Ok` and at least `SlowThreshold` | Warning |
| `Ok` | Information |

Sampling is decided when the root ends: a failed or cancelled root, a report at Warning or above, a
report with any failed step, and an `N+1 suspected` report are always written; only the remaining
fast successes are kept with probability `SampleRate`. With `SampleRate < 1` the request-rate and
error-ratio queries that count log entries are wrong; use the ASP.NET Core request metrics instead.

#### Reading a report

| Marker | Meaning |
|---|---|
| `x42 [total 380 ms] [max 31 ms] [failed 0]` | Aggregated siblings with the same name. |
| `N+1 suspected` | The aggregated line reached `NPlusOneThreshold`. |
| `[truncated]` | An attribute value, step name or failure message was cut. |
| `+{n} steps dropped` | Steps beyond `MaxSteps`; they still count in their parent's aggregate line. |
| `+{n} deeper steps` / `+{n} attributes dropped` | Steps beyond `MaxDepth` / attributes beyond `MaxAttributesPerStep`. |
| `+{n} nested` | Children of the steps behind an aggregated line. |
| `+{n} late steps` | Steps started under a parent step that had already closed. |
| `(unfinished)` | The step was still open when its root ended. |
| `+{n} lines truncated` | The rendered tree is capped at 32,768 characters. |
| `flight recorder:` | Buffered Debug entries of a failed root, closed by `+{n} records dropped` when capped. |

#### Log entries

| EventId | Name | Category | Template |
|---:|---|---|---|
| 2154 | `ChronicleReported` | `SolTechnology.Core.Logging.Chronicle` | `Chronicle [{ChronicleName}] [{ChronicleStatus}] in [{DurationMs} ms], [{StepCount}] steps: [{@Chronicle}]` |
| 2155 | `RequestChronicleReported` | `SolTechnology.Core.Logging.Middleware.LoggingMiddleware` | `Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]. Chronicle: [{ChronicleStatus}] [{@Chronicle}]` |

The operation entries 2137–2140 are unchanged; the request envelope keeps 2141–2143 and 2148, writes
2144–2146 only when Chronicle is disabled or replaced, and 2147 is gone. CQRS writes 2149 and
Hangfire 2150–2153 (see their pages). The report reaches providers under the state key `Chronicle`
(the `[LoggerMessage]` generator drops the `@`), so Serilog, OpenTelemetry and Application Insights
store it as the tree text; filter on the scalar properties. With the OpenTelemetry logging option
`IncludeFormattedMessage = true` the tree is exported twice, in the body and in the `Chronicle`
attribute.

```kusto
traces
| where customDimensions.EventId == "2155"
| where customDimensions.ChronicleStatus != "Ok" or toint(customDimensions.ElapsedMs) > 1000
| project timestamp, customDimensions.RequestPath, customDimensions.StatusCode, message
```

To drop the per-request CQRS entries and keep the Chronicle reports, set the
`SolTechnology.Core.CQRS.PipelineBehaviors.LoggingPipelineBehavior` category to Warning: that filters
2137 (start) and 2138 (success) and keeps the failure entries 2149 (Warning, failed `Result`) and 2139
(Error, exception); the requests still appear as steps.

#### Log contract changes in 2.0

| 1.x entry (no EventId) | 2.0, Chronicle disabled | 2.0, Chronicle enabled (default) |
|---|---|---|
| `Finished request …` at Information / Warning (4xx) / Error (5xx) | 2146, same template and levels | 2155 with `ChronicleStatus`; a success slower than `SlowThreshold` is Warning |
| `Finished request … — timings: [{@Timings}]` | removed (2147 retired) | 2155 |
| `Request [..] [..] failed after [..]`, Error, with the exception | 2145 | 2155, Error, with the exception, `ChronicleStatus = Failed` |
| `Request [..] [..] aborted by client after [..]`, Warning | 2144 | 2155, Warning, `ChronicleStatus = Cancelled` |

A 5xx response produced without an exception (an MVC filter mapped it) is `Failed` too.
`RequestMethod`, `RequestPath`, `StatusCode` and `ElapsedMs` keep their names and values.

```kusto
// 1.x
traces | where message has "failed after"
// 2.0
traces | where customDimensions.EventId == "2155" and customDimensions.ChronicleStatus == "Failed"
```

#### Flight recorder

With `FlightRecorder:Enabled`, Debug entries written inside a root are buffered and attached to the
report only when the root fails; successful roots drop them.

- Inside a root `IsEnabled(Debug)` returns `true`, so `IsEnabled`-guarded Debug code runs and every
  Debug template is formatted; worst-case memory is `MaxEntries × MaxMessageLength × 2 bytes` per
  concurrent root (100 KB with the defaults).
- `Microsoft*` and `System*` categories are buffered from Warning only, unless a logging rule targets
  the provider alias `ChronicleFlightRecorder`; Chronicle's own category is never buffered.
- Serilog's `UseSerilog()` replaces the logger factory, so add `writeToProviders: true` (or use
  `builder.Logging.AddSerilog()`), let Serilog's `MinimumLevel` admit Debug, and keep the sinks at
  their own level:

```csharp
builder.Host.UseSerilog((context, configuration) => configuration
        .MinimumLevel.Debug()
        .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information),
    writeToProviders: true);
```

#### Rules of use

- Keep step names constant (`SaveCity`, not `SaveCity 42`): names become span names and aggregation
  keys. Put IDs into attributes.
- `Set` records the raw value; use `SetMasked` for personal data.
- A root must not span a service lifetime: open one root per loop iteration, message or job
  (`BeginRoot` inside a `BackgroundService` loop).
- WebSocket and other upgraded requests (SignalR, Blazor Server) and server-sent events
  (`Accept: text/event-stream`) get no request root, because one report per connection would grow
  until disconnect: they keep the 2144–2146 entries, and each CQRS call inside them is its own root.
  Add their paths to `SkipPaths` to drop the envelope entries as well.
- SQL capture is a hosted service, so it runs only in hosts that start their `IHostedService`s.
- The console formatter's `SingleLine: true` flattens the tree into one line; keep `SingleLine: false`
  where people read the console.

#### Migrating from ITimingService (1.x)

- Inject `IChronicle` instead of `ITimingService`.
- Replace `using (timingService.StartContext("x"))` with `using (chronicle.Step("x"))`.
- Delete `GetTimings()` and `Reset()` calls; the report is written automatically per root.
- Use `chronicle.BeginRoot("name")` for work that outlives its caller, so it gets its own report.
- Queries on `{@Timings}` move to the 2155 entry and its `Chronicle` property.
- The removal of `ITimingService` is binary-breaking: rebuild every assembly compiled against Logging
  1.x that references it. `SOLCORE001` does not detect this; `ValidateOnBuild` reports the missing
  service at startup.

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
  `SolTechnology.Core.CQRS.PipelineBehaviors.LoggingPipelineBehavior: Information`,
  `SolTechnology.Core.Logging.Chronicle: Information`.

### What ships in DI

`AddSolLogging` registers:

- `ICorrelationIdService` — ambient correlation accessor with `Set`, `Get`, `GetOrGenerate`, and `Clear`
  (singleton, no `AsyncLocal` traps).
- `TimeProvider` — `TimeProvider.System` (singleton, TryAdd — your custom
  registration wins for testing).
- `LoggingOptions` — bound and validated on application start.
- `IChronicle` (singleton) with its reporter, `ChronicleOptions` (validated on start), the
  flight-recorder `ILoggerProvider`, and the SqlClient subscription as an `IHostedService`
  (`AddSolChronicle`).
- `LoggingMiddleware` — request envelope + scope composition, activated by
  `UseSolLogging`.
- `ILogScopeEnricher` set — declarative `LogDetail` registrations plus any
  custom enricher you add via `AddSolLogScopeEnricher<T>()`.

All registrations are `TryAdd*` so a consumer's custom registration always wins.

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
- Call `correlation.Clear()` when work set the id synchronously on a long-lived thread, so the next unit of work on
  that thread does not inherit it.
- Dispose the scope when done — `DbContext`, `IIdempotencyStore`, etc. are released.
- For Hangfire jobs: `Core.Hangfire`'s `CorrelationIdJobFilter` does steps 1+3 automatically.
  Use this pattern only for raw `BackgroundService` or custom polling loops.


