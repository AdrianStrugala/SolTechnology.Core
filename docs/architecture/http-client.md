# HTTP Client

`AddSolHTTPClient<TInterface, TClient>()` registers typed HTTP clients with production-oriented
resilience defaults. Polly is enabled by default. Correlation propagation is enabled by default
and can be disabled explicitly.

## Resilience defaults

| Setting | Default |
|---|---|
| Attempts | 3 total |
| Initial retry delay | 200 ms |
| Maximum retry delay | 30 s |
| Per-attempt timeout | 10 s |
| Overall request budget | Disabled |
| Circuit failure ratio | 0.3 |
| Circuit sampling duration | 10 s |
| Circuit minimum throughput | 5 |
| Circuit break duration | 10 s |
| Unsafe-method retries | Disabled |
| Response-body capture | Disabled |

Retries cover `408`, `429`, `500`, `502`, `503`, `504`, network failures, and timeout failures.
`POST`, `PATCH`, and `CONNECT` are excluded unless unsafe retries are explicitly enabled. A
`RetryPredicate` may veto otherwise retryable responses but cannot expand the default set.
`Retry-After` is honored up to the configured retry timeout.

With Polly enabled, `HttpClient.Timeout` is infinite and Polly owns deadlines. Without Polly,
`TimeoutSeconds` controls `HttpClient.Timeout`. Backoff consumes time in addition to individual
attempts, so configure `OverallRequestBudget` when total latency must be bounded.

## Diagnostics and safety

Base address, annotations, and timeout relationships are validated on host start. Response-body
capture is opt-in, capped at 8 KiB, pooled, and excluded from exception `Message` and
`ToString()` to reduce accidental disclosure.

Correlation propagation preserves an existing `X-Correlation-Id`; otherwise it adds the ambient
correlation value and `traceparent` when available. Stable metrics use meter
`SolTechnology.Core.HTTP`.

These defaults prevent duplicate side effects, bound brownout impact, avoid response-data leaks,
and keep telemetry contracts stable.
