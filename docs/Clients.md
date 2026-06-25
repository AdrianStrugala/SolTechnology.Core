## SolTechnology.Core.HTTP

A thin, opinionated wrapper around `HttpClient` that bakes production-ready
resilience, observability, and ergonomics into outbound HTTP integrations. Drop
it into any ASP.NET Core / worker service and your typed clients ship with
retry + circuit breaker + per-attempt timeout, correlation propagation, a
fluent request builder, and a diagnostic exception type — no boilerplate, no
hand-rolled Polly pipelines, no leaking secrets into log sinks.

Design rationale lives in [ADR 005](adr/005-http-production-defaults.md);
before pointing this at production traffic, walk through the
[HTTP Production Checklist](HTTP-Production-Checklist.md).

### Features

- **Typed-client registration** — `services.AddHTTPClient<IFooClient, FooClient>("foo")`
  with optional strongly-typed options bound from configuration.
- **Resilience pipeline** — retry (exponential backoff + jitter) → circuit
  breaker → per-attempt timeout, built on
  `Microsoft.Extensions.Http.Resilience` / Polly v8. Optional outer
  `OverallRequestBudget` caps the total wall-clock per call.
- **Idempotent-only retry by default** — `POST` / `PATCH` are NOT retried
  unless you opt in with `RetryOnUnsafeVerbs`. No silent double-charges.
- **Recoverable-aware retry** — opt-in `RetryPredicate` inspects the response body
  and refuses retries when the upstream signals a non-recoverable business error
  (`"recoverable": false` in ProblemDetails). Built-in helper: `RetryPredicates.RecoverableOnly`.
- **`Retry-After` honoured** — 429 and 5xx with a `Retry-After` header back off
  exactly as the server asks, capped at `RetryTimeout`.
- **Correlation propagation** — `X-Correlation-Id` + W3C `traceparent` on every
  outbound request and every retry. Caller-supplied headers win.
- **Fluent request builder** — `httpClient.CreateRequest("/path").WithHeader(...).WithBody(...).GetAsync<T>()`,
  reusable across multiple terminal verbs.
- **Per-request policy override** — `CreateRequest(path, HttpPolicyConfiguration)`
  + `WithJsonOptions(...)` for one-off debug or migration scenarios.
- **Diagnostic exceptions** — `HttpRequestFailedException` carries method, URI,
  status, reason phrase. Response body capture is opt-in
  (`IncludeResponseBodyInException`) and bounded to 8 KiB so PII / tokens don't
  flow into logs through `Exception.Message`.
- **Metrics out of the box** — `Meter("SolTechnology.Core.HTTP")` with
  `retries` and `circuit_state_changes` counters.
- **Startup validation** — every option type uses `.ValidateOnStart()`, so bad
  config fails the host instead of the first production request.
- **`System.Text.Json` streaming** — serialize + deserialize without LOH
  pressure on large payloads. Avro supported via `DataType.Avro`.

### Registration

```csharp
services.AddHTTPClient<IFootballDataHTTPClient, FootballDataHTTPClient>("football-data");
```

The string `"football-data"` is the client name and must match the key under
`HTTPClients:` in `appsettings.json` when the configuration is bound from
`IConfiguration`. For a client with its own options section, use the
three-parameter overload — `GoogleHTTPOptions` is bound from
`HTTPClients:Google:Options`:

```csharp
services.AddHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
```

### Configuration

Two layers configured independently per client:

- **`HTTPClientConfiguration`** — base address, request timeout, default headers.
- **`HttpPolicyConfiguration`** — retry / circuit breaker / timeout policy.

```jsonc
{
  "HTTPClients": {
    "football-data": {
      "BaseAddress": "https://api.football-data.org",
      "TimeoutSeconds": 30,
      "Headers": [
        { "Name": "X-Auth-Token", "Value": "..." }
      ],
      "Policy": {
        "MaxRequestRetries": 2,
        "RequestTimeout": 15000
      }
    }
  },
  "HttpPolicy": {
    "MaxRequestRetries": 3,
    "CircuitBreakerFailureThreshold": 0.3
  }
}
```

Policy precedence (most specific wins):

1. Explicit `HttpPolicyConfiguration` passed to `AddHTTPClient`.
2. `HTTPClients:{name}:Policy` — per-client override.
3. `HttpPolicy` — global default for all clients.
4. Built-in defaults (table below).

For short scripts or unit tests, pass the configuration directly:

```csharp
services.AddHTTPClient<IFootballDataHTTPClient, FootballDataHTTPClient>(
    "football-data",
    new HTTPClientConfiguration
    {
        BaseAddress    = "https://api.football-data.org",
        TimeoutSeconds = 30,
        Headers        = [ new() { Name = "X-Auth-Token", Value = "..." } ]
    });
```

`HttpPolicyConfiguration` defaults:

| Field | Default | Notes |
|---|---:|---|
| `UsePolly` | `true` | Set to `false` to bypass the whole pipeline. Surfaced at startup as a `Warning`. |
| `RequestTimeout` | 30 000 ms | Per-attempt timeout. |
| `MaxRequestRetries` | 3 | Initial attempt + 3 retries. |
| `RetryInitialDelay` | 200 ms | Seed for the exponential-jitter sequence. |
| `RetryTimeout` | 30 000 ms | Upper bound on a single retry delay. |
| `CircuitBreakerFailureThreshold` | 0.3 | Ratio in `[0.0, 1.0]`. |
| `CircuitBreakerSamplingDuration` | 30 000 ms | Window over which the ratio is sampled. |
| `CircuitBreakerMinimumThroughput` | 10 | Minimum requests in the window before the breaker can trip. |
| `CircuitBreakerDelayDuration` | 10 000 ms | Time the breaker stays open before half-opening. |

Retried automatically: `408`, `429` (honours `Retry-After`), `500`, `502`,
`503` (honours `Retry-After`), `504`, plus `HttpRequestException` /
`TimeoutRejectedException` / `TaskCanceledException`.

### Usage

#### Inject and call

```csharp
public sealed class FootballDataHTTPClient(HttpClient httpClient) : IFootballDataHTTPClient
{
    public Task<MatchModel> GetMatchAsync(int id, CancellationToken ct = default) =>
        httpClient.CreateRequest($"v2/matches/{id}")
                  .WithResponseType(DataType.Json)
                  .GetAsync<MatchModel>(ct);
}
```

#### Fluent builder

```csharp
// GET typed
var match = await httpClient.CreateRequest("v2/matches/42")
    .WithResponseType(DataType.Json)
    .GetAsync<MatchModel>(cancellationToken);

// POST with body
var created = await httpClient.CreateRequest("v2/matches")
    .WithHeader("X-Idempotency-Key", idempotencyKey)
    .WithBody(payload)              // Content-Type: application/json
    .WithResponseType(DataType.Json)
    .PostAsync<MatchModel>(cancellationToken);

// Raw response — caller owns disposal
using var response = await httpClient.CreateRequest("v2/matches/42").GetAsync(cancellationToken);
```

Verbs supported: `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`,
`DeleteAsync`. Each has a typed (`<T>`) and untyped (`HttpResponseMessage`)
overload. The same builder is safe to reuse across multiple terminal verbs:

```csharp
var builder = httpClient.CreateRequest("v2/health").WithHeader("X-Probe", "true");
var head = await builder.GetAsync();   // call 1 — OK
var poke = await builder.PostAsync();  // call 2 — still OK
```

#### Per-request policy override

```csharp
var debugPolicy = new HttpPolicyConfiguration { IncludeResponseBodyInException = true };

var match = await httpClient
    .CreateRequest("v2/matches/42", debugPolicy)
    .GetAsync<MatchModel>(ct);
```

Typed clients should pull
`IOptionsMonitor<HttpPolicyConfiguration>.Get(clientName)` and pass it in
explicitly rather than constructing the override ad-hoc.

#### Handling failures

Any non-2xx response on a typed call throws `HttpRequestFailedException`:

```csharp
try
{
    var match = await httpClient.CreateRequest("v2/matches/42").GetAsync<MatchModel>();
}
catch (HttpRequestFailedException ex)
{
    logger.LogWarning(ex,
        "Upstream returned [{Status}] for [{Method}] [{Uri}]",
        ex.StatusCode, ex.Method, ex.RequestUri);
}
```

| Property | Value |
|---|---|
| `StatusCode` | `System.Net.HttpStatusCode` |
| `Method` | `HttpMethod` of the failing call |
| `RequestUri` | Absolute URI of the failing call |
| `ReasonPhrase` | Server-side reason phrase |
| `ResponseBody` | First 8 KiB of the body, opt-in; oversize bodies end with `… [response body truncated]` |

`HttpRequestFailedException` inherits from `HttpRequestException`, so existing
`catch (HttpRequestException)` handlers continue to work. `Exception.Message`
carries only metadata — the response body is exposed **only** via
`ResponseBody`, so tokens / PII do not leak into logging sinks.

#### Correlation propagation

Every outbound request automatically carries:

- `X-Correlation-Id` — sourced from the ambient `ICorrelationIdService`
  (provided by `SolTechnology.Core.Logging`). One id per logical call,
  preserved across retries.
- `traceparent` — full W3C Trace Context value built from `Activity.Current`,
  attached only when a real Activity is in scope.

Both headers use "caller wins" semantics — `WithHeader("X-Correlation-Id", "...")`
overrides the ambient value. Works equally well in background workers /
functions: the handler generates one on the first outbound call and persists it
for the rest of the async scope.

#### Observability

```csharp
services.AddOpenTelemetry()
    .WithMetrics(b => b
        .AddMeter("Polly")                       // resilience-pipeline metrics
        .AddMeter("SolTechnology.Core.HTTP"))    // retries / circuit_state_changes counters
    .WithTracing(b => b.AddHttpClientInstrumentation());
```

Each retry / circuit-breaker state transition is also logged at `Warning` via
the `ILogger<HttpPolicyFactory>` category. The resilience pipeline name is
`core-http-{httpClientName}` for filtering in dashboards.

### Testing

The companion package **`SolTechnology.Core.HTTP.Testing`** mocks typed clients in component tests: a
`WireMockFixture` plus a fluent `Fake<TClient>()` DSL keyed off your generated client interface — full
IntelliSense, compile-time argument checks, no hand-written URL matchers. Reference it from test projects
only. Full reference: [HTTP.Testing.md](HTTP.Testing.md).

> **Breaking migration:** this package replaces `SolTechnology.Core.Faker`. The namespace changed
> (`SolTechnology.Core.Faker` → `SolTechnology.Core.HTTP.Testing`), so there is no type-forwarding — swap
> the package reference **and** update `using` directives. See [ADR-008](adr/008-testing-framework-companions.md).

```csharp
// Arrange a fake via a DIRECT method call — full IntelliSense + compile-time argument checks.
wireMockFixture.Fake<IFootballDataHTTPClient>()
    .WithRequest(x => x.GetMatchAsync(42))
    .WithResponse(r => r.WithSuccess().WithBodyAsJson(new { id = 42 }));

var match = await sut.GetMatchAsync(42);
match.Id.Should().Be(42);
```

When you need to assert the **resilience pipeline** itself (retry / circuit breaker), drive the raw
WireMock server with a stateful scenario — it returns `503` then `200`, and you assert two attempts hit
the wire:

```csharp
[Test]
public async Task GetMatch_RetriesOnce_OnTransient503()
{
    // Arrange — first call 503, second 200 (the retry). Initialize() returns the underlying
    // WireMockStartup, whose WireMockServer drives raw stateful scenarios.
    var server = wireMockFixture.Initialize().WireMockServer;
    server.Given(Request.Create().WithPath("/v2/matches/42"))
          .InScenario("retry").WillSetStateTo("served")
          .RespondWith(Response.Create().WithStatusCode(503));
    server.Given(Request.Create().WithPath("/v2/matches/42"))
          .InScenario("retry").WhenStateIs("served")
          .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { id = 42 }));

    // Act
    var match = await sut.GetMatchAsync(42);

    // Assert
    match.Id.Should().Be(42);
    wireMockFixture.LogEntries.Should().HaveCount(2);
}
```

For unit-testing a typed client in isolation, inject a `HttpClient` built on a `HttpMessageHandler`
stub — bypass the resilience pipeline entirely and assert the request shape your client produces.

### Conventions

- **One typed client per upstream system.** Name it after the system
  (`GoogleHTTPClient`, `FootballDataHTTPClient`); the partial class lives in a
  folder with one method per file (`GoogleHTTPClient.GetLocationOfCity.cs`).
  See `ClaudeCodingGuide.md` §5.
- **Never leak transport types.** The interface returns domain models or DTOs,
  never `HttpResponseMessage`, `JObject`, or `Stream`.
- **Idempotency keys on `POST` / `PATCH`** if you opt into `RetryOnUnsafeVerbs`.
  The retry policy will not deduplicate for you.
- **`IncludeResponseBodyInException = true` only when you need it.** Bodies can
  contain PII; the default opt-out is intentional.
- **Catch `HttpRequestFailedException`, not `HttpRequestException`,** when you
  need the status / URI / body. The latter is the base type and still works for
  unconditional retries / circuit logging.
- **Per-attempt timeout < `OverallRequestBudget`.** Validation enforces this on
  startup; if you tune them, keep the gap large enough for at least one
  complete attempt.

### Recoverable-aware retry predicate

By default, the pipeline retries on transient status codes (408/429/5xx) regardless of the response
body. When the upstream uses `Core.Api`'s `ProblemDetails` with `"recoverable": true/false`, you can
opt in to body-aware retry — stop wasting retries on deterministic business rejections:

```csharp
services.AddHTTPClient<IPaymentsClient, PaymentsClient>(
    "Payments",
    new HTTPClientConfiguration { BaseAddress = "https://payments/" },
    new HttpPolicyConfiguration
    {
        RetryPredicate = RetryPredicates.RecoverableOnly
    });
```

**Semantics (restrict-only):**
- Predicate is called **only** when the standard checks (transient status + safe verb) already passed.
- `true` → allow the retry (body says recoverable, or unparseable — benefit of the doubt).
- `false` → **stop retrying** (body says non-recoverable — will never succeed on retry).
- A non-retryable status (400, 401, 403, 404) is **never** retried regardless of body content.
  The predicate cannot expand retries, only restrict them.

**Custom predicate** — use any logic:

```csharp
RetryPredicate = async response =>
{
    var body = await response.Content.ReadAsStringAsync();
    return !body.Contains("DUPLICATE_DETECTED");
}
```

### Typed service-call error taxonomy (`TryXxxAsync<T>`)

The existing `GetAsync<T>()` / `PostAsync<T>()` methods **throw** on failure. When you prefer
railway-style error handling (`Result<T>`), use the `Try` variants:

```csharp
var result = await httpClient.CreateRequest("/api/payments")
    .WithBody(payment)
    .TryPostAsync<PaymentResponse>();

if (result.IsFailure)
{
    // result.Error is a typed Core Error: NotFoundError, ValidationError, TimeoutError, etc.
    logger.LogWarning("Payment call failed: {Error}", result.Error.Message);
    return Result<PaymentResponse>.Fail(result.Error);
}

return Result<PaymentResponse>.Success(result.Data);
```

**Error mapping taxonomy (`ServiceCallErrorMapper`):**

| Failure | Mapped to | Recoverable? |
|---|---|---|
| Socket-level (DNS, connection refused) | `TimeoutError` | ✅ yes |
| Timeout (per-attempt or outer budget) | `TimeoutError` | ✅ yes |
| 404 | `NotFoundError` | ❌ no (heuristic) |
| 401 | `UnauthorizedError` | ❌ no |
| 403 | `ForbiddenError` | ❌ no |
| 400 / 422 | `ValidationError` | ❌ no |
| 409 | `ConflictError` | ❌ no |
| 408 / 504 | `TimeoutError` | ✅ yes |
| 5xx | `Error` | ✅ yes (heuristic) |
| Deserialization failure (bad JSON) | `Error` | ❌ no |

**ProblemDetails override:** if the response body contains `"recoverable": true/false`, it
**overrides** the status-code heuristic. The `title` field is extracted as the error `Message`.

**Additive:** the existing exception-throwing path (`GetAsync<T>()` → `HttpRequestFailedException`)
stays unchanged. `TryXxxAsync<T>` is an opt-in alternative — choose per call site.

### What ships in DI

`AddHTTPClient` (and the three-parameter overload) registers, per client name:

- The typed client (`TClient`) and its interface (`TIClient`), via
  `AddHttpClient<TIClient, TClient>`.
- A named `HttpClient` configured with the base address, timeout, and default
  headers from `HTTPClientConfiguration`.
- A named `HttpPolicyConfiguration` bound through `IOptionsMonitor<>`,
  validated on start.
- The shared `HttpPolicyFactory` and `HttpClientMetrics` singletons (idempotent
  `TryAddSingleton`).
- A resilience handler under pipeline name `core-http-{clientName}`.
- `CorrelationPropagatingHandler` (when `propagateCorrelation: true`, the
  default) inserted before the resilience handler so every retry carries the
  same correlation headers.

`TOptions` (when supplied) is bound from `HTTPClients:{name}:Options` and
available via `IOptions<TOptions>` / `IOptionsMonitor<TOptions>`.
