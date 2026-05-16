# HTTP — Production Checklist

This page is the operator-facing companion to [Clients.md](Clients.md). It
captures the decisions a team must make consciously before pointing
`SolTechnology.Core.HTTP` at production traffic, and the meter / log surface
operators can rely on once it is running.

## 1. Production defaults at a glance

The defaults below ship in `0.7.0`. They are conservative-but-realistic; for
specific endpoints (long-running search, file uploads, low-traffic
service-to-service) tune per-client via `HTTPClients:{name}:Policy`.

| Setting                                | Default       | Notes |
| -------------------------------------- | ------------- | ----- |
| `UsePolly`                             | `true`        | Disabling removes retry + breaker + per-attempt timeout. Emits a startup `LogWarning`. |
| `MaxRequestRetries`                    | `2`           | 2 retries → 3 attempts total. Worst-case wall-clock = 3 × `RequestTimeout`. |
| `RetryOnUnsafeVerbs`                   | `false`       | `POST` / `PATCH` / `CONNECT` are NOT retried. See §3. |
| `RetryInitialDelay`                    | `200 ms`      | Exponential + jitter. |
| `RetryTimeout`                         | `30 000 ms`   | Cap on a single backoff delay AND cap on `Retry-After`. |
| `RequestTimeout`                       | `10 000 ms`   | Per-attempt timeout. Polly owns time when `UsePolly=true`. |
| `OverallRequestBudget`                 | `null` (off)  | Optional outer budget. Bounds the full logical call. See §4. |
| `CircuitBreakerMinimumThroughput`      | `5`           | Polly floor is 2. Below this, the breaker cannot trip. |
| `CircuitBreakerSamplingDuration`       | `10 000 ms`   | Window for the failure-ratio computation. |
| `CircuitBreakerFailureThreshold`       | `0.3`         | 30 % failure ratio over the window. |
| `CircuitBreakerDelayDuration`          | `10 000 ms`   | Break duration before half-open probe. |
| `IncludeResponseBodyInException`       | `false`       | Body capture is opt-in. See §2. |

## 2. PII / secret hygiene — `IncludeResponseBodyInException`

Upstream response bodies routinely contain PII, tokens or partner-confidential
payloads. Default formatters used by Serilog, Application Insights, and Sentry
serialise the full exception state — so anything attached to an exception
object reaches every log sink.

- **Production default**: `IncludeResponseBodyInException = false`.
  `HttpRequestFailedException.ResponseBody` will be `null`; the metadata-only
  `Message` (verb, URI, status, reason phrase) remains.
- **Dev / staging**: opt in per client (or per request via the `CreateRequest(path, policy)`
  overload) when you need a structured-error payload for debugging.
- `HttpRequestFailedException.ToString()` deliberately excludes `ResponseBody`
  even when populated — a belt-and-braces guard for log formatters that
  re-stringify exceptions.

## 3. Idempotency — `RetryOnUnsafeVerbs`

`GET` / `HEAD` / `OPTIONS` / `PUT` / `DELETE` are retried automatically. `POST`
/ `PATCH` / `CONNECT` are NOT retried by default — a retried `POST` after a
network-induced 5xx or timeout can duplicate side effects upstream (booking,
charge, email send).

Override per-client only when the endpoint is documented as idempotent (e.g.
guarded by an `Idempotency-Key` header) or your caller has its own
deduplication:

```json
"HTTPClients": {
  "payments": {
    "BaseAddress": "https://payments.example/",
    "Policy": {
      "RetryOnUnsafeVerbs": true
    }
  }
}
```

The circuit breaker still observes failed `POST` / `PATCH` attempts — a
5xx-storm on writes will trip the breaker for subsequent reads.

## 4. Outer time budget — `OverallRequestBudget`

By default the only deadline on a logical call is the per-attempt
`RequestTimeout` × number of attempts (so worst-case = `(MaxRequestRetries + 1)
× RequestTimeout`). For latency-sensitive callers, set `OverallRequestBudget`
to a hard ceiling — the pipeline will raise `Polly.Timeout.TimeoutRejectedException`
even if retries are still pending.

Validation enforces `OverallRequestBudget > RequestTimeout` so at least one
full attempt fits inside the budget.

## 5. Timeout ownership

When `UsePolly=true` (the default), Polly is the **single** timeout owner:
`HttpClient.Timeout` is set to `Timeout.InfiniteTimeSpan`. The legacy
`HTTPClientConfiguration.TimeoutSeconds` setting is ignored and a
`LogWarning` is emitted at client construction time so the misconfiguration is
visible in startup logs.

When `UsePolly=false`, `TimeoutSeconds` is honoured on `HttpClient.Timeout` as
the only remaining deadline.

## 6. Correlation propagation — opt-out

`AddHTTPClient(..., propagateCorrelation: true)` (default) inserts
`CorrelationPropagatingHandler` before the resilience handler so every retry
attempt carries the same `X-Correlation-Id` / `traceparent`. The handler is
**additive-only**: if the caller (or another middleware) already attached
`X-Correlation-Id`, neither the header nor the ambient `ICorrelationIdService`
AsyncLocal store is touched.

Set `propagateCorrelation: false` only when your host owns correlation
end-to-end (OpenTelemetry baggage propagator, firm middleware) and you want
this library to keep its hands off the wire and the AsyncLocal store.

## 7. Observability

Stable meter: `SolTechnology.Core.HTTP`. Instrument names are a documented
contract — they will not change without a MAJOR bump.

| Instrument                                       | Type    | Tags                                            |
| ------------------------------------------------ | ------- | ----------------------------------------------- |
| `soltechnology.core.http.retries`                | Counter | `client.name`, `http.method`, `outcome`         |
| `soltechnology.core.http.circuit_state_changes`  | Counter | `client.name`, `state` (open / half-open / closed) |

Wire your preferred OpenTelemetry / .NET metrics exporter against the meter
name. Existing structured logs from `OnRetry` / `OnOpened` / `OnClosed` /
`OnHalfOpened` are still emitted on the `SolTechnology.Core.HTTP` logger
category so log-based monitoring works without metrics.

## 8. Validation at startup

`AddHTTPClient` wires `.ValidateDataAnnotations()`, cross-field validation for
`OverallRequestBudget`, and `.ValidateOnStart()` on both option types.
Mis-configuration surfaces as `OptionsValidationException` at host startup —
not on the first production request after a deploy passes its health-check.

## 9. Per-request `JsonSerializerOptions`

The default JSON behaviour (preserve property casing on write, case-insensitive
read) matches the previous Newtonsoft-compatible contract. For endpoints
requiring camelCase, polymorphism, or custom converters, override per request:

```csharp
var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
await httpClient.CreateRequest("v1/orders")
    .WithJsonOptions(options)
    .WithBody(order)
    .PostAsync<OrderConfirmation>();
```

## 10. Disposing raw responses

`RequestBuilder.GetAsync()` / `PostAsync()` (untyped overloads) return
`HttpResponseMessage`. They are annotated `[MustDisposeResource]` — Rider /
ReSharper / JetBrains analysers will flag callers that drop the message.
Always wrap in `using`:

```csharp
using var response = await httpClient.CreateRequest("/x").GetAsync(ct);
```

A missed `using` leaks a connection-pool slot until GC.

