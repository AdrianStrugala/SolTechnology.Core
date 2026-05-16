# ADR 005 — HTTP production defaults (`SolTechnology.Core.HTTP` 0.7.0)

- **Status**: Accepted
- **Date**: 2026-05-15
- **Module**: [`SolTechnology.Core.HTTP`](../../src/SolTechnology.Core.HTTP)
- **Companion document**: [HTTP-Production-Checklist.md](../HTTP-Production-Checklist.md)
- **Companion analyses**: previous premortem and blue/red-team review of the
  0.6.0 surface and its production-usage risks (Top-3: retry on POST,
  retry-storm under brownout, PII via `ResponseBody`).

## Context

`SolTechnology.Core.HTTP 0.6.0` shipped a Polly v8 pipeline that fixed a
correctness issue (the `services.BuildServiceProvider()` second-root
anti-pattern) and a body-buffering footgun, but the defaults and feature
surface still left three classes of incidents waiting in production:

1. **Retry on non-idempotent verbs** — a `POST` that 5xx's was retried, with
   no awareness that replaying it can duplicate bookings, charges, emails.
2. **Defaults tuned for never-firing** — `MinimumThroughput=10` over a 30 s
   sampling window effectively disabled the circuit breaker for low-traffic
   clients; `RequestTimeout=30 s × MaxRequestRetries=3` let a single logical
   call wedge a thread for ~90 s under an upstream brownout.
3. **`HttpRequestFailedException.ResponseBody`** (8 KiB) was always populated
   and flowed verbatim through every standard exception formatter (Serilog,
   Application Insights, Sentry), leaking PII and partner-confidential
   payloads to log storage.

Additional, smaller foot-guns:
- Dual timeout ownership (`HttpClient.Timeout` vs Polly's per-attempt) with
  non-obvious precedence.
- `CorrelationPropagatingHandler` always-on with no opt-out for hosts that
  already own correlation.
- `UsePolly=false` had no operator-visible signal at startup.
- `ValidateDataAnnotations()` was lazy — bad config passed health-checks and
  surfaced on the first production request.
- No first-class metrics; operators were reduced to log-as-metric.
- No outer time budget — retries could legitimately exceed any caller-side
  SLO.
- Reflection projector in `ModuleInstaller.BuildInMemorySource` could
  silently drop a future complex / collection property.

## Decision

We bump to `0.7.0` (still pre-1.0 — breaking changes accepted) and ship the
following changes as a single batch. Per ADR 003's pre-1.0 stance, the version
delta documents intent rather than promising semver MAJOR.

### Defaults flipped to safer production values

| Setting | 0.6.0 | 0.7.0 |
| --- | --- | --- |
| `MaxRequestRetries` | 3 | **2** |
| `RequestTimeout` | 30 000 ms | **10 000 ms** |
| `CircuitBreakerMinimumThroughput` | 10 | **5** |
| `CircuitBreakerSamplingDuration` | 30 000 ms | **10 000 ms** |
| `RetryOnUnsafeVerbs` | (n/a; always retried) | **`false`** |
| `IncludeResponseBodyInException` | (n/a; always captured) | **`false`** |
| `OverallRequestBudget` | (n/a) | nullable, opt-in |

### New behavioural contracts

1. **Idempotent-only retry by default**. `POST` / `PATCH` / `CONNECT` are
   not retried unless `RetryOnUnsafeVerbs = true`. The circuit breaker still
   observes their failures.
2. **Response-body capture is opt-in**. `HttpRequestFailedException.ResponseBody`
   is `null` unless `IncludeResponseBodyInException = true`. `ToString()` is
   overridden to exclude the body even when populated.
3. **Single timeout owner**. When `UsePolly=true`, `HttpClient.Timeout =
   Timeout.InfiniteTimeSpan` and `HTTPClientConfiguration.TimeoutSeconds` is
   ignored (with a `LogWarning` at registration if set).
4. **`OverallRequestBudget`** caps the whole logical call when set; validated
   `> RequestTimeout` at startup.
5. **`UsePolly=false`** logs a startup `LogWarning` per client so operators
   see the disabled-resilience state without grepping config.
6. **`propagateCorrelation`** parameter on `AddHTTPClient<,>` (default
   `true`); when `false`, the correlation handler is not registered and the
   AsyncLocal store is not touched. The handler is also additive-only —
   if the caller already attached `X-Correlation-Id`, neither the wire
   value nor the AsyncLocal is changed.
7. **`ValidateOnStart()`** on both option types plus a cross-field validator
   for `OverallRequestBudget > RequestTimeout` and a `BaseAddress` required
   check. Misconfiguration fails the host build, not the first request.
8. **First-class metrics** via `Meter("SolTechnology.Core.HTTP")` with
   documented instrument names (`retries`, `circuit_state_changes`) and
   tags. Stable contract going forward.
9. **`RequestBuilder` policy flow** via `HttpRequestMessage.Options`
   (`RequestBuilder.PolicyOptionsKey`) — enables the per-request body opt-in,
   the per-request `WithJsonOptions(...)`, and future per-request policy
   overrides. New `HttpClient.CreateRequest(path, HttpPolicyConfiguration)`
   overload exposes the seam to consumers.
10. **`[MustDisposeResource]`** (JetBrains.Annotations, `PrivateAssets=all`)
    on the raw-response verb overloads so Rider / ReSharper flag missed
    `using`.
11. **`BuildInMemorySource` projector guard** rejects unsupported property
    types (anything that is not `string` / primitive / enum / `IFormattable`)
    so a future complex property cannot silently disappear from the
    explicit-parameter path.
12. **`ArrayPool<byte>`** for response-body capture to remove the per-failure
    8 KiB allocation under burst load.

## Consequences

### Positive

- Three classes of incident closed by default behaviour, not by operator
  knowledge.
- Misconfigurations surface at host startup.
- Operators get a stable metrics contract for dashboards / alerts.
- Per-request policy / JSON overrides remove the "one global setting fits
  every endpoint" trap.

### Negative / migration cost

- Every consumer must re-validate behaviour: shorter `RequestTimeout`, no POST
  retries, `null` response body in exceptions.
- Hosts that depended on `HttpClient.Timeout` being the active deadline must
  either keep it (by setting `UsePolly=false`) or migrate their tuning to
  `HttpPolicyConfiguration.RequestTimeout`.
- Hosts with their own correlation pipeline that don't yet pass
  `propagateCorrelation: false` will continue to get our handler, but the
  additive-only change in `CorrelationPropagatingHandler` removes the
  AsyncLocal-divergence risk.

### Accepted risk

- Hosts that catch `HttpRequestFailedException` and expect `ResponseBody !=
  null` need an explicit opt-in. We accept the migration cost as the price
  of removing the PII vector.

## Alternatives considered

- **Keep 0.6.0 defaults, document the foot-guns.** Rejected — every team
  would re-discover the same incidents independently. Defaults are the
  product.
- **Hide unsafe-verb retries behind a per-call header (`X-Retry-Allowed:
  true`).** Rejected — moves policy into request construction, away from
  the registration call where every other resilience choice lives.
- **Split into `SolTechnology.Core.HTTP.AspNetCore` (with correlation) and
  `SolTechnology.Core.HTTP.Core` (without).** Deferred — the
  `propagateCorrelation` parameter solves the same problem without splitting
  the package.
- **Emit metrics through Polly's built-in telemetry only.** Rejected — Polly's
  instrument names are an implementation detail and have churned between
  versions. We own a stable contract.

## References

- [HTTP-Production-Checklist.md](../HTTP-Production-Checklist.md) — operator-facing
  details.
- [`src/SolTechnology.Core.HTTP/HttpPolicyConfiguration.cs`](../../src/SolTechnology.Core.HTTP/HttpPolicyConfiguration.cs) — every default has
  an in-code rationale comment next to it.
- [`tests/SolTechnology.Core.HTTP.Tests/HttpPolicyFactoryTests.cs`](../../tests/SolTechnology.Core.HTTP.Tests/HttpPolicyFactoryTests.cs) — pinned coverage
  for retry-by-verb, `Retry-After` cap, outer budget, `UsePolly=false`
  pass-through.
- [`tests/SolTechnology.Core.HTTP.Tests/ModuleInstallerTests.cs`](../../tests/SolTechnology.Core.HTTP.Tests/ModuleInstallerTests.cs) — `ValidateOnStart`,
  cross-field validation, projector guard, propagateCorrelation toggle.

