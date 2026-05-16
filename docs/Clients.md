### Overview

`SolTechnology.Core.HTTP` is a thin, opinionated wrapper around `HttpClient` that bakes production-ready resilience, observability, and ergonomics into outbound HTTP integrations. Drop it into any ASP.NET Core / worker service and you get:

> **Production reading**: before pointing this at production traffic, walk
> through the [HTTP Production Checklist](HTTP-Production-Checklist.md) —
> idempotency defaults, PII hygiene, timeout ownership, and the
> `SolTechnology.Core.HTTP` metrics contract. The design rationale lives in
> [ADR 005](adr/005-http-production-defaults.md).

| Feature | Out of the box |
|---|---|
| Typed-client registration | `services.AddHTTPClient<IFooClient, FooClient>("foo")` |
| Resilience pipeline | retry (exponential backoff + jitter) → circuit breaker → per-attempt timeout (Microsoft.Extensions.Http.Resilience / Polly v8); optional outer `OverallRequestBudget` |
| Idempotent-only retry | `POST` / `PATCH` are NOT retried by default — opt in with `RetryOnUnsafeVerbs` |
| `Retry-After` honouring | 429 + 5xx with a `Retry-After` header back off as the server asks (capped at `RetryTimeout`) |
| Correlation propagation | `X-Correlation-Id` + W3C `traceparent` attached to every outbound request (and every retry) — additive-only, opt-out per client |
| Fluent request builder | `httpClient.CreateRequest("/path").WithHeader(...).WithBody(...).GetAsync<T>()` |
| Per-request overrides | `CreateRequest(path, HttpPolicyConfiguration)` + `WithJsonOptions(...)` |
| Diagnostic exceptions | `HttpRequestFailedException` carries method/URI/status; body capture is opt-in (`IncludeResponseBodyInException`) |
| Metrics | `Meter("SolTechnology.Core.HTTP")` with `retries` / `circuit_state_changes` counters |
| Startup validation | `.ValidateOnStart()` on every option type — bad config fails the host, not the first request |
| System.Text.Json | streaming serialize + deserialize, no LOH pressure on large payloads |

---

### Registration

Reference the **SolTechnology.Core.HTTP** NuGet package and register your typed clients in `Program.cs`:

```csharp
services.AddHTTPClient<IFootballDataHTTPClient, FootballDataHTTPClient>("football-data");
```

The string `"football-data"` is the client name. It must match the key under `HTTPClients:` in `appsettings.json` (when the configuration is resolved from `IConfiguration`).

If your client needs strongly-typed options bound from configuration, use the three-parameter overload:

```csharp
services.AddHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
```

`GoogleHTTPOptions` is bound from the `HTTPClients:Google:Options` section.

---

### Configuration

Two layers can be configured independently per client:

1. **`HTTPClientConfiguration`** — base address, request timeout, default headers.
2. **`HttpPolicyConfiguration`** — retry / circuit breaker / timeout policy.

#### 1) `appsettings.json` — the recommended path

```json
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

**Policy precedence (most specific wins):**

1. Explicit `HttpPolicyConfiguration` passed to `AddHTTPClient`.
2. `HTTPClients:{name}:Policy` — per-client override.
3. `HttpPolicy` — global default for all clients.
4. Built-in defaults (see below).

#### 2) Parameter-based — for short scripts or unit tests

```csharp
var configuration = new HTTPClientConfiguration
{
    BaseAddress = "https://api.football-data.org",
    TimeoutSeconds = 30,
    Headers = new List<Header>
    {
        new() { Name = "X-Auth-Token", Value = "..." }
    }
};

services.AddHTTPClient<IFootballDataHTTPClient, FootballDataHTTPClient>(
    "football-data", configuration);
```

#### Built-in policy defaults

`HttpPolicyConfiguration` is validated at first resolve (`OptionsValidationException` on misconfiguration). Defaults:

| Field | Default | Notes |
|---|---:|---|
| `UsePolly` | `true` | set to `false` to bypass the whole pipeline |
| `RequestTimeout` | 30 000 ms | per-attempt timeout |
| `MaxRequestRetries` | 3 | initial attempt + 3 retries |
| `RetryInitialDelay` | 200 ms | seed for the exponential-jitter sequence |
| `RetryTimeout` | 30 000 ms | upper bound on a single retry delay |
| `CircuitBreakerFailureThreshold` | 0.3 | ratio in [0.0, 1.0] |
| `CircuitBreakerSamplingDuration` | 30 000 ms | window over which the ratio is sampled |
| `CircuitBreakerMinimumThroughput` | 10 | minimum requests in the window before the breaker can trip |
| `CircuitBreakerDelayDuration` | 10 000 ms | time the breaker stays open before half-opening |

Retried automatically: `408 Request Timeout`, `429 Too Many Requests` (honours `Retry-After`), `500`, `502`, `503` (also honours `Retry-After`), `504`, plus `HttpRequestException` / `TimeoutRejectedException` / `TaskCanceledException`.

---

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

#### Fluent builder API

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

Verbs supported: `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync` — each with both typed (`<T>` deserialized) and untyped (`HttpResponseMessage`) overloads. Avro is also supported via `DataType.Avro` on both request and response sides.

The same builder instance is safe to reuse across multiple terminal verbs:

```csharp
var builder = httpClient.CreateRequest("v2/health").WithHeader("X-Probe", "true");
var head = await builder.GetAsync();   // call 1 — OK
var poke = await builder.PostAsync();  // call 2 — still OK
```

---

### Handling failures

Any non-2xx response on a typed call throws `HttpRequestFailedException`:

```csharp
try
{
    var match = await httpClient.CreateRequest("v2/matches/42").GetAsync<MatchModel>();
}
catch (HttpRequestFailedException ex)
{
    // ex.StatusCode    — System.Net.HttpStatusCode
    // ex.Method        — HttpMethod
    // ex.RequestUri    — absolute URI of the failing call
    // ex.ReasonPhrase  — server-side reason phrase
    // ex.ResponseBody  — first 8 KiB of the body (best-effort, truncated on overflow)
    logger.LogWarning(ex,
        "Upstream returned {Status} for {Method} {Uri}",
        ex.StatusCode, ex.Method, ex.RequestUri);
}
```

Notes:

- `HttpRequestFailedException` inherits from `HttpRequestException`, so existing `catch (HttpRequestException)` handlers continue to work.
- `Exception.Message` carries only metadata. The response body is exposed **only** via `ResponseBody` so tokens / PII do not leak into logging sinks.
- The body is captured up to 8 KiB; oversize bodies end with `… [response body truncated]`.

---

### Correlation propagation

Every outbound request automatically carries:

- `X-Correlation-Id` — sourced from the ambient `ICorrelationIdService` (provided by `SolTechnology.Core.Logging`). One id per logical call, preserved across retries.
- `traceparent` — full W3C Trace Context value built from `Activity.Current`, attached **only** when a real Activity is in scope. Compatible with OpenTelemetry, Application Insights, Datadog, and the rest of the W3C-aware ecosystem.

Both headers are added with "caller wins" semantics — a `WithHeader("X-Correlation-Id", "...")` override is honoured.

Pairs naturally with `Core.Logging.AddCoreLogging()` on the inbound side: the same id flows through the request scope and onto every downstream call. Works equally well in background workers / functions — the handler generates one on the first outbound call and persists it for the rest of the async scope.

---

### Observability

`Microsoft.Extensions.Http.Resilience` tags every resilience event with the pipeline name `core-http-{httpClientName}`. OpenTelemetry consumers wire it up via:

```csharp
services.AddOpenTelemetry()
    .WithMetrics(b => b.AddMeter("Polly"))     // resilience-pipeline metrics
    .WithTracing(b => b.AddHttpClientInstrumentation());
```

Each retry / circuit-breaker state transition is also logged at `Warning` level via the `ILogger<HttpPolicyFactory>` category.

---

### Version & compatibility

- TFM: `net10.0`.
- Depends on: `Microsoft.AspNetCore.App` (shared framework — uses the Options / DI / Configuration / Logging stacks shipped with ASP.NET Core), `Microsoft.Extensions.Http.Resilience` 10.x, `Polly` 8.x, `SolTechnology.Core.Logging`, `AvroConvert`.
- Public API is at `0.x` — breaking changes (notably the planned `HTTP→Http` naming pass) are tracked in `docs/reviews/HTTP-Review.md` and gated on a `1.0.0` release.
