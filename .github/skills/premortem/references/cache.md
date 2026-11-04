# Cache — `src/SolTechnology.Core.Cache/`

- TTL semantics change (sliding ↔ absolute) → stale data served.
- Key format change → silent cache miss (perf regression, not correctness).
- Cache stampede on cold start with new eviction policy.
- Distributed cache provider swap with in-flight serialised payloads → deserialisation failures
  for warm keys after deploy.
- Redis connection factory change → `abortConnect` semantics at boot. `ConnectionMultiplexer.Connect`
  defaults to `abortConnect=true` for non-Azure endpoints, so Redis down at start throws from every
  consumer that resolves the shared multiplexer (lock, idempotency store, health check, and
  `IDistributedCache` through `ConnectionMultiplexerFactory`) instead of failing open. Check that the
  factory sets `AbortOnConnectFail = false` and that a test boots against an unreachable endpoint
  without `abortConnect=false` in the connection string (cache code review, 2026-11-04).

