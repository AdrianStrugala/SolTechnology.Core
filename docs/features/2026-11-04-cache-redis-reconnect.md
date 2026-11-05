---
status: abandoned
created: 2026-11-04
completed: 2026-11-05
---

# Cache — Redis connection recovers at runtime

> Historical delivery record. It may not describe the current system.

## Goal

The Redis connection of `SolTechnology.Core.Cache` recovers at runtime, not only at host start, and ships in 2.0.
Success means:

- One internal singleton connection provider owns the current `ConnectionMultiplexer`. `IRedisCache`, the Redis
  lock (including its release handle), the Redis idempotency store, and the Redis health check take the
  `IDatabase` from it on every call, so after a reconnect nothing in the package holds a stale or disposed
  multiplexer.
- `IRedisCache` talks to `IDatabase` directly. `AddSolDistributedCache` no longer calls `AddStackExchangeRedisCache`,
  and the package no longer depends on `Microsoft.Extensions.Caching.StackExchangeRedis`. Keys stay byte-identical
  (`{InstanceName}{TKey type}:{TItem type}:{JSON key}`), the absolute expiry is still `ExpirationSeconds`, and a
  Redis failure still falls through to the factory.
- When connection errors persist longer than `DistributedCacheConfiguration.ReconnectErrorThreshold` (default 30 s)
  and at least `DistributedCacheConfiguration.ReconnectMinInterval` (default 60 s) passed since the last reconnect,
  the provider builds a new multiplexer (`AbortOnConnectFail = false`), swaps it atomically, and disposes the old one
  after a grace period. One reconnect runs at a time, timing uses `TimeProvider`, both options are validated at
  start, and every reconnect is logged with a stable `EventId` and bracketed placeholders.

## Context

**Requested by:** the user on 2026-11-04 ("Redis connection must recover at runtime, not only at start"). Scope,
the out-of-scope items, and the two new public options (`CLAUDE.md §2`) were confirmed by the user on 2026-11-04.

**Q1 resolved (user, 2026-11-04): option A.** `AddSolDistributedCache` stops registering `IConnectionMultiplexer` in
DI (step 03). Applications that inject it get a DI validation error at start and must register their own connection.
Removing a public DI registration is a breaking change; the user's answer is the `CLAUDE.md §2` confirmation (row
`C-31`, P-21). Rejected: B (a registration that returns the current multiplexer, which a singleton holder sees
disposed after the first reconnect) and C (keep the initial multiplexer alive forever, which leaks a connection the
package gave up on).

**Depends on:**

- [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md) is `completed` (2026-11-04). This feature changes
  what that feature delivered: the shared fail-open multiplexer (`943540f9`) and the `IDistributedCache` reuse
  through `RedisCacheOptions.ConnectionMultiplexerFactory` (cache step 03).
- [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) depends on this feature
  being `completed` (dated line added to its "Depends on" list on 2026-11-04). Release 05 writes the rows under
  "Umbrella-brief rows" below into the 2.0 release notes.

**Clean-room note:** the idea mirrors a pattern the user knows from an internal company service (a singleton
connection provider that reconnects on failure). No proprietary code is referenced or copied. The design follows
the public StackExchange.Redis documentation and Microsoft's public guidance only:

- Azure Cache for Redis, "Best practices for connection resilience", section "Using ForceReconnect with
  StackExchange.Redis" (`https://learn.microsoft.com/azure/azure-cache-for-redis/cache-best-practices-connection`):
  keep a singleton `ConnectionMultiplexer` and force a new one when `RedisConnectionException` or socket errors
  persist; use `RedisTimeoutException` as a trigger only with generous intervals, because new connections can
  cascade an overloaded server; users of the old multiplexer must tolerate `ObjectDisposedException`.
- The MIT-licensed `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0 `RedisCache` (dotnet/aspnetcore
  `release/10.0`, `src/Caching/StackExchangeRedis/src/RedisCache.cs`) implements the same rule behind the
  `Microsoft.AspNetCore.Caching.StackExchangeRedis.UseForceReconnect` app-context switch, with
  `ReconnectMinInterval` 60 s and `ReconnectErrorThreshold` 30 s.

### Current state (`master@0e9498ef`)

| Symbol | File | Role |
|---|---|---|
| `ModuleInstaller.AddSolDistributedCache(DistributedCacheConfiguration)` | `src/SolTechnology.Core.Cache/ModuleInstaller.cs:54` | Validates options, calls `AddStackExchangeRedisCache`, sets `RedisCacheOptions.ConnectionMultiplexerFactory` to the DI multiplexer, registers a singleton `IConnectionMultiplexer` built with `ConfigurationOptions.Parse` + `AbortOnConnectFail = false`, registers `IRedisCache` |
| `RedisCache(IDistributedCache, IOptions<DistributedCacheConfiguration>, ILogger<RedisCache>)` | `src/SolTechnology.Core.Cache/RedisCache.cs:21` | `GetOrAdd` through `IDistributedCache.GetAsync`/`SetAsync`, `AbsoluteExpirationRelativeToNow = ExpirationSeconds`, Warning and fall-through on failure |
| `RedisDistributedLockService(IConnectionMultiplexer, …)` and `LockHandle(IDatabase, …)` | `src/SolTechnology.Core.Cache/RedisDistributedLockService.cs:13,57` | The handle keeps the `IDatabase` of the acquisition until release, which can be up to 365 days later |
| `RedisIdempotencyStore(IConnectionMultiplexer, …, TimeSpan ttl)` | `src/SolTechnology.Core.Cache/RedisIdempotencyStore.cs:19` | `redis.GetDatabase()` per call |
| `RedisHealthCheck(IConnectionMultiplexer, TimeSpan)` | `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheck.cs:12` | `PING` per probe |
| `RedisHealthCheckExtensions.AddSolRedisHealthCheck(...)` | `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheckExtensions.cs:24` | Resolves `IConnectionMultiplexer` per probe |
| `DistributedCacheConfiguration` | `src/SolTechnology.Core.Cache/DistributedCacheConfiguration.cs` | `ConnectionString`, `InstanceName`, `ExpirationSeconds` |

Facts that shape the plan:

- `ModuleInstaller.cs` is exactly 150 lines, the Guide §9 cap. Step 04 moves the option validation into a new
  internal file, so the two new options fit.
- Microsoft's `RedisCache` caches the `IDatabase` it got from `ConnectionMultiplexerFactory` for its lifetime, and its
  `Dispose()` and its force-reconnect path both call `Close()` and `Dispose()` on that multiplexer
  (`ReleaseConnection`). With a shared multiplexer that the provider swaps, `IDistributedCache` would keep the old
  one, and the app-context switch would dispose the multiplexer every other consumer uses. That is why `IRedisCache`
  must leave `IDistributedCache` before swapping exists (step 02 before step 03).
- Microsoft's `RedisCache` stores an entry as a Redis hash (`absexp`, `sldexp`, `data`) and sets the TTL with a
  separate `EXPIRE` sent alongside `HSET` (not atomic). The `IRedisCache` keys are new in 2.0 (P-07), and 2.0.0 is
  not published (release feature `planned`), so no released version wrote a hash under a 2.0 key.
- `docs/Cache.md:293` ("What ships in DI") lists `IRedisCache`, `IDistributedCache`, and one `IConnectionMultiplexer`
  for `AddSolDistributedCache`. No `src/` project other than Cache resolves `IConnectionMultiplexer` or
  `IDistributedCache`; `src/SolTechnology.Core.Api` only mentions `AddSolRedisHealthCheck()` in an XML example, and
  DreamTravel uses only the local cache.
- `Microsoft.Extensions.Caching.StackExchangeRedis` (`Directory.Packages.props:29`, `10.0.1`) is referenced only by
  `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj:17`. `StackExchange.Redis` `3.0.0` stays a direct
  reference. `IDistributedCache` stays available to tests through `Microsoft.Extensions.Caching.Memory` →
  `Microsoft.Extensions.Caching.Abstractions`.
- `nuget-stats.json` has download counts only: `SolTechnology.Core.Cache` `1.0.0`, 1,494 downloads. The change removes
  a dependency and adds none, so the `CLAUDE.md §2` new-dependency check does not apply. Consumers that used
  `Microsoft.Extensions.Caching.StackExchangeRedis` types through Cache's transitive reference must reference it
  themselves (P-21).
- `tests/SolTechnology.Core.Cache.Tests/CapturingLogger.cs` records level, template, and properties, but not
  `EventId` or the exception; the reconnect tests need both.
- EventIds `2137`–`2151` are taken or planned (Logging, CQRS, SQL). Cache has none yet; this feature takes
  `2301`–`2304`.
- `RedisFixture` (`src/SolTechnology.Core.Redis.Testing/RedisFixture.cs:62`) binds a random host port, so a
  stop/start of its container can change the port. The container test builds its own container with a fixed free
  host port and leaves the shared fixture alone.
- StackExchange.Redis `3.0.0` behaviour (source at tag `3.0.0`, verified by the plan review on 2026-11-04):
  - a command that times out in the backlog throws `RedisConnectionException` (inner: the bridge's last connection
    exception) when a connection error is known, otherwise `RedisTimeoutException` (`ExceptionFactory.Timeout`);
  - `RedisTimeoutException` derives from `System.TimeoutException`; `RedisConnectionException.FailureType` is a
    `ConnectionFailureType`, which includes `SocketFailure`, `SocketClosed`, `UnableToConnect`, and `ConnectionDisposed`;
  - `IsConnected` is `true` when any server endpoint is connected;
  - the heartbeat marks a connection with timed-out commands and no read for 4 × `AsyncTimeout` as `SocketFailure`
    (dead-socket detection), after which `IsConnected` is `false` for a single endpoint;
  - `ConfigurationOptions.AsyncTimeout` defaults to `SyncTimeout` (5000 ms);
  - `DisposeAsync()` calls `CloseAsync(allowCommandsToComplete: true)`, which sends `QUIT` and waits up to
    `AsyncTimeout`; commands issued on a disposed multiplexer throw `ObjectDisposedException`.
- `RedisConnectionProvider` becomes a DI singleton that the container disposes. A container disposed synchronously
  (`ServiceProvider.Dispose()`) throws `InvalidOperationException` for a resolved singleton that implements only
  `IAsyncDisposable`; `ConnectionMultiplexer` implements both, so the provider implements both.
- `AddSolRedisHealthCheck()` today resolves any registered `IConnectionMultiplexer`. After step 01 it resolves
  `RedisConnectionProvider`, so it requires `AddSolDistributedCache` (recorded in P-21).

### Design alternatives (blue-red-team, 2026-11-04)

#### A. Where reconnect lives

**Proposal:** an internal `RedisConnectionProvider` singleton owns the multiplexer and the force-reconnect rule, and
every Cache consumer takes `IDatabase` from it per call.

- **Blue:** one owner for creation, swap, and disposal; consumers stay two lines longer at most (`ReportError` in
  their existing `catch`); the fail-open table in `docs/architecture/cache.md` is unchanged; `TimeProvider` and a
  factory seam make the rule testable without Docker; no public type is added.
- **Red:** another internal type with concurrency (swap, grace disposal, single flight) inside a 150-line budget;
  the provider only sees errors that consumers report, so an idle process reconnects only through the health check.
- **Alternatives rejected:**
  - *Rely on StackExchange.Redis's own reconnect only* — the Azure guidance documents cases where it does not
    recover (for example the Linux 15-minute TCP retransmission case linked from the same page).
  - *Enable Microsoft's `UseForceReconnect` switch* — covers only `IDistributedCache` and disposes the multiplexer
    shared with the lock, the idempotency store, and the health check.
  - *A public `IRedisConnection` abstraction* — new public API that the user did not confirm; consumers of the
    package do not need it to get recovery.
  - *Feed `ConnectionMultiplexer.ConnectionFailed` / `ConnectionRestored` into the policy* (added by the plan review,
    2026-11-04) — would open the error window without traffic, but needs subscribe/unsubscribe on every swap and
    events from the library's own retries; deferred to a follow-up (step 06).
- **Crux:** whether errors seen by consumers are enough of a signal. The container test in step 04 proves the swap
  path end to end (Redis stopped, errors reported by the cache and the lock, a reconnect logged, recovery after
  restart, no `ObjectDisposedException`). It cannot reproduce a connection the library never recovers; that case
  rests on the Azure guidance and on dead-socket detection turning silent timeouts into counted errors.
- **Recommendation:** proceed.

#### B. `IRedisCache` value layout after leaving `IDistributedCache`

**Proposal:** store an entry as a Redis string, `SET {key} {UTF-8 JSON} EX {ExpirationSeconds}`, read it with `GET`.

- **Blue:** one atomic command sets the value and the TTL, where `HSET` + `EXPIRE` can leave an entry without TTL if
  the connection drops between them; the lock and idempotency store already use strings; `GET` returns the JSON as
  is, which `redis-cli` users can read; no `absexp`/`sldexp` metadata that `IRedisCache` never used (absolute expiry
  only).
- **Red:** a persisted layout change, which the premortem cache checklist flags ("Distributed cache provider swap
  with in-flight serialised payloads"); a warm hash under a 2.0 key makes `GET` fail with `WRONGTYPE`.
- **Alternative rejected:** *keep Microsoft's hash layout* (`HSET absexp/sldexp/data` + `EXPIRE`, `HGET data`) — it
  preserves a layout that only unreleased 2.0 builds wrote, and keeps the non-atomic TTL.
- **Crux:** does any released version write a hash under a 2.0 key? No: 2.0.0 is unpublished, and 1.x keys have no
  type prefix (P-07). An unreleased build's hash self-heals: `GET` throws `RedisServerException` (`WRONGTYPE`), the
  cache logs the existing Warning and falls through, and `SET` overwrites the key with a string. Step 02 pins this
  with a test. `WRONGTYPE` is not a connection error and never triggers a reconnect.
- **Recommendation:** proceed with the string layout (P-22).

#### C. Trigger classification

User scope: "`RedisConnectionException` / socket-failure-type errors". StackExchange.Redis 3.0.0 has no
`RedisSocketFailureException` type (its exceptions are `RedisException`, `RedisConnectionException`,
`RedisTimeoutException`, `RedisServerException`, `RedisCommandException`; socket failures are
`RedisConnectionException` with `ConnectionFailureType.SocketFailure`). The plan counts:

- `RedisConnectionException`, except `FailureType == ConnectionFailureType.ConnectionDisposed` (raised by the
  package's own disposal of a replaced multiplexer);
- `System.Net.Sockets.SocketException`, as the error or any inner exception;
- `TimeoutException` (including `RedisTimeoutException` and the health check's `WaitAsync` probe timeout) only while
  the current multiplexer reports `IsConnected == false` (a command that timed out in the backlog because no
  connection was available). A timeout on a connected multiplexer means a slow or overloaded server and does not
  count, per the Azure cascade warning. A half-open socket that only produces timeouts counts once dead-socket
  detection marks the connection failed (see "Facts that shape the plan").

Nothing else counts: `RedisServerException`, `RedisCommandException`, `JsonException`, `ObjectDisposedException`
(an in-flight call on the multiplexer being disposed), and `OperationCanceledException`. `IsConnected` is read from
the current multiplexer at report time, not from the one that raised the error.

Premortem decisions (delegated by the user, decided by the coordinator, 2026-11-04):
- `ConnectionString` is parsed with `ConfigurationOptions.Parse` during options validation at start; a malformed string stops the host (configuration error, fail fast). An unreachable Redis still starts and fails open.
- No public hook exposing the package's connection to OpenTelemetry or third-party health checks in 2.0; recorded as a follow-up.

## Scope

### In scope

- `src/SolTechnology.Core.Cache`: the connection provider, the factory seam, the reconnect policy, the four consumers,
  `RedisCache` on `IDatabase`, `DistributedCacheConfiguration.ReconnectErrorThreshold` and `ReconnectMinInterval`
  with start validation, reconnect log messages, the removal of `Microsoft.Extensions.Caching.StackExchangeRedis`.
- `Directory.Packages.props` (remove the unused `PackageVersion`) and
  `.github/skills/package-management/references/canonical-versions.md` (remove its row).
- `tests/SolTechnology.Core.Cache.Tests` (NUnit, FluentAssertions, NSubstitute, `FakeTimeProvider`, Testcontainers
  through `SolTechnology.Core.Redis.Testing`).
- `docs/Cache.md`, XML docs, `.github/skills/premortem/references/cache.md`; `docs/architecture/cache.md` in the
  retrospective.

### Out of scope

- Periodic forced refresh of the connection and token/credential hooks (Entra ID, AWS IAM) — user decision,
  2026-11-04; the retrospective records them as follow-ups.
- A fail-fast backlog policy or timeout guidance (follow-up of the cache feature); force reconnect does not change how
  long a call waits in the backlog during an outage.
- StackExchange.Redis 3.0 → 3.4 (outdated-package sweep).
- A public connection abstraction, and a public option to disable force reconnect.
- `SolTechnology.Core.Redis.Testing` public API (`RedisFixture` keeps its random port).

## Implementation plan

Steps lived in the temporary working folder `2026-11-04-cache-redis-reconnect/`, deleted when the feature was
abandoned on 2026-11-05 (last state of each step under "Completion summary").
Gates: `review: done (2026-11-04)` (public API and DI contract change); `premortem: go-with-mitigations (2026-11-04)` (`ModuleInstaller.cs` edit and the
`IRedisCache` value layout, a persisted contract, `CLAUDE.md §4`).

Notation: `cache-reconnect NN` names step `NN` of this feature in the umbrella-brief rows.

| Step | Title | Concern |
|---|---|---|
| 00 | Run premortem | Gate |
| 01 | Route every Redis consumer through an internal connection provider | Provider owns the initial multiplexer; consumers take `IDatabase` per call; no behaviour change |
| 02 | `IRedisCache` on `IDatabase`; drop `IDistributedCache` and its package | Value layout, DI surface, dependency removal |
| 03 | Atomic connection swap with delayed disposal | Internal `Reconnect()`: single flight, swap, grace disposal, logs; removes the `IConnectionMultiplexer` DI registration (Q1 = A) |
| 04 | Force-reconnect trigger and public options | Policy, `ReconnectErrorThreshold`, `ReconnectMinInterval`, validation, consumers report errors, container test |
| 05 | Module docs and premortem checklist | `docs/Cache.md`, remaining XML docs, premortem reference |
| 06 | Retrospective | Architecture page, brief completion, working folder deletion |

Steps run in number order; each is one PR. Step 02 must precede step 03 (see "Facts that shape the plan").

Plan review (2026-11-04) changes, no scope change:

- Trigger classification: `TimeoutException` (not only `RedisTimeoutException`) counts while disconnected, so the
  health check's probe timeout feeds the policy; `RedisConnectionException` with `ConnectionDisposed` does not count.
- `RedisConnectionProvider` implements `IDisposable` as well as `IAsyncDisposable`.
- `Reconnect()` completes after the swap; the grace disposal runs as a tracked, cancellable background task.
- `RedisCache` takes `GetDatabase()` per command, never across the factory call.
- The `IConnectionMultiplexer` XML-doc sweep of `ModuleInstaller.cs` moves from step 05 to step 03.
- `AddSolRedisHealthCheck()` requiring `AddSolDistributedCache` is recorded in P-21 and `docs/Cache.md`.
- Step 01 lists the existing unit tests that construct the consumers directly.

Internal shape (exact names; implementers record any deviation in the step):

| Type | File | Members |
|---|---|---|
| `internal interface IRedisMultiplexerFactory` + `internal sealed class RedisMultiplexerFactory` | `RedisMultiplexerFactory.cs` | `IConnectionMultiplexer Create(ConfigurationOptions options)` → `ConnectionMultiplexer.Connect(options)` |
| `internal sealed class RedisConnectionProvider : IDisposable, IAsyncDisposable` | `RedisConnectionProvider.cs` | `IConnectionMultiplexer Multiplexer { get; }`, `IDatabase GetDatabase()`, `Task Reconnect()` (step 03), `void ReportError(Exception error)` (step 04), `void Dispose()`, `ValueTask DisposeAsync()` |
| `internal sealed class RedisReconnectPolicy` | `RedisReconnectPolicy.cs` | `bool ShouldReconnect(Exception error, bool isConnected, out TimeSpan errorDuration)`, `void Reconnected()` |
| `internal static partial class RedisConnectionLogMessages` | `RedisConnectionLogMessages.cs` | `[LoggerMessage]` methods for EventIds `2301`–`2304` |
| `internal static class DistributedCacheConfigurationValidation` | `DistributedCacheConfigurationValidation.cs` | `extension(OptionsBuilder<DistributedCacheConfiguration>)` with `ValidateDistributedCache()` holding all five rules |

Log contract (new, additive):

| EventId | Level | Template |
|---|---|---|
| 2301 | Warning | `Redis connection errors persisted for [{ErrorDurationMs}] ms; reconnecting` |
| 2302 | Information | `Redis connection replaced; the previous connection is disposed in [{GracePeriodMs}] ms` |
| 2303 | Error | `Redis reconnect failed; keeping the current connection` (with the exception) |
| 2304 | Warning | `Disposing the previous Redis connection failed` (with the exception) |

No template contains the connection string or any `ConfigurationOptions` value, because the connection string may
carry a password.

### Umbrella-brief rows (for the user to copy into `2026-11-02-core-2-0-flagship-hardening.md`)

Rows for "Public API changes requiring `CLAUDE.md §2` confirmation" (next free IDs after `C-29`):

```markdown
| C-30 | Cache (additive, user, 2026-11-04) | NEW `DistributedCacheConfiguration.ReconnectErrorThreshold` (`TimeSpan`, default 30 s) and `DistributedCacheConfiguration.ReconnectMinInterval` (`TimeSpan`, default 60 s), both validated `> TimeSpan.Zero` at start; when Redis connection errors persist longer than the threshold and the interval has passed since the last reconnect, the shared Redis connection is replaced (force reconnect) | cache-reconnect 04 |
| C-31 | Cache (breaking, user Q1 = A, 2026-11-04) | `AddSolDistributedCache` no longer registers `IConnectionMultiplexer` in DI; the Redis connection is internal to the package so a reconnect never disposes an instance an application holds; apps that inject `IConnectionMultiplexer` fail DI validation at start and register their own connection (P-21) | cache-reconnect 03 |
```

Rows for "Behavioural and persisted-contract breaks" (next free IDs after `P-20`):

```markdown
| P-21 | `AddSolDistributedCache` no longer registers the framework `IDistributedCache` (it no longer calls `AddStackExchangeRedisCache`), and `SolTechnology.Core.Cache` no longer depends on `Microsoft.Extensions.Caching.StackExchangeRedis`. It also no longer registers `IConnectionMultiplexer` (C-31, user Q1 = A, 2026-11-04), and `AddSolRedisHealthCheck()` requires `AddSolDistributedCache` instead of any registered `IConnectionMultiplexer` | Apps that injected `IDistributedCache` (or `RedisCacheOptions`) from `AddSolDistributedCache` fail DI validation at start; they call `AddStackExchangeRedisCache` themselves and add the package reference. Apps that injected `IConnectionMultiplexer` fail DI validation at start and register their own connection (for example `services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString))`). Apps that used `AddSolRedisHealthCheck()` with their own `IConnectionMultiplexer` and without `AddSolDistributedCache` fail when the check is resolved; they call `AddSolDistributedCache` | cache-reconnect 01, 02, 03 |
| P-22 | `IRedisCache` entries are Redis strings (`SET … EX`, value = camelCase UTF-8 JSON) instead of the `IDistributedCache` hash (`absexp`, `sldexp`, `data`); keys and the absolute `ExpirationSeconds` TTL are unchanged | None for 1.x upgrades (2.0 keys are new, P-07). A hash left under a 2.0 key by a pre-release build logs one `GET failed` Warning, falls through to the factory, and is overwritten | cache-reconnect 02 |
```

Q1 of step 03 is resolved (option A, user, 2026-11-04); the rows above are final for copying.

Release-notes input (append to "Release-notes inputs"):

```markdown
- cache (Redis reconnect, 2026-11-04, C-30, C-31, P-21, P-22): the shared Redis connection now recovers at runtime. One
  internal provider owns it; `IRedisCache`, the Redis lock (including its release), the idempotency store, and the
  health check take the database from it on every call. When connection errors (`RedisConnectionException`, socket
  errors, or timeouts while disconnected) persist for `ReconnectErrorThreshold` (30 s) and `ReconnectMinInterval`
  (60 s) has passed since the last reconnect, a new connection replaces the old one, which is disposed after the
  command timeout. New log events: 2301 (Warning, reconnecting), 2302 (Information, connection replaced), 2303
  (Error, reconnect failed), 2304 (Warning, disposing the old connection failed). `IRedisCache` no longer goes
  through `IDistributedCache`. Breaking: `AddSolDistributedCache` no longer registers `IDistributedCache` or
  `IConnectionMultiplexer`, and the package no longer depends on `Microsoft.Extensions.Caching.StackExchangeRedis`;
  an app that injects either service fails DI validation at start and registers its own (`AddStackExchangeRedisCache`,
  or its own `IConnectionMultiplexer` singleton). `AddSolRedisHealthCheck()` requires `AddSolDistributedCache`.
```

## Acceptance criteria

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] `grep -rn "IConnectionMultiplexer" src/SolTechnology.Core.Cache --include=*.cs` matches only
  `RedisConnectionProvider.cs` and `RedisMultiplexerFactory.cs` (Q1 = A: `ModuleInstaller.cs` registers no
  `IConnectionMultiplexer`).
- [ ] `grep -rn "IDistributedCache\|Caching.StackExchangeRedis" src/SolTechnology.Core.Cache Directory.Packages.props`
  prints nothing.
- [ ] The existing test `RedisCacheTests.CacheKey_GenericItemType_HasNoAssemblyVersion` passes unchanged (byte-identical key).
- [ ] A Redis outage of the container test is followed by at least one EventId `2302`, and after Redis restarts
  `IRedisCache`, the lock, and the health check work again with no `ObjectDisposedException` logged.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` reports 0 errors, and
  `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.Cache.Tests` is green with Docker running.
- [ ] `find src/SolTechnology.Core.Cache -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'`
  prints nothing.
- [ ] The Release pack of `SolTechnology.Core.Cache` has no `Microsoft.Extensions.Caching.StackExchangeRedis`
  dependency in its `.nuspec` and contains `lib/net10.0/SolTechnology.Core.Cache.xml`.
- [ ] Every C# snippet in `docs/Cache.md` compiles in a throwaway project under the git-excluded `.scratch/`.
- [ ] `C-30`, `C-31`, `P-21`, `P-22`, and the release-notes input are in the umbrella brief (copied by the user).

## Completion summary

**Abandoned on 2026-11-05 by the user** ("Porzuć duży plan. Zrób mały"). No production code was written; the plan
was reviewed and passed its premortem, and implementation never started.

**Reason.** An experiment on 2026-11-05 showed that the Redis connection of `SolTechnology.Core.Cache` already recovers
at runtime without a custom provider. Setup: StackExchange.Redis `3.0.0`, `Microsoft.Extensions.Caching.StackExchangeRedis`
`10.0.1`, and the exact wiring of the package on that day (one shared multiplexer with `AbortOnConnectFail = false`,
reused by Microsoft's `IDistributedCache` through `ConnectionMultiplexerFactory`, plus direct `IDatabase` use); Redis 7
in Docker on a fixed host port.

| Action | `IDistributedCache` path and direct `IDatabase` path |
|---|---|
| Start the app with Redis down | Both fail open |
| `docker start` | Both recover by themselves after 22 ms |
| `docker stop` | Both fail open |
| `docker start` again | Both recover after 33 ms |
| `docker restart` | Both recover after 13 ms |

StackExchange.Redis reconnects the existing multiplexer by itself, so the provider, the atomic swap, the
force-reconnect policy, the two public options (C-30), and the removal of the `IConnectionMultiplexer` registration
(C-31, a DI break) would add complexity and a breaking change without fixing an observed failure. The real problem
the experiment showed is latency: while Redis is down each call waits about 5–11 seconds (sync and async timeouts,
two Redis operations in `IRedisCache.GetOrAdd`) before it fails open. The user chose the small fix, delivered by
[`2026-11-05-cache-redis-fail-fast`](2026-11-05-cache-redis-fail-fast.md).

Last state of each step when the working folder was deleted (review `done (2026-11-04)`, premortem
`go-with-mitigations (2026-11-04)`):

| # | Step | Last state |
|---|---|---|
| 00 | Run premortem | done (Go with mitigations; mitigations folded into steps 01, 03, 04, 05, 06) |
| 01 | Route every Redis consumer through an internal connection provider | to-do |
| 02 | `IRedisCache` on `IDatabase`; drop `IDistributedCache` and its package | to-do |
| 03 | Atomic connection swap with delayed disposal | to-do |
| 04 | Force-reconnect trigger and public options | to-do |
| 05 | Module docs and premortem checklist | to-do |
| 06 | Retrospective | to-do |

The rows C-30, C-31, P-21, P-22 and the release-notes input under "Umbrella-brief rows" were never copied into
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); those IDs stay unused. The
dependency of [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) on this feature
was replaced by the fail-fast feature on 2026-11-05. The acceptance criteria above stay unchecked.

## Deviations

None; the plan was abandoned before implementation.

## Follow-ups

- Fail fast while Redis is down: delivered by [`2026-11-05-cache-redis-fail-fast`](2026-11-05-cache-redis-fail-fast.md).
- Force reconnect stays unplanned. Revisit it only if a failure that StackExchange.Redis does not recover from by
  itself is observed (for example a connection that stays broken after Redis is reachable again), or with token
  credentials (Entra ID, AWS IAM) that need a new connection.
