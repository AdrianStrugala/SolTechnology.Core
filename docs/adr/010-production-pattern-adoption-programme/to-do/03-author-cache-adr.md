---
adr: 010-production-pattern-adoption-programme
step: 03 of 10
status: to-do
---

# Step 03: Author the Cache child ADR (C1, C2, C3, C4)

## Summary
Author the child ADR (provisional ADR-012) that adds a distributed (Redis) cache tier, resilient
cache-aside, a Scrutor decorator helper, and a cache-key/invalidator convention to
`SolTechnology.Core.Cache`. Blocked on open question Q1 (Redis client) from step 01. Seeds its own
plan and premortem.

## Affected components
- `docs/adr/<next>-cache-distributed-and-resilient.md` — the child ADR.
- `docs/adr/<next>-cache-distributed-and-resilient/` — its plan folder.

## Details
- **C1 — distributed tier.** `IDistributedTaskCache` over `IDistributedCache`
  (`GetAsync<T>`/`SetAsync<T>`/`RemoveAsync`/`ExistsAsync`, `System.Text.Json`, per-entry TTL),
  pairing with the existing in-memory `SingletonCache`/`ScopedCache` as a second tier.
- **C2 — resilient cache-aside.** Every cache read/write degrades to the source on cache error
  (logged): a Redis outage must make the call "slow", never "down". Bake it in and document it.
- **C3 — decorator helper.** `services.AddCachedDecorator<IFoo, CachedFoo>()` via Scrutor (already
  used in DreamTravel at 5.0.2, **new** for the Cache runtime package).
- **C4 — key/invalidator.** Cache-key factory convention + `ICacheInvalidator` with a
  `NoCacheInvalidator` no-op default so call sites stay unconditional.
- **Guard-rail (G2):** the cache's `JsonSerializerOptions` MUST be `static`/singleton.
- **Dependency impact (`CLAUDE.md` §1):** Redis client + Scrutor are new runtime deps — run
  `package-management` + `dependency-audit` and report in the child ADR. Component tests use the
  existing `SolTechnology.Core.Redis.Testing` fixture.

## Acceptance criteria
- Child ADR authored with blue/red + premortem-as-final-step; semver **MINOR**.
- New-dependency impact (Redis client, Scrutor) reported per `CLAUDE.md` §1.
- C2 resilient-degradation behaviour and C4 no-op default are explicit in the ADR.
- Index row added in `docs/adr/README.md`.

## Open questions
- Q1 (Redis client) — must be resolved in step 01 before authoring.

