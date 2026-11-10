## SolTechnology.Core.HTTP

Typed `HttpClient` registration with a production-ready resilience pipeline, correlation
propagation, a fluent request builder, and a diagnostic exception type. Retry, circuit breaker and
per-attempt timeout are wired for you, and upstream payloads stay out of log sinks by default.

Design rationale lives in [HTTP client architecture](architecture/http-client.md);
before pointing this at production traffic, walk through the
[HTTP Production Checklist](HTTP-Production-Checklist.md).

### Features

- **Typed-client registration** — `services.AddSolHTTPClient<IFooClient, FooClient>("foo")`
  with optional strongly-typed options bound from configuration.
- **Resilience pipeline** — retry (exponential backoff + jitter) → circuit
  breaker → per-attempt timeout, built on
  `Microsoft.Extensions.Http.Resilience` / Polly v8. Optional outer
  `OverallRequestBudget` caps the total wall-clock per call.
- **Idempotent-only retry by default** — `POST` / `PATCH` are NOT retried, not even after a
  connection error or a timeout, unless you opt in with `RetryOnUnsafeVerbs`. No silent double-charges.
- **Recoverable-aware retry** — opt-in `RetryPredicate` inspects the response body
  and refuses retries when the upstream signals a non-recoverable business error
  (`"recoverable": false` in ProblemDetails). Built-in helper: `RetryPredicates.RecoverableOnly`.
- **`Retry-After` honoured** — 429 and 5xx with a `Retry-After` header back off
  exactly as the server asks, capped at `RetryTimeout`.
- **Correlation propagation** — `X-Correlation-Id` + W3C `traceparent` on every
  outbound request and every retry. Caller-supplied headers win.
- **Fluent request builder** — `httpClient.CreateRequest("/path").WithHeader(...).WithBody(...).Get<T>()`,
  reusable across multiple terminal verbs, with `Result<T>`-returning `Try*` verbs.
- **Diagnostic exceptions** — `HttpRequestFailedException` carries method, URI,
  status, reason phrase. Response body capture is opt-in per request
  (`IncludeResponseBodyInException`) and bounded to 8 KiB so PII / tokens don't
  flow into logs through `Exception.Message`.
- **Metrics and startup validation** — `Meter("SolTechnology.Core.HTTP")` with `retries` and
  `circuit_state_changes` counters; every option type is validated with `ValidateOnStart()`.

### Registration

```csharp
services.AddSolHTTPClient<IFootballDataHTTPClient, FootballDataHTTPClient>("football-data");
```

The string `"football-data"` is the client name and must match the key under
`HTTPClients:` in `appsettings.json` when the configuration is bound from
`IConfiguration`. For a client with its own options section, use the
three-type-parameter overload — `GoogleHTTPOptions` is bound from
`HTTPClients:Google:Options` and validated with its data annotations at start:

```csharp
services.AddSolHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
```

### Configuration

Two layers configured independently per client:

- **`HTTPClientConfiguration`** — base address, `HttpClient` timeout (used only without Polly), default headers.
- **`HttpPolicyConfiguration`** — retry / circuit breaker / timeout policy.

```jsonc
{
  "HTTPClients": {
    "football-data": {
      "BaseAddress": "https://api.football-data.org",
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

1. Explicit `HttpPolicyConfiguration` passed to `AddSolHTTPClient`.
1. `HTTPClients:{name}:Policy` — per-client override.
1. `HttpPolicy` — global default for all clients.
1. Built-in defaults (table below).

For short scripts or unit tests, pass the configuration directly:

```csharp
services.AddSolHTTPClient<IFootballDataHTTPClient, FootballDataHTTPClient>(
    "football-data",
    new HTTPClientConfiguration
    {
        BaseAddress = "https://api.football-data.org",
        Headers     = [new() { Name = "X-Auth-Token", Value = "..." }]
    });
```

`HttpPolicyConfiguration` defaults (durations in milliseconds). Ranges follow Polly 8 and are
validated at host start, so a value Polly would reject on the first request stops the host instead:

| Field | Default | Notes |
|---|---:|---|
| `UsePolly` | `true` | Set to `false` to bypass the whole pipeline. Surfaced at startup as a `Warning`. |
| `RequestTimeout` | 10 000 | Per-attempt timeout; 10 ms to one day. |
| `MaxRequestRetries` | 2 | Initial attempt + 2 retries; 0 to 100. `0` sends once and keeps the circuit breaker. |
| `RetryInitialDelay` | 200 | Seed for the exponential-jitter sequence; 0 to one day. |
| `RetryTimeout` | 30 000 | Upper bound on a single retry delay and on `Retry-After`; 1 ms to one day. |
| `RetryOnUnsafeVerbs` | `false` | `POST` / `PATCH` / `CONNECT` are never retried unless `true`. |
| `RetryPredicate` | `null` | Optional body-aware restriction; see below. |
| `OverallRequestBudget` | `null` | Optional outer budget for the whole call; must exceed `RequestTimeout`, at most one day. |
| `CircuitBreakerFailureThreshold` | 0.3 | Ratio in `[0.0, 1.0]`. |
| `CircuitBreakerSamplingDuration` | 10 000 | Window over which the ratio is sampled; 500 ms to one day. |
| `CircuitBreakerMinimumThroughput` | 5 | Minimum requests in the window before the breaker can trip; at least 2. |
| `CircuitBreakerDelayDuration` | 10 000 | Time the breaker stays open before half-opening; 500 ms to one day. |
| `IncludeResponseBodyInException` | `false` | Honoured per request only; see [per-request policy](#per-request-policy). |

Retried automatically for safe and idempotent verbs: `408`, `429` (honours `Retry-After`), `500`,
`502`, `503` (honours `Retry-After`), `504`, plus `HttpRequestException`,
`TimeoutRejectedException`, and any other cancellation the caller did not request. A cancellation
of the caller's own token is neither retried nor counted by the circuit breaker.

### Usage

#### Inject and call

```csharp
public sealed class FootballDataHTTPClient(HttpClient httpClient) : IFootballDataHTTPClient
{
    public Task<MatchModel> GetMatch(int id, CancellationToken cancellationToken = default) =>
        httpClient.CreateRequest($"v2/matches/{id}")
                  .WithResponseType(DataType.Json)
                  .Get<MatchModel>(cancellationToken);
}
```

#### Fluent builder

```csharp
// GET typed
var match = await httpClient.CreateRequest("v2/matches/42")
    .WithResponseType(DataType.Json)
    .Get<MatchModel>(cancellationToken);

// POST with body
var created = await httpClient.CreateRequest("v2/matches")
    .WithHeader("X-Idempotency-Key", idempotencyKey)
    .WithBody(payload)              // Content-Type: application/json
    .WithResponseType(DataType.Json)
    .Post<MatchModel>(cancellationToken);

// Raw response — caller owns disposal
using var response = await httpClient.CreateRequest("v2/matches/42").Get(cancellationToken);
```

Verbs supported: `Get`, `Post`, `Put`, `Patch`, `Delete`. Each has a typed (`<T>`) and an
untyped (`HttpResponseMessage`) overload, and the typed ones have `TryGet<T>` … `TryDelete<T>`
counterparts that return `Result<T>`. The same builder is safe to reuse across multiple terminal verbs:

```csharp
var builder = httpClient.CreateRequest("v2/health").WithHeader("X-Probe", "true");
using var first = await builder.Get();    // call 1 — OK
using var second = await builder.Post();  // call 2 — still OK
```

`WithJsonOptions(...)` replaces the default `JsonSerializerOptions` (original casing on write,
case-insensitive read) for one request.

#### Per-request policy

`IncludeResponseBodyInException` is read from the policy passed to `CreateRequest(path, policy)`;
the named client's policy does not reach a request created without it. It is the only per-request
field: retry, circuit breaker and timeouts always follow the named client's policy.

```csharp
var debugPolicy = new HttpPolicyConfiguration { IncludeResponseBodyInException = true };

var match = await httpClient
    .CreateRequest("v2/matches/42", debugPolicy)
    .Get<MatchModel>(cancellationToken);
```

To honour the configured flag of a named client, pass its policy from `IOptionsMonitor`:

```csharp
public sealed class FootballDataHTTPClient(
    HttpClient httpClient,
    IOptionsMonitor<HttpPolicyConfiguration> policies) : IFootballDataHTTPClient
{
    public Task<MatchModel> GetMatch(int id, CancellationToken cancellationToken = default) =>
        httpClient.CreateRequest($"v2/matches/{id}", policies.Get("football-data"))
                  .Get<MatchModel>(cancellationToken);
}
```

#### Handling failures

Any non-2xx response on a typed call throws `HttpRequestFailedException`:

```csharp
try
{
    var match = await httpClient.CreateRequest("v2/matches/42").Get<MatchModel>(cancellationToken);
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
| `ResponseBody` | First 8 KiB of the body, opt-in per request; oversize bodies end with `… [response body truncated]` |

`HttpRequestFailedException` inherits from `HttpRequestException`, so existing
`catch (HttpRequestException)` handlers continue to work. `Exception.Message`
carries only metadata — the response body is exposed **only** via
`ResponseBody`, so tokens / PII do not leak into logging sinks.

#### Result-returning verbs (`Try*`)

`Get<T>()` / `Post<T>()` throw on failure; the `Try` variants return `Result<T>` with a typed Core error:

```csharp
var result = await httpClient.CreateRequest("/api/payments")
    .WithBody(payment)
    .TryPost<PaymentResponse>(cancellationToken);

if (result.IsFailure)
{
    logger.LogWarning("Payment call failed: [{Error}]", result.Error!.Message);
    return Result<PaymentResponse>.Fail(result.Error);
}

return Result<PaymentResponse>.Success(result.Data!);
```

A cancellation requested by the caller's token still throws `OperationCanceledException`.
`ServiceCallErrorMapper` maps every other failure:

| Failure | Mapped to | Recoverable? |
|---|---|---|
| Socket-level (DNS, connection refused) | `TimeoutError` | ✅ yes |
| Timeout (per-attempt, outer budget, or `HttpClient.Timeout`) | `TimeoutError` | ✅ yes |
| Open circuit (`BrokenCircuitException`) | `Error` "Upstream circuit open" | ✅ yes |
| 404 | `NotFoundError` | ❌ no (heuristic) |
| 401 | `UnauthorizedError` | ❌ no |
| 403 | `ForbiddenError` | ❌ no |
| 400 / 422 | `ValidationError` | ❌ no |
| 409 | `ConflictError` | ❌ no |
| 408 / 504 | `TimeoutError` | ✅ yes |
| 5xx | `Error` | ✅ yes (heuristic) |
| Deserialization failure (bad JSON) | `Error` | ❌ no |

If a JSON-object response body contains `"recoverable": true/false`, it overrides the status-code
heuristic, and its `title` (else `message`, else `detail`) string becomes the error `Message`.

#### Recoverable-aware retry predicate

Stop spending retries on deterministic business rejections when the upstream uses `Core.Api`'s
`ProblemDetails` with `"recoverable": true/false`:

```csharp
services.AddSolHTTPClient<IPaymentsClient, PaymentsClient>(
    "Payments",
    new HTTPClientConfiguration { BaseAddress = "https://payments/" },
    new HttpPolicyConfiguration
    {
        RetryPredicate = RetryPredicates.RecoverableOnly
    });
```

| Predicate outcome | Effect |
|---|---|
| Not called | The status or exception is not transient, the verb is unsafe, or it is the last attempt. |
| Skipped | The body is larger than 8 KiB; the transient status decides. |
| `true` | Retry (body says recoverable, or is not a JSON object — benefit of the doubt). |
| `false` | Stop retrying (body says non-recoverable). |

The predicate restricts retries only; a `400`, `401`, `403` or `404` is never retried. The body is
buffered (at most 8 KiB) before the call, so a custom predicate may read it:

```csharp
RetryPredicate = async response =>
{
    var body = await response.Content.ReadAsStringAsync();
    return !body.Contains("DUPLICATE_DETECTED");
}
```

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

Each retry / circuit-breaker state transition is also logged at `Warning` under the
`SolTechnology.Core.HTTP` logger categories; the retry entry and the "Circuit breaker opened for
[{ClientName}]" entry carry the triggering exception as the log entry's exception. The resilience pipeline name is `core-http-{httpClientName}` for
filtering in dashboards.

#### Chronicle

`AddSolHTTPClient` registers [Chronicle](Log.md#chronicle): every call inside a root is one step
`HTTP {Method} {client}` with attributes `Host`, `StatusCode` and `Attempts`, however many retries
the resilience pipeline made.

| Call outcome | Step status |
|---|---|
| 2xx–4xx response | `Ok` (a 4xx is often expected; see `StatusCode`) |
| 5xx final response | `Failed` (`HTTP500`, …) |
| Exception in the pipeline (timeout, broken circuit, connection failure) | `Failed`, with the exception type |
| `HttpClient.Timeout` elapsed (`UsePolly = false` with `TimeoutSeconds`) | `Failed` (`TimeoutException`) |
| Caller cancellation | `Cancelled` |

The path and query are never recorded, because they can carry personal data. Calls in a loop
aggregate per client and method, so the N+1 hint also catches chatty HTTP. Clients registered with
`propagateCorrelation: false` are recorded too. The steps are logical spans around `HttpClient`'s
own spans.

#### Upstream service health check

Subclass `BaseUpstreamServiceHealthCheck<TReport>` to probe a downstream's `/health` endpoint
through a named client registered with `AddSolHTTPClient`:

```csharp
public sealed record PartnerHealth(string Status);

public sealed class PartnerHealthCheck(IHttpClientFactory httpClientFactory, ILogger<PartnerHealthCheck> logger)
    : BaseUpstreamServiceHealthCheck<PartnerHealth>(
        httpClientFactory, "partner", "/health", new UpstreamHealthCheckOptions(), logger)
{
    protected override HealthCheckResult EvaluateReport(PartnerHealth report) =>
        report.Status == "ok"
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"partner status: {report.Status}");
}

// "partner" is the client name used in AddSolHTTPClient<IPartnerClient, PartnerClient>("partner").
builder.Services.AddHealthChecks()
    .AddSolUpstreamHttpHealthCheck<PartnerHealthCheck>("partner");
```

| Outcome | Status |
|---|---|
| 2xx + valid report | mapped via your `EvaluateReport` |
| 2xx + `null` / bad JSON body | `Degraded` (upstream reachable, payload broken) |
| Non-2xx or connection failure | registration's failure status (default `Unhealthy`) |
| Per-call timeout | registration's failure status (default `Unhealthy`) |
| Caller-cancellation | Rethrows |

Every result, healthy or not, is cached per upstream for `UpstreamHealthCheckOptions.CacheDuration`
(default 30 s), so a flurry of `/healthz` hits does not hammer the downstream. The check is a
singleton that creates the named client from `IHttpClientFactory` on every probe, so DNS changes are
picked up. The per-call `Timeout` (default 10 s) is independent of the cache window.

### Testing

The companion package **`SolTechnology.Core.HTTP.Testing`** mocks typed clients in component tests: a
`WireMockFixture` plus a fluent `Fake<TClient>()` DSL keyed off your generated client interface — full
IntelliSense, compile-time argument checks, no hand-written URL matchers. Reference it from test projects
only. Full reference: [HTTP.Testing.md](HTTP.Testing.md). It replaces `SolTechnology.Core.Faker`
(namespace `SolTechnology.Core.HTTP.Testing`, no type forwarding); see the
[testing companions feature record](features/2026-05-30-testing-framework-companions.md).

```csharp
// Arrange a fake via a DIRECT method call — full IntelliSense + compile-time argument checks.
wireMockFixture.Fake<IFootballDataHTTPClient>()
    .WithRequest(x => x.GetMatch(42, CancellationToken.None))
    .WithResponse(r => r.WithSuccess().WithBodyAsJson(new { id = 42 }));

var match = await sut.GetMatch(42);
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
    var match = await sut.GetMatch(42);

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

### What ships in DI

`AddSolHTTPClient` (and the three-type-parameter overload) registers, per client name:

- The typed client (`TClient`) and its interface (`TIClient`), via
  `AddHttpClient<TIClient, TClient>`.
- A named `HttpClient` configured with the base address, timeout, and default
  headers from `HTTPClientConfiguration`, validated on start (`BaseAddress` is required).
- A named `HttpPolicyConfiguration` bound through `IOptionsMonitor<>`,
  validated on start.
- The shared policy factory and `HttpClientMetrics` singletons (idempotent
  `TryAddSingleton`).
- A resilience handler under pipeline name `core-http-{clientName}`.
- `CorrelationPropagatingHandler` (when `propagateCorrelation: true`, the
  default) inserted before the resilience handler so every retry carries the
  same correlation headers.

`TOptions` (when supplied) is bound from `HTTPClients:{name}:Options`, validated with its data
annotations on start, and available via `IOptions<TOptions>` / `IOptionsMonitor<TOptions>`.
