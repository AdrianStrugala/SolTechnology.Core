---
spec: 2026-11-04-cache-redis-reconnect
step: 00
status: done
---

# Step 00: Run premortem

## Summary
The premortem is mandatory (`CLAUDE.md §4`): steps 01–05 edit `src/SolTechnology.Core.Cache/ModuleInstaller.cs`, and
step 02 changes a persisted contract (the Redis value layout of `IRedisCache` entries). Run the
[premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did not author this plan, record
its output here, and set `premortem:` in [`summary.md`](../summary.md) to the verdict in the same change. No `src/`
code is touched by this step.

## Affected components
- `docs/features/2026-11-04-cache-redis-reconnect/steps/00-run-premortem.md` — EDIT — premortem output
- `docs/features/2026-11-04-cache-redis-reconnect/summary.md` — EDIT — `premortem:` verdict and step status

## Changes
- **Modules touched:** `src/SolTechnology.Core.Cache` (`ModuleInstaller.cs`, `DistributedCacheConfiguration.cs`, `RedisCache.cs`, `RedisDistributedLockService.cs`, `RedisIdempotencyStore.cs`, `HealthChecks/*`, new internal `RedisConnectionProvider`, `RedisMultiplexerFactory`, `RedisReconnectPolicy`, `RedisConnectionLogMessages`, `DistributedCacheConfigurationValidation`), `Directory.Packages.props`, `tests/SolTechnology.Core.Cache.Tests`, `docs/Cache.md`, `.github/skills/package-management/references/canonical-versions.md`, `.github/skills/premortem/references/cache.md`.
- **Contract delta:**
  - Public API (additive, C-30, confirmed by the user on 2026-11-04): `DistributedCacheConfiguration.ReconnectErrorThreshold` (`TimeSpan`, default 30 s) and `ReconnectMinInterval` (`TimeSpan`, default 60 s), validated `> TimeSpan.Zero` at start.
  - DI contract (P-21, C-31 breaking, confirmed by the user's Q1 = A answer on 2026-11-04): `AddSolDistributedCache` no longer registers `IDistributedCache` / `IOptions<RedisCacheOptions>` (step 02) or `IConnectionMultiplexer` (step 03); apps that inject either fail DI validation at start and register their own. `AddSolRedisHealthCheck()` requires `AddSolDistributedCache` instead of any registered `IConnectionMultiplexer` (step 01).
  - Package dependency (P-21): `Microsoft.Extensions.Caching.StackExchangeRedis` removed from `SolTechnology.Core.Cache` (1.0.0: 1,494 downloads, [`nuget-stats.json`](../../../../nuget-stats.json)).
  - Persisted contract (P-22): `IRedisCache` entries become Redis strings (`SET … EX`) instead of the `IDistributedCache` hash; keys and the TTL are unchanged.
  - Log contract (additive): EventIds `2301`–`2304`; existing Cache templates unchanged.
  - Runtime behaviour: the shared multiplexer can be replaced and the old one disposed after `asyncTimeout`; the internal `RedisConnectionProvider` singleton implements `IDisposable` and `IAsyncDisposable`.
- **Applicable checklists:** [`references/cache.md`](../../../../.github/skills/premortem/references/cache.md) (key format, TTL semantics, provider swap with in-flight payloads, `abortConnect` at boot), [`references/di.md`](../../../../.github/skills/premortem/references/di.md) (registration changes), [`references/build-and-nuget.md`](../../../../.github/skills/premortem/references/build-and-nuget.md) (dependency removal, CPM), [`references/logging.md`](../../../../.github/skills/premortem/references/logging.md) (new EventIds, no secrets in templates), [`references/testing-kits.md`](../../../../.github/skills/premortem/references/testing-kits.md) (container stop/start test).
- **Steps under review:** [01](01-connection-provider.md), [02](02-redis-cache-on-idatabase.md), [03](03-connection-swap.md), [04](04-force-reconnect-trigger.md), [05](05-module-docs.md), [06](06-retrospective.md); feature brief [`../../2026-11-04-cache-redis-reconnect.md`](../../2026-11-04-cache-redis-reconnect.md).

## Acceptance criteria
- [x] Premortem output (failure stories, checklist sweep, mitigations, verdict) recorded in this file.
- [x] `summary.md` `premortem:` is `go (date)`, `go-with-mitigations (date)`, or `no-go (date)` in the same change; mitigations are added to the affected steps.

## Premortem output

Run on 2026-11-04 in a fresh session that did not author the plan, against `master@0e9498ef`, the brief, steps
01–06, the Cache sources, and the StackExchange.Redis source at tag `3.0.0`. Stories were written before the
checklists were opened.

### Premortem — Cache: Redis connection recovers at runtime

#### Frame

- **Modules touched**: `SolTechnology.Core.Cache` (`ModuleInstaller.cs`, `DistributedCacheConfiguration.cs`,
  `RedisCache.cs`, `RedisDistributedLockService.cs`, `RedisIdempotencyStore.cs`, `HealthChecks/*`, five new internal
  files), `Directory.Packages.props`, `tests/SolTechnology.Core.Cache.Tests`, `docs/Cache.md`, two skill references.
- **API delta**: added `DistributedCacheConfiguration.ReconnectErrorThreshold` and `ReconnectMinInterval` (C-30);
  removed DI registrations `IConnectionMultiplexer` (C-31), `IDistributedCache` and `IOptions<RedisCacheOptions>`
  (P-21); removed package dependency `Microsoft.Extensions.Caching.StackExchangeRedis` (P-21); `AddSolRedisHealthCheck()`
  now requires `AddSolDistributedCache`; persisted value layout hash → string (P-22); EventIds `2301`–`2304` (additive).
- **Semver impact**: MAJOR — DI registrations and a package dependency are removed
  (`ModuleInstaller.cs:72`, `:78-83`, `SolTechnology.Core.Cache.csproj:17`). It lands inside the unpublished 2.0.0
  (`src/Directory.Build.props:5`; latest tag `v1.0.0`; `nuget-stats.json` lists only Cache `1.0.0`), so no extra bump.
- **Consumers in workspace**: DreamTravel references Cache by project reference
  (`DreamTravel.GeolocationDataClients.csproj:9`) and calls only `AddSolLocalCache` (`DreamTravel.Api/Program.cs:114`,
  `DreamTravel.Worker/Program.cs:51`); it does not use Redis, `IDistributedCache`, or `IConnectionMultiplexer`, so it
  only has to keep building. `SolTechnology.Core.Api` mentions `AddSolRedisHealthCheck()` in an XML example only
  (`HealthChecksEndpointExtensions.cs:25,30`). External: 1,494 downloads of Cache 1.0.0, whose `AddSolDistributedCache`
  registered `IDistributedCache` and `IConnectionMultiplexer` (`git show v1.0.0:src/SolTechnology.Core.Cache/ModuleInstaller.cs`).

#### Imagined Failure

Two weeks after 2.0.0 ships, a community user reports that their Kubernetes deployment went fully NotReady on
upgrade: their readiness endpoint used a third-party Redis health check wired as
`sp => sp.GetRequiredService<IConnectionMultiplexer>()`, which `ValidateOnBuild` never saw, so every probe threw. A
second user's staging stack returns 500 on every cached call because a typo in the connection string makes the
multiplexer factory throw, and `ReportError` re-runs that factory inside the cache's `catch`, so the fail-open path
itself throws. Meanwhile our own CI flakes on the container test, because the old multiplexer is disposed while
StackExchange.Redis still holds backlogged commands and they surface as `ObjectDisposedException`. And the one case
force reconnect exists for (a stuck multiplexer on a pod pulled out of rotation) never recovers, because the
kubelet's 1 s probe timeout cancels the health check before it can report an error.

#### Scenarios

| # | Story (what + mechanism) | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation → step |
|---|---|---|---|---|---|---|---|
| 1 | **Fail-open path throws.** The first multiplexer creation throws (for example `ConfigurationOptions.Parse` rejects a malformed connection string; only emptiness is validated). The consumer's `GetDatabase()` throws inside its `try`, the `catch` calls `ReportError`, and `ReportError` reads `Multiplexer.IsConnected`. Step 03 makes a failed first access retry `factory.Create`, so it throws again, this time out of the `catch`. Every `GetOrAdd`, lock, idempotency, and health call throws instead of failing open. Today the cache falls through, because Microsoft's `RedisCache` connects inside `GetAsync`. | step 04 Changes (`ReportError` → `Multiplexer.IsConnected`); step 03 Changes ("lets the next access retry"); `RedisCache.cs:49`, `:65`; `ModuleInstaller.cs:63-64` (no parse validation) | public NuGet | H | L | none — gap (`Multiplexer_FirstCreateThrows_NextAccessRetries` pins the retry that causes it) | `ReportError` never creates or throws; consumer fail-open test → **step 04** |
| 2 | **Old connection disposed too early.** The grace period equals `AsyncTimeout`. StackExchange.Redis 3.0.0 times out backlogged commands only when the heartbeat runs (`PhysicalBridge.OnHeartbeat` → `StartBacklogProcessor`, `PhysicalBridge.cs:557-568`, default `HeartbeatInterval` 1 s), so a command queued just before the swap can outlive `AsyncTimeout` by up to one heartbeat. Disposal abandons the backlog with `ObjectDisposedException("Connection is being disposed")` (`PhysicalBridge.cs:136`). In production that is a fail-open Warning carrying an `ObjectDisposedException`. In CI it breaks the container test's "no `ObjectDisposedException`" criterion, where `asyncTimeout=500` makes the heartbeat twice the grace period. | step 03 Changes ("Grace period: … `AsyncTimeout`"); step 04 container test (`asyncTimeout=500`) | public NuGet (log noise); internal CI (flaky release gate) | M | M | none — gap | grace = `AsyncTimeout` + `HeartbeatInterval` → **step 03**; docs → **step 05** |
| 3 | **Silent `IDistributedCache` loss.** An app had the Redis `IDistributedCache` from `AddSolDistributedCache` and consumed it optionally: `HybridCache` uses any registered `IDistributedCache` as L2, and an app that also calls `AddDistributedMemoryCache()` (a `TryAdd`) for sessions used to get Redis because Microsoft's `AddStackExchangeRedisCache` registered last. After step 02 there is no DI error. HybridCache silently runs L1-only and sessions become per-pod, so users are logged out behind a load balancer. P-21 says these apps "fail DI validation at start", which is wrong here, and wrong for constructor injection too when `ValidateOnBuild` is off (the ASP.NET Core default outside Development). | `ModuleInstaller.cs:72` removed by step 02; brief P-21 row | public NuGet | M | L | P-21 row + `docs/Cache.md:293` (claims a start-time failure) | name the silent cases and qualify "at start" in docs, P-21, and release notes → **step 05** |
| 4 | **Late `IConnectionMultiplexer` failures that validation cannot see.** Integrations resolve the multiplexer inside lambdas or optionally. `AspNetCore.HealthChecks.Redis` `AddRedis(sp => sp.GetRequiredService<IConnectionMultiplexer>())` throws on every probe, so readiness fails on every pod: a full outage after upgrade. OpenTelemetry's `AddRedisInstrumentation()` resolves the multiplexer from DI when none is passed, so Redis spans silently disappear. Data Protection's Redis repository built in a `Configure<IServiceProvider>` lambda fails at the first key use. Step 03's validation test only covers constructor injection. | `ModuleInstaller.cs:78-83` removed by step 03; step 03 AC `…ConsumerInjectsConnectionMultiplexer_FailsValidationOnBuild` | public NuGet | H | L | C-31 / P-21 (user Q1 = A); MAJOR version | docs/P-21/release notes list lambda and optional resolutions plus migration → **step 05**; instrumentation hook follow-up → **step 06** |
| 5 | **The idle-recovery signal never arrives.** The health check is the only error source on a pod pulled out of rotation. The kubelet's default probe `timeoutSeconds: 1` aborts the request, and `HealthCheckMiddleware` passes `RequestAborted`, so `PingAsync().WaitAsync(5 s, ct)` is cancelled before the backlog times out (5 s). The caller-cancellation `catch` rethrows without reporting, the policy never sees an error, and a stuck multiplexer on an unready pod never force-reconnects. The same happens when errors arrive further apart than `ReconnectErrorThreshold`, for example probes every 60 s: the window restarts on every error. | `RedisHealthCheck.cs:28-32`; step 04 Changes ("not in the caller-cancellation `catch`"; rule 3 gap restart) | public NuGet | M | M | none — gap | report a caller-cancelled probe while disconnected → **step 04**; probe-timeout and traffic guidance → **step 05** |
| 6 | **Fleet-wide churn.** All pods see the same outage, so their error windows open together and they force-reconnect in sync every ~90 s (30 s threshold plus 60 s interval). During each grace period every pod holds two multiplexers (twice the connections), which can hit `maxclients` on small Azure tiers. On Redis Cluster, `IsConnected` is `true` while any shard is up, so a single dead shard's `RedisConnectionException`s rebuild the connections to every healthy shard each cycle. Auth or TLS failures (rotated password, expired certificate) loop the same way, because the reconnect re-parses the same static connection string. | step 04 Changes rule 1 (`RedisConnectionException` counts regardless of `IsConnected`); brief Facts ("`IsConnected` is `true` when any server endpoint is connected") | public NuGet | M | L | `ReconnectMinInterval` + threshold; timeouts excluded while connected (`NonConnectionErrors_DoNotCount`); same rule as Microsoft's `UseForceReconnect` | accepted; documented → **step 05**; jitter follow-up → **step 06** |
| 7 | **Slow leak of replaced connections.** The step 03 list of "pending old multiplexers" is never pruned after grace disposal, so a days-long outage (one reconnect per ~90 s) accumulates references to disposed multiplexers. Separately, `Multiplexer` accessed for the first time after the provider is disposed (a background job during shutdown, or many short-lived providers in a consumer's test suite) creates a multiplexer that nobody disposes. A small `ReconnectMinInterval` combined with a large `asyncTimeout` also keeps many multiplexers alive at once (validation has no ratio rule). | step 03 Changes ("kept in a list guarded by a lock"; disposal rules cover only created or pending multiplexers) | public NuGet | L | L | none — gap | prune and disposed-access rules + tests → **step 03**; `asyncTimeout` vs interval note → **step 05** |
| 8 | **WRONGTYPE on the value layout (P-22).** A hash written under a 2.0 key by a pre-release build (a project reference or a CI feed) meets the string layout during a rolling deploy. New pods hit `WRONGTYPE` on `GET`, fall through, and overwrite. Old pods hit `WRONGTYPE` on `HMGET`/`HSET` against the string and miss until the rollout ends. 1.x keys cannot collide: 1.x used `{InstanceName}{JSON}` (`v1.0.0` `RedisCache.cs`), and 2.0 prefixes type names (`CacheKey.cs:12`), which never start with a JSON token. `WRONGTYPE` (`RedisServerException`) never triggers a reconnect. | step 02 Changes (`StringGetAsync`/`StringSetAsync`); `CacheKey.cs:12` | sample apps on master only | L | L | step 02 `GetOrAdd_KeyHoldsHash_FallsThroughAndOverwrites`; 2.0.0 unpublished | accepted |
| 9 | **Tests cannot substitute the internal factory.** Steps 01, 03, and 04 build an NSubstitute `IRedisMultiplexerFactory`. Castle DynamicProxy cannot proxy an internal interface without `InternalsVisibleTo("DynamicProxyGenAssembly2")`, which the package does not declare (`SolTechnology.Core.Cache.csproj:23-25`). The tests fail at runtime, and the quick fix (adding that attribute to the shipped csproj) is an unplanned production change. | step 01 Affected components (`TestRedisConnection` "NSubstitute `IRedisMultiplexerFactory`") | internal | L | H | none — gap | hand-written `FakeMultiplexerFactory` in the test project → **step 01** (used by **03**, **04**) |
| 10 | **Lock or idempotency store without `AddSolDistributedCache` stops resolving.** In 1.x and 2.0-pre these services needed only some registered `IConnectionMultiplexer`, plus an `IOptions<DistributedCacheConfiguration>` that resolves with defaults. After step 01 they need the internal `RedisConnectionProvider`, and the DI error names an internal type the user cannot register. | `ModuleInstaller.cs:143`; step 01 Changes | public NuGet | M | L | `docs/Cache.md:295,297` already say "requires `AddSolDistributedCache`" | add to P-21 and the DI note → **step 05** |
| 11 | **Slower shutdown with Redis down.** Provider disposal closes the current and any pending multiplexer; each `CloseAsync(allowCommandsToComplete: true)` may wait up to `AsyncTimeout`. | step 03 Changes (`DisposeAsync()`/`Dispose()`) | public NuGet | L | L | today's single multiplexer has the same wait; at most two multiplexers with default options | accepted |

Checklist sweep (supplementary; evidence for every bullet):

- **cache.md**
  - TTL semantics: absolute `ExpirationSeconds`, unchanged; pinned by step 02 `GetOrAdd_Miss_SetsTtlWithinExpirationSeconds`.
  - Key format: byte-identical; pinned by `CacheKey_GenericItemType_HasNoAssemblyVersion` and `GetOrAdd_SameKeyDifferentItemType_DoesNotCollide`.
  - Stampede: no eviction change, and a swap flushes nothing (N/A).
  - Provider swap with warm payloads: scenario 8.
  - `abortConnect` at boot: kept by step 01 `Multiplexer_ConnectionStringAbortConnectTrue_…`, step 03 `Reconnect_ConnectionStringAbortConnectTrue_…`, and `AddSolDistributedCache_RedisDownAtStart_FailsOpen`.
- **di.md**
  - Registration removed: scenarios 3, 4, and 10.
  - Lifetime: the provider and every consumer are singletons; the health check is created per probe and resolves the singleton (no captive dependency); pinned by the `StrictValidation` tests in `ModuleResolutionTests.cs:17-21`.
  - Decorator order: none.
  - Options without validation: both new options are validated `> 0` (step 04); no ratio rule (scenario 7).
  - Installer not called: scenario 10.
- **build-and-nuget.md**
  - `TreatWarningsAsErrors` (`src/Directory.Build.props:6`) with `GenerateDocumentationFile`: new XML docs and `cref`s must compile; covered by the Release build criteria of steps 02–05.
  - Demoted warnings: not touched.
  - Transitive `Microsoft.Extensions.*`: a dependency is removed and none added; `Microsoft.Extensions.Caching.Memory` keeps `Caching.Abstractions`. Consumers that used `AddStackExchangeRedisCache` through the transitive reference get a compile error, which P-21 documents.
  - snupkg and metadata: untouched.
  - .NET 10: the new public members are non-nullable `TimeSpan` properties.
- **logging.md**
  - Existing templates: unchanged, pinned by the step 02 log assertions.
  - New EventIds `2301`–`2304`: no collision (`grep` finds no other `23xx` in `src`).
  - PII: no template carries `ConfigurationOptions` values; step 03 `Reconnect_LogsContainNoConnectionString` pins it with a
    real `password` option. Exception messages from StackExchange.Redis show endpoints, not passwords.
  - Hot path: `ReportError` takes one lock per failure and logs only when it reconnects.
  - NU1510: no new framework reference.
- **testing-kits.md**
  - `RedisFixture` is untouched.
  - The container test owns its container (`WithCleanUp(true)`, no reuse, fixed free port with one retry on a bind race).
  - The test sits in namespace `…Cache.Tests.Redis`, so it also triggers the shared `RedisTestEnvironment` set-up fixture. This is harmless and noted only.

#### Top 3 Risks

1. #1: a malformed connection string turns every fail-open Redis call into an exception. This is a regression from today, where the cache falls through.
2. #4: lambda or optional `IConnectionMultiplexer` resolutions (third-party health checks, OpenTelemetry, Data Protection) fail after the upgrade, which `ValidateOnBuild` cannot see. Readiness can take the whole service out.
3. #5: the health check, the only recovery signal on an idle pod, is cancelled by the kubelet's 1 s probe timeout before it reports. The feature then fails exactly where it is meant to help.

#### Required Mitigations Before Merge

- #9 → **step 01**: add `tests/SolTechnology.Core.Cache.Tests/FakeMultiplexerFactory.cs`, a hand-written internal fake with a
  per-call delegate and a call counter, and use it everywhere instead of `Substitute.For<IRedisMultiplexerFactory>()`.
  The package csproj gets no `DynamicProxyGenAssembly2`.
- #2 → **step 03**: grace period = parsed `AsyncTimeout` + `HeartbeatInterval`;
  `Reconnect_DisposesOldAfterGracePeriod` asserts the boundary at 2999/3000 ms for `asyncTimeout=2000` with the
  default 1 s heartbeat.
- #7 → **step 03**: prune each old multiplexer from the pending list once its grace disposal finishes. After provider
  disposal, `Multiplexer`/`GetDatabase()` throw `ObjectDisposedException` and create nothing. Tests
  `Reconnect_GraceDisposalDone_NoPendingMultiplexerLeft` and `Multiplexer_AfterDispose_ThrowsAndCreatesNothing`.
- #1 → **step 04**: `ReportError` reads only the already-published multiplexer (no creation; unpublished counts as
  disconnected) and is wrapped so it never throws. Tests `RedisConnectionSwapTests.ReportError_MultiplexerNeverCreated_DoesNotCreate` and
  `RedisConsumersAfterSwapTests.Consumers_MultiplexerCreationThrows_FailOpen`: the factory always throws
  `ArgumentException`; `GetOrAdd` returns the factory result, the lock returns `null`, `TryAdd` returns `true`, and
  the health check returns the failure status, with no exception escaping.
- #5 → **step 04**: the health check's caller-cancellation `catch` reports a `TimeoutException` before rethrowing; the
  policy counts it only while the published multiplexer is disconnected. Test
  `RedisConnectionSwapTests.HealthCheck_CallerCancelledWhileDisconnected_CountsTowardsReconnect`.
- #3, #4, #5, #6, #7, #10 → **step 05**: `docs/Cache.md` plus the brief's P-21 row and release-notes input name:
  - the silent `IDistributedCache` cases (HybridCache L2, an in-memory `IDistributedCache` taking over);
  - "at start" only under `ValidateOnBuild` (the Development default), otherwise at first resolution;
  - lambda and optional `IConnectionMultiplexer` consumers (third-party health checks, OpenTelemetry
    `AddRedisInstrumentation()`, Data Protection) with the migration;
  - the lock and idempotency store needing `AddSolDistributedCache`;
  - the probe-timeout rule (the health-check timeout must be below the probe timeout, and errors must arrive within
    `ReconnectErrorThreshold`);
  - the doubled connection count during a grace period, Cluster partial outages, and keeping `ReconnectMinInterval`
    above `asyncTimeout`;
  - the grace formula.
- #4, #6 → **step 06**: Follow-ups add reconnect jitter and an instrumentation hook for the package-owned connection.

#### Accepted Risks

- #6: synchronized reconnects, connection doubling, and Cluster or auth churn are bounded by the 60 s interval and
  mirror Microsoft's own `UseForceReconnect` rule. Jitter is a follow-up.
- #8: only unreleased builds ever wrote a hash under a 2.0 key; the self-heal is pinned by a step 02 test.
- #11: at most two multiplexers with default options; the same wait exists today with one.
- Password rotation and token credentials: a force reconnect re-uses the static connection string. This is out of
  scope by user decision (2026-11-04) and already a step 06 follow-up.

#### Decision

**Go with mitigations.** The one `H` code risk (#1) has a cheap, testable fix. The other `H` (#4) is a user-confirmed
breaking change in a MAJOR whose remaining gap is documentation. The semver classification matches the diff.

User-only decisions (do not block the gate; state them to the user in Polish):

- **Walidacja connection stringa przy starcie:** czy dodać regułę, że `ConfigurationOptions.Parse(ConnectionString)`
  musi się udać przy `ValidateOnStart` (literówka w connection stringu zatrzyma start hosta zamiast działać fail-open
  bez Redisa)? Domyślnie: nie. Mitygacja #1 wystarcza do bramki.
- **Instrumentacja połączenia Redis pakietu:** po C-31 OpenTelemetry (`AddRedisInstrumentation()`) i zewnętrzne health
  checki nie widzą połączenia pakietu. Czy akceptujesz to w 2.0 z follow-upem (step 06), czy chcesz w 2.0 hook, np.
  publiczną opcję? Hook byłby nowym publicznym API i wymaga potwierdzenia wg `CLAUDE.md §2`.

## Open questions
- none (Q1 of [step 03](03-connection-swap.md) resolved by the user on 2026-11-04: option A, no `IConnectionMultiplexer` registration; the DI contract above is final)

## Deviations
