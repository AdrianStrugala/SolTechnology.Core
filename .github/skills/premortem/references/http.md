# HTTP / ApiClient — `src/SolTechnology.Core.HTTP/`, `src/SolTechnology.Core.ApiClient/`

- Default timeout / retry policy change → upstream service marked unhealthy.
- Response contract change (added required member, renamed) → consumer deserialization fails.
- `HttpClient` registration leak (transient instead of typed/factory).
- TLS / handler behaviour change on .NET 10 surface.
- Polly resilience pipeline replaced → retry/backoff semantics drift silently.
- `HttpClient.Timeout` left at default instead of `InfiniteTimeSpan` when Polly owns timeout →
  Polly retry killed mid-flight by the outer client timeout.
- A `DelegatingHandler` that maps `OperationCanceledException` with a cancelled token to "caller
  cancelled" → `HttpClient.Timeout` (without Polly) is misreported, because handlers see only the
  linked token; check for an inner `TimeoutException` or the elapsed client timeout.
- Recording request paths or query strings in logs, spans, or reports → API keys and personal data
  leak; record the host only.
- A host-wide default handler (Aspire ServiceDefaults' `ConfigureHttpClientDefaults` adding
  `AddStandardResilienceHandler()`) on a Sol client → two nested Polly pipelines: retries
  multiply, timeouts stack, two circuit breakers per call. Microsoft.Extensions.Http applies the
  defaults first whatever the registration order; check the built handler chain for exactly one
  `ResilienceHandler`, not the registration code.
