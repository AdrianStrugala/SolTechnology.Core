---
spec: 2026-11-04-cache-redis-reconnect
step: 05
status: to-do
---

# Step 05: Module docs and premortem checklist

## Summary
Update the consumer-facing `docs/Cache.md` to the delivered behaviour: runtime reconnect, the two new options, the
reconnect log events, the smaller DI surface, and the string value layout. Sweep the remaining XML docs that still
name `IConnectionMultiplexer` or `IDistributedCache`, and add the lesson of this feature to the premortem cache
checklist. `docs/architecture/cache.md` is updated by the retrospective, after behaviour ships.

## Affected components
- `docs/Cache.md` — EDIT — sections below
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — XML doc of `AddSolDistributedCache` mentions force reconnect and the two options (doc-only change; the `IConnectionMultiplexer` wording was already replaced in step 03)
- `src/SolTechnology.Core.Cache/DistributedCacheConfiguration.cs` — EDIT — class summary names the connection-recovery options (doc-only change)
- `.github/skills/premortem/references/cache.md` — EDIT — one new bullet (below)
- `docs/features/2026-11-04-cache-redis-reconnect.md` — EDIT — "Umbrella-brief rows": P-21 row and release-notes input text (premortem #3, #4, #10); the brief's grace-period wording (premortem #2)

## Changes
- `docs/Cache.md` "Features": the Redis connection recovers at runtime (force reconnect after persistent connection errors), in addition to failing open at start.
- `docs/Cache.md` "Configuration" table: rows `ReconnectErrorThreshold` (`00:00:30`, must be greater than 0) and `ReconnectMinInterval` (`00:01:00`, must be greater than 0), with one sentence on which errors count (connection and socket errors, timeouts only while disconnected) and an `appsettings.json` example using `"00:00:30"` strings.
- `docs/Cache.md`: a short `#### Connection recovery` subsection under `### Configuration` (no new `###` section): the rule, the grace period (the connection's `asyncTimeout` plus `heartbeatInterval`, premortem #2), the EventIds `2301`–`2304` with level and template, and that recovery needs Redis traffic or the health check to observe errors.
- `docs/Cache.md` "What ships in DI": `AddSolDistributedCache` registers `IRedisCache` and validated `DistributedCacheConfiguration`; no `IDistributedCache` (call `AddStackExchangeRedisCache` yourself if you need it); no `IConnectionMultiplexer` (Q1 = A, user, 2026-11-04, C-31): one sentence that the Redis connection is internal so a reconnect never disposes an instance the app holds, that injecting `IConnectionMultiplexer` without registering it fails DI validation at start, and a one-line example `services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));` for apps that need their own connection; the `AddSolDistributedCache` row says it uses a registered `TimeProvider` when present; the `AddSolRedisHealthCheck(...)` row says it requires `AddSolDistributedCache` (P-21).
- `docs/Cache.md`: the Redis value of an `IRedisCache` entry is the camelCase JSON string with the `ExpirationSeconds` TTL.
- Premortem bullet: "Redis connection swap → any type that keeps an `IConnectionMultiplexer` or `IDatabase` across calls (a lock handle, a cached field, a DI registration) uses a disposed connection after a reconnect. Check that every consumer takes the database from the provider per call (cache Redis reconnect, 2026-11-04)."
- Premortem 2026-11-04 additions to `docs/Cache.md` (all under the existing sections; no new `###`):
  - #3, "What ships in DI": removing `IDistributedCache` is silent where it was consumed optionally. `HybridCache` stops using Redis as L2, and an app that also calls `AddDistributedMemoryCache()` (for example for sessions) now gets the in-memory cache. Both need `AddStackExchangeRedisCache` themselves. A missing constructor dependency fails at start only with `ValidateOnBuild` (the ASP.NET Core default in Development); otherwise it fails at first resolution.
  - #4, "What ships in DI": `IConnectionMultiplexer` resolved inside a lambda or optionally fails at first use, or silently does nothing, rather than at start. Name third-party Redis health checks (`AddRedis(sp => sp.GetRequiredService<IConnectionMultiplexer>())`, use `AddSolRedisHealthCheck()` instead), OpenTelemetry `AddRedisInstrumentation()` (instruments only a connection the app registers itself; the package-owned connection is not instrumented), and the Data Protection Redis repository. Give the migration: register your own multiplexer.
  - #10, "What ships in DI": `AddSolDistributedLock()` and `AddSolDistributedIdempotency()` need `AddSolDistributedCache`; an app-registered `IConnectionMultiplexer` is no longer used by them.
  - #5, `#### Connection recovery`: errors are seen only through Redis calls and the health check, and they must arrive at most `ReconnectErrorThreshold` apart. A probe polled more often than the threshold provides that. Keep the health check `timeout` and the connection's `asyncTimeout` below the platform's probe timeout (Kubernetes `timeoutSeconds` defaults to 1 s); a probe cancelled by the caller counts only while the connection is down.
  - #6, #7, `#### Connection recovery`: during a grace period the process holds two connections, so size `maxclients` for twice the instance count. On Redis Cluster a single unavailable shard rebuilds the whole connection at most once per `ReconnectMinInterval`. Keep `ReconnectMinInterval` above the grace period.
  - #2, `#### Connection recovery`: the grace period is `asyncTimeout` plus `heartbeatInterval` (default 6 s).
- Brief edits (premortem #2, #3, #4, #10):
  - P-21 row, consequence column: add the silent `IDistributedCache` cases, the `ValidateOnBuild` qualification, the lambda and optional `IConnectionMultiplexer` consumers, and the lock and idempotency requirement, one clause each.
  - Release-notes input: "disposed after the command timeout" becomes "disposed after the command timeout plus one heartbeat", and add one sentence: "Integrations that resolve `IConnectionMultiplexer` or `IDistributedCache` lazily (HybridCache, third-party health checks, OpenTelemetry Redis instrumentation) silently stop seeing Redis or fail at first use."

## Acceptance criteria
- [ ] `grep -n "IDistributedCache" docs/Cache.md` shows only the sentence that tells apps to call `AddStackExchangeRedisCache` themselves.
- [ ] `grep -n "IConnectionMultiplexer" docs/Cache.md` shows only the "What ships in DI" note and its registration example; the `AddSolDistributedCache` row of the table no longer lists it.
- [ ] `grep -n "ReconnectErrorThreshold\|ReconnectMinInterval\|2301\|2302\|2303\|2304" docs/Cache.md` finds the options and all four EventIds.
- [ ] `grep -rn "IConnectionMultiplexer\|IDistributedCache" src/SolTechnology.Core.Cache --include=*.cs | grep "///"` prints nothing.
- [ ] Every C# block of `docs/Cache.md` compiles in a throwaway console project under the git-excluded `.scratch/` that references `src/SolTechnology.Core.Cache`; record the block count in `Deviations` if any block needed a fix.
- [ ] `docs/Cache.md` keeps the `docs/PublicDocumentationGuide.md` section structure: `grep -c "^### " docs/Cache.md` is unchanged from before this step, and "Connection recovery" is a `####` heading under `### Configuration`.
- [ ] `grep -n "AddSolRedisHealthCheck" docs/Cache.md` shows the "What ships in DI" row stating that it requires `AddSolDistributedCache`.
- [ ] Premortem #3, #4: `grep -n "HybridCache\|ValidateOnBuild\|AddRedisInstrumentation" docs/Cache.md` finds each term at least once in "What ships in DI", and the brief's P-21 row contains `HybridCache` and `ValidateOnBuild`.
- [ ] Premortem #5: `grep -n "timeoutSeconds" docs/Cache.md` finds the probe-timeout guidance under `#### Connection recovery`.
- [ ] Premortem #2, #6: `grep -n "heartbeatInterval\|maxclients" docs/Cache.md` finds the grace formula and the connection-doubling note; `grep -n "plus one heartbeat" docs/features/2026-11-04-cache-redis-reconnect.md` finds the release-notes input.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` reports 0 errors (XML docs compile without warnings).

## Open questions
- none

## Deviations
