---
status: completed
created: 2026-05-15
completed: 2026-05-15
---

# HTTP Production Defaults

> Historical delivery record. It may not describe the current system.

## Goal

Make typed HTTP clients safe under retries, brownouts, timeouts, diagnostics, and correlation.

## Context

Earlier defaults allowed unsafe retries, unbounded aggregate latency, eager response capture, and
ambiguous timeout ownership. A production review identified three primary incident classes:

1. Non-idempotent `POST`, `PATCH`, and `CONNECT` requests could be replayed, duplicating bookings,
	charges, or notifications.
2. A high circuit-breaker throughput threshold and long per-attempt timeout made low-traffic
	clients effectively unprotected during an upstream brownout.
3. Captured response bodies flowed through standard exception formatters and could leak personal
	or partner-confidential data into log storage.

Secondary risks included competing `HttpClient.Timeout` and Polly deadlines, always-on correlation
propagation, lazy option validation, no stable metrics contract, no overall request budget, and a
reflection-based configuration projector that could silently ignore future complex properties.

## Original decision

Ship the safety changes as one pre-1.0 release batch, treating defaults as part of the product:

| Setting | Previous | Selected default |
|---|---:|---:|
| `MaxRequestRetries` | 3 | 2 |
| `RequestTimeout` | 30 seconds | 10 seconds |
| `CircuitBreakerMinimumThroughput` | 10 | 5 |
| `CircuitBreakerSamplingDuration` | 30 seconds | 10 seconds |
| `RetryOnUnsafeVerbs` | Implicitly enabled | `false` |
| `IncludeResponseBodyInException` | Implicitly enabled | `false` |
| `OverallRequestBudget` | Not available | Nullable, opt-in |

The intended behavioral contracts were:

- unsafe verbs are observed by the circuit breaker but retried only after explicit opt-in;
- exception response bodies are absent by default and excluded from `ToString()` even when
  captured;
- Polly owns request deadlines when enabled, with `HttpClient.Timeout` made infinite;
- an optional outer timeout caps the complete logical call;
- disabled resilience produces an operator-visible warning;
- correlation propagation can be disabled and never overwrites a caller-supplied header;
- options and cross-field constraints validate when the host starts;
- stable `SolTechnology.Core.HTTP` metrics cover retries and circuit-state changes;
- per-request policy and JSON options flow through `HttpRequestMessage.Options`;
- unsupported configuration projection types fail instead of disappearing silently.

The batch also planned disposal annotations for raw responses and pooled buffers for bounded
response capture.

## Alternatives considered

### Keep the old defaults and document hazards

Rejected because every consumer would need to rediscover the same retry, latency, and logging
failures. Safe behavior needed to be the default.

### Enable unsafe retries with a request header

Rejected because resilience policy belongs with client registration and configuration, not as an
ad hoc wire-level convention.

### Split correlation into a second package

Deferred because an explicit registration option provided the required ownership boundary without
creating another package.

### Depend only on Polly telemetry names

Rejected because those names are an implementation detail. The package chose to own a stable
consumer-facing metrics contract.

## Scope

- Adopt conservative retry and circuit-breaker defaults.
- Exclude unsafe methods from retries by default.
- Add explicit per-attempt and optional overall budgets.
- Make response capture bounded and opt-in.
- Add stable metrics and correlation propagation.

## Implementation plan

Change configuration defaults, resilience construction, validation, exception capture,
telemetry, tests, and consumer documentation as one release batch.

## Acceptance criteria

- Unsafe retries require explicit opt-in.
- Configuration fails early when invalid.
- Response bodies are not captured by default.
- Resilience and telemetry behavior is covered by tests.
- `OverallRequestBudget` is greater than the per-attempt timeout when configured.
- A caller-provided correlation ID is preserved.
- Unsupported explicit configuration values fail rather than being silently dropped.

## Expected consequences

### Positive

- Unsafe replay, brownout amplification, and response-body leakage are prevented by default.
- Configuration errors surface before the first production request.
- Consumers receive stable metrics and per-request override seams.

### Negative

- Existing consumers must revalidate shorter deadlines and reduced retry counts.
- Consumers that relied on response bodies in exceptions must opt in explicitly.
- Hosts that treated `HttpClient.Timeout` as the active deadline must move tuning into the Polly
	configuration or disable Polly.

## Completion summary

The safer defaults, verb-aware retry, optional outer budget, response-capture controls, validation,
correlation ownership, metrics, per-request options, disposal annotations, projection guard, and
pooled capture shipped. Tests pinned verb behavior, retry delays, budget validation, disabled
resilience, correlation registration, and configuration projection.

Current values and rationale live in
[`../architecture/http-client.md`](../architecture/http-client.md).

## Deviations

- Retry backoff means total latency is not simply attempts multiplied by request timeout.
- Some warnings occur when a client pipeline is materialized rather than strictly at host startup.

## Follow-ups

- Revisit package splitting only if correlation ownership cannot be expressed by registration.
- Treat instrument names as a compatibility contract when telemetry evolves.
