---
spec: 2026-11-04-cache-redis-reconnect
review: done (2026-11-04)
premortem: go-with-mitigations (2026-11-04)
---

# Cache — Redis connection recovers at runtime — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-04-cache-redis-reconnect.md`](../2026-11-04-cache-redis-reconnect.md).

Gates: review is required (public API C-30 and C-31, DI contract P-21, persisted value layout P-22). The premortem is
required by `CLAUDE.md §4` (`ModuleInstaller.cs` edits in steps 01–05; persisted contract in step 02). Q1 of step 03
was answered by the user on 2026-11-04 (option A: `AddSolDistributedCache` stops registering `IConnectionMultiplexer`;
breaking change confirmed as C-31). Plan review done on 2026-11-04 with no open question. Premortem run on 2026-11-04
in a fresh session: Go with mitigations; the required mitigations are folded into steps 01, 03, 04, 05, and 06 (marked
"premortem #N"). No step is `blocked`.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | done |
| 01 | Route every Redis consumer through an internal connection provider | [`steps/01-connection-provider.md`](steps/01-connection-provider.md) | to-do |
| 02 | `IRedisCache` on `IDatabase`; drop `IDistributedCache` and its package | [`steps/02-redis-cache-on-idatabase.md`](steps/02-redis-cache-on-idatabase.md) | to-do |
| 03 | Atomic connection swap with delayed disposal | [`steps/03-connection-swap.md`](steps/03-connection-swap.md) | to-do |
| 04 | Force-reconnect trigger and public options | [`steps/04-force-reconnect-trigger.md`](steps/04-force-reconnect-trigger.md) | to-do |
| 05 | Module docs and premortem checklist | [`steps/05-module-docs.md`](steps/05-module-docs.md) | to-do |
| 06 | Retrospective | [`steps/06-retrospective.md`](steps/06-retrospective.md) | to-do |
