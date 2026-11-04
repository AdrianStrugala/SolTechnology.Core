# HTTP Client

`AddSolHTTPClient<TInterface, TClient>()` registers typed HTTP clients with production-oriented
resilience defaults. Polly is enabled by default. Correlation propagation is enabled by default
and can be disabled explicitly. `HttpClientExtensions.CreateRequest` and the `RequestBuilder` verbs
live in the `SolTechnology.Core.HTTP` namespace, not in `System.Net.Http`, so the library never
extends a BCL namespace; the verbs carry no `Async` suffix (`Get`, `TryGet<T>`).

## Resilience defaults

| Setting | Default | Accepted range |
|---|---|---|
| Attempts | 3 total | 0 to 100 retries |
| Initial retry delay | 200 ms | 0 ms to one day |
| Maximum retry delay | 30 s | 1 ms to one day |
| Per-attempt timeout | 10 s | 10 ms to one day |
| Overall request budget | Disabled | Above the per-attempt timeout, at most one day |
| Circuit failure ratio | 0.3 | 0.0 to 1.0 |
| Circuit sampling duration | 10 s | 500 ms to one day |
| Circuit minimum throughput | 5 | At least 2 |
| Circuit break duration | 10 s | 500 ms to one day |
| Unsafe-method retries | Disabled | |
| Response-body capture | Disabled | |

The ranges are Polly 8's own limits, validated at host start: a value Polly would reject when it
builds the pipeline on the first request stops the host instead. Zero retries leaves the retry
strategy out of the pipeline, because Polly does not accept zero attempts; the circuit breaker and
timeouts still apply.

Retries cover `408`, `429`, `500`, `502`, `503`, `504`, network failures, and timeouts. `POST`,
`PATCH`, and `CONNECT` are never retried — not after a status, a network failure, or a timeout —
unless unsafe retries are explicitly enabled, because a replay can duplicate side effects
upstream. The method comes from the request message stored in the resilience context, since an
exception outcome has no response. Their failures still count towards the circuit breaker. A
`RetryPredicate` may veto otherwise retryable responses but cannot expand the default set; the body
it inspects is buffered only up to 8 KiB with the request's token, and never on the last attempt,
whose response goes to the caller unread. `Retry-After` is honored up to the configured retry
timeout.

A cancellation of the caller's own token is not an upstream failure: it is neither retried nor
counted by the circuit breaker, and it records no retry log or metric. Any other cancellation is a
timeout and is treated as transient.

With Polly enabled, `HttpClient.Timeout` is infinite and Polly owns deadlines. Without Polly,
`TimeoutSeconds` controls `HttpClient.Timeout`. Backoff consumes time in addition to individual
attempts, so configure `OverallRequestBudget` when total latency must be bounded.

## Errors

The throwing verbs raise `HttpRequestFailedException` for a non-success status. The `Try*` verbs
return `Result<T>` and map failures through `ServiceCallErrorMapper`: timeouts (per-attempt, overall
budget, or `HttpClient.Timeout`) and socket failures are recoverable `TimeoutError`s, an open
circuit is a recoverable "Upstream circuit open" error because a later call may succeed, and status
codes map onto the Core error subtypes, with a ProblemDetails `recoverable` field overriding the
status heuristic. Only the caller's own cancellation is rethrown. Reading an error body tolerates
transport failures only; any other exception propagates.

## Diagnostics and safety

Base address, annotations, ranges, and timeout relationships of every option type, including the
custom options of `AddSolHTTPClient<TInterface, TClient, TOptions>`, are validated on host start.

Response-body capture is opt-in, capped at 8 KiB, pooled, and excluded from exception `Message` and
`ToString()` to reduce accidental disclosure. It is honoured per request only: the policy passed to
`CreateRequest(path, policy)` travels in the request options and is read when the exception is
built, while retry, circuit breaker, and timeouts always follow the named client's policy. Flowing
the named client's flag through a `DelegatingHandler` was the alternative; per-request only was
chosen for simplicity, and a typed client that wants its configured flag passes
`IOptionsMonitor<HttpPolicyConfiguration>.Get(name)` to `CreateRequest`.

`BaseUpstreamServiceHealthCheck<TReport>` probes an upstream through `IHttpClientFactory` and the
client's name on every probe, so it gets the client's handlers and picks up DNS changes without
capturing an `HttpClient` in the singleton check. It disposes every response and caches each result
for the configured duration.

Correlation propagation preserves an existing `X-Correlation-Id`; otherwise it adds the ambient
correlation value and `traceparent` when available. Stable metrics use meter
`SolTechnology.Core.HTTP`: the retry counter is tagged with the client name, the request method,
and the outcome kind; the circuit counter with the client name and the new state. Every log
template wraps its placeholders in brackets, and the retry and circuit-opened warnings pass the
triggering exception as the log entry's exception instead of formatting it into a property.

These defaults prevent duplicate side effects, bound brownout impact, avoid response-data leaks,
fail misconfiguration at start instead of on the first request, and keep telemetry contracts
stable.
