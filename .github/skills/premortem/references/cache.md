# Cache — `src/SolTechnology.Core.Cache/`

- TTL semantics change (sliding ↔ absolute) → stale data served.
- Key format change → silent cache miss (perf regression, not correctness).
- Cache stampede on cold start with new eviction policy.
- Distributed cache provider swap with in-flight serialised payloads → deserialisation failures
  for warm keys after deploy.

