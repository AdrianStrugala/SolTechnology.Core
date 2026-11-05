---
spec: 2026-11-04-cache-redis-reconnect
step: 04
status: to-do
---

# Step 04: Force-reconnect trigger and public options

## Summary
Add the force-reconnect rule from Microsoft's public Azure Cache for Redis guidance: every Redis consumer reports the
errors it already catches to the provider, and when connection errors persist longer than
`ReconnectErrorThreshold` and at least `ReconnectMinInterval` passed since the last reconnect, the provider calls
`Reconnect()` from step 03. The two options are new public members of `DistributedCacheConfiguration` (C-30,
confirmed by the user on 2026-11-04), validated at start. A container test stops and restarts Redis and checks
recovery.

## Affected components
- `src/SolTechnology.Core.Cache/DistributedCacheConfiguration.cs` — EDIT — `public TimeSpan ReconnectErrorThreshold { get; set; } = TimeSpan.FromSeconds(30);` and `public TimeSpan ReconnectMinInterval { get; set; } = TimeSpan.FromSeconds(60);` with XML docs
- `src/SolTechnology.Core.Cache/DistributedCacheConfigurationValidation.cs` — NEW — `internal static class DistributedCacheConfigurationValidation` with `extension(OptionsBuilder<DistributedCacheConfiguration> builder)` → `OptionsBuilder<DistributedCacheConfiguration> ValidateDistributedCache()` holding the three existing rules (moved verbatim from `ModuleInstaller.cs:65-72`) and the two new ones, then `ValidateOnStart()`
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` — EDIT — `Configure` copies `ReconnectErrorThreshold` and `ReconnectMinInterval`; the validation chain becomes `.ValidateDistributedCache()`
- `src/SolTechnology.Core.Cache/RedisReconnectPolicy.cs` — NEW — `internal sealed class RedisReconnectPolicy(TimeSpan errorThreshold, TimeSpan minInterval, TimeProvider timeProvider)`
- `src/SolTechnology.Core.Cache/RedisConnectionProvider.cs` — EDIT — owns a `RedisReconnectPolicy`; `internal void ReportError(Exception error)`
- `src/SolTechnology.Core.Cache/RedisConnectionLogMessages.cs` — EDIT — EventId `2301`
- `src/SolTechnology.Core.Cache/RedisCache.cs` — EDIT — `connection.ReportError(ex)` in the GET and SET `catch` blocks
- `src/SolTechnology.Core.Cache/RedisDistributedLockService.cs` — EDIT — `connection.ReportError(ex)` in the acquire and release `catch` blocks (not for `OperationCanceledException`)
- `src/SolTechnology.Core.Cache/RedisIdempotencyStore.cs` — EDIT — `connection.ReportError(ex)` in all four `catch` blocks
- `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheck.cs` — EDIT — `connection.ReportError(ex)` in the failure `catch`; `connection.ReportError(new TimeoutException(...))` in the caller-cancellation `catch` before the rethrow (premortem #5)
- `tests/SolTechnology.Core.Cache.Tests/RedisConsumersAfterSwapTests.cs` — EDIT — fail-open test when the multiplexer factory always throws (premortem #1)
- `tests/SolTechnology.Core.Cache.Tests/RedisReconnectPolicyTests.cs` — NEW — rule tests with `FakeTimeProvider`
- `tests/SolTechnology.Core.Cache.Tests/RedisConnectionSwapTests.cs` — EDIT — `ReportError` end-to-end through the provider
- `tests/SolTechnology.Core.Cache.Tests/ModuleInstallerTests.cs` — EDIT — option validation and option flow
- `tests/SolTechnology.Core.Cache.Tests/Redis/RedisReconnectContainerTests.cs` — NEW — stop/start container test

## Changes
- `RedisReconnectPolicy.ShouldReconnect(Exception error, bool isConnected, out TimeSpan errorDuration)`, under a private lock (error path only); `errorDuration` is `now - firstError` when it returns `true`, otherwise `TimeSpan.Zero`:
  1. Not a connection error → `false`. Connection errors: `RedisConnectionException` except `FailureType == ConnectionFailureType.ConnectionDisposed`; `SocketException` as the error or any inner exception; `TimeoutException` (including `RedisTimeoutException` and the health check's `WaitAsync` timeout) only when `isConnected == false`. Everything else (`RedisServerException`, `RedisCommandException`, `JsonException`, `ObjectDisposedException`, `OperationCanceledException`, `RedisConnectionException` with `ConnectionDisposed`, `TimeoutException` while connected) → `false`.
  2. `now - lastReconnect < minInterval` → `false`. `lastReconnect` starts at the policy's creation time.
  3. No error window open, or `now - previousError > errorThreshold` (a gap) → open a new window: `firstError = previousError = now`; `false`.
  4. Otherwise `previousError = now`; when `now - firstError >= errorThreshold` → close the window and return `true`.
- `RedisReconnectPolicy.Reconnected()` sets `lastReconnect = now` and closes the window; the provider calls it inside `Reconnect()` right after winning the single-flight gate (step 03), so direct `Reconnect()` calls count too and a failing `Create` also waits `minInterval` before the next try.
- `RedisConnectionProvider.ReportError(Exception error)`: returns at once after disposal; otherwise `policy.ShouldReconnect(error, isConnected, out var errorDuration)`, where `isConnected` comes from the already-published multiplexer field (`Volatile.Read`). `ReportError` never reads the `Multiplexer` property and never calls `factory.Create`; a multiplexer that was never published counts as `isConnected == false` (premortem #1: the consumers call `ReportError` from their `catch`, and a first creation that keeps throwing, for example on a malformed connection string, would otherwise escape the fail-open path). On `true` it logs `2301` Warning `Redis connection errors persisted for [{ErrorDurationMs}] ms; reconnecting` with `errorDuration` and starts `Reconnect()` without awaiting it (the caller's fail-open path is not delayed; the discarded task never faults because `Reconnect()` catches). The body is wrapped in `try`/`catch` that swallows everything, so it never throws.
- `RedisHealthCheck` caller-cancellation `catch` (premortem #5): call `connection.ReportError(new TimeoutException("Redis health probe cancelled by the caller"))`, then rethrow as today. Rule 1 counts a `TimeoutException` only while the published multiplexer is disconnected, so a cancelled probe on a connected multiplexer still counts nothing. Reason: the kubelet's default probe `timeoutSeconds: 1` aborts the request (and `HealthCheckMiddleware` passes `RequestAborted`) before the 5 s backlog timeout, so without this an idle, unready pod never feeds the policy.
- Defaults are Microsoft's: `ReconnectErrorThreshold` 30 s, `ReconnectMinInterval` 60 s.
- Validation messages: `DistributedCacheConfiguration.ReconnectErrorThreshold must be greater than 0.` and `DistributedCacheConfiguration.ReconnectMinInterval must be greater than 0.` (rule `> TimeSpan.Zero`; no upper bound).
- Container test setup: `new RedisBuilder("redis:7-alpine").WithPortBinding(freePort, 6379)` where `freePort` comes from a `TcpListener` on port 0, so `StopAsync`/`StartAsync` keeps the endpoint; connection string `127.0.0.1:{freePort},connectTimeout=500,asyncTimeout=500,syncTimeout=500`; `ReconnectErrorThreshold = 1 s`, `ReconnectMinInterval = 2 s`; `AddSolDistributedLock()` and `AddHealthChecks().AddSolRedisHealthCheck(timeout: TimeSpan.FromSeconds(1))`; logs captured by registering `CapturingLogger<>` as the open-generic `ILogger<>` singleton and read through `Snapshot()` of the resolved `ILogger<RedisConnectionProvider>`, `ILogger<RedisCache>`, and `ILogger<RedisDistributedLockService>` (cast to `CapturingLogger<T>`); the container is never reused (`WithCleanUp(true)`, no `WithReuse`).
- Container test timing: the outage loop runs for 6 s of wall-clock time; recovery is polled every 250 ms for up to 20 s, never a fixed sleep. Redis may restore its RDB snapshot on restart (the default image saves on `SIGTERM`), so every lock name used after the restart differs from any name acquired before the stop, and the cache assertion uses a key first written after the restart. The free-port probe (`TcpListener` on port 0, then stop) has a small reuse race; on a port-bind failure the test retries the container start once with a new port.

## Acceptance criteria
- [ ] `RedisReconnectPolicyTests` pass (defaults 30 s / 60 s; every time below is an offset after advancing `FakeTimeProvider` by `minInterval` from the policy's creation):
  - `ConnectionErrors_ShorterThanThreshold_DoNotReconnect` — errors at 0, 10, 20, 29 s → all `false`.
  - `ConnectionErrors_ForThreshold_Reconnect` — errors at 0, 10, 20, 30 s → `true` at 30 s only, with `errorDuration == 30 s`.
  - `ConnectionErrors_WithinMinIntervalOfLastReconnect_DoNotReconnect` — `Reconnected()` at 0 s; errors every 10 s from 10 s to 50 s → all `false`; errors at 60, 70, 80, 90 s → `true` at 90 s only.
  - `ErrorGapLongerThanThreshold_RestartsWindow` — errors at 0 s and 40 s, then 41 s → `false`.
  - `NonConnectionErrors_DoNotCount` — `[TestCase]` per excluded type, including `RedisTimeoutException` and `TimeoutException` with `isConnected: true`, and `RedisConnectionException(ConnectionFailureType.ConnectionDisposed, …)`.
  - `TimeoutWhileDisconnected_Counts` — `[TestCase]` for `RedisTimeoutException` and plain `TimeoutException` (the health check's `WaitAsync` timeout), `isConnected: false`.
  - `SocketExceptionAsInnerException_Counts`.
- [ ] `RedisConnectionSwapTests.ReportError_PersistentConnectionErrors_ReconnectsOnceAndLogs2301And2302` — with `FakeTimeProvider` and a substitute multiplexer whose `IsConnected` is `false`; the test awaits a `TaskCompletionSource` completed by the second `Create` call (bounded `WaitAsync(TimeSpan.FromSeconds(5))`) and then the `2302` entry via a bounded poll of `Snapshot()`; `Create` is called exactly twice.
- [ ] `RedisConnectionSwapTests.ReportError_AfterDispose_DoesNothing` — no `Create` call and no log.
- [ ] `RedisConnectionSwapTests.ReportError_MultiplexerNeverCreated_DoesNotCreate` (premortem #1) — `ReportError(new RedisConnectionException(ConnectionFailureType.SocketFailure, "x"))` on a provider whose `Multiplexer` was never accessed: `FakeMultiplexerFactory.CreateCalls == 0` and no exception.
- [ ] `RedisConsumersAfterSwapTests.Consumers_MultiplexerCreationThrows_FailOpen` (premortem #1) — `FakeMultiplexerFactory` always throws `ArgumentException`: `GetOrAdd` returns the factory result, `TryAcquireLock` returns `null`, `IIdempotencyStore.TryAdd` returns `true`, `RedisHealthCheck.CheckHealthAsync` returns the registration's failure status, and no call throws.
- [ ] `RedisConnectionSwapTests.HealthCheck_CallerCancelledWhileDisconnected_CountsTowardsReconnect` (premortem #5) — substitute multiplexer with `IsConnected == false` whose `PingAsync` never completes; probes cancelled by the caller every 10 s of `FakeTimeProvider` time (after `minInterval`) each throw `OperationCanceledException`, and after the 30 s threshold `Create` was called a second time; the same test with `IsConnected == true` leaves `CreateCalls == 1`.
- [ ] `ModuleInstallerTests.AddSolDistributedCache_ReconnectOptionNotPositive_HostStartFails` — `[TestCase]` zero and `-1 s` for each option; host start throws `OptionsValidationException` naming the option.
- [ ] `ModuleInstallerTests.AddSolDistributedCache_ReconnectOptions_FlowIntoOptions` — `ReconnectErrorThreshold = 7 s`, `ReconnectMinInterval = 11 s` appear in `IOptions<DistributedCacheConfiguration>.Value`.
- [ ] `new DistributedCacheConfiguration()` has `ReconnectErrorThreshold == 30 s` and `ReconnectMinInterval == 60 s` (test `ModuleInstallerTests.DistributedCacheConfiguration_Defaults_MatchForceReconnectGuidance`).
- [ ] `RedisReconnectContainerTests.RedisStoppedAndRestarted_ConnectionRecovers`: after `StopAsync`, `GetOrAdd` and `TryAcquireLock` keep failing open for 6 s (factory result, `null`); at least one captured log has `EventId.Id == 2301` and one `2302`; after `StartAsync`, within 20 s of polling, `GetOrAdd` returns the cached value on its second call, `TryAcquireLock` returns a handle, and `HealthCheckService` reports `Healthy`; no captured log of the three loggers carries an `ObjectDisposedException` (directly or as an inner exception).
- [ ] `ModuleInstallerTests.AddSolDistributedCache_RedisDownAtStart_FailsOpen` still passes.
- [ ] `find src/SolTechnology.Core.Cache -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `perl -ne 'print "$ARGV:$.: $_" if /(Log\w*\(|Message\s*=)/ && /"[^"]*(?<!\[)\{[A-Z]\w*\}(?!\])[^"]*"/; close ARGV if eof' src/SolTechnology.Core.Cache/*.cs src/SolTechnology.Core.Cache/HealthChecks/*.cs` prints nothing (bracketed placeholders; verified on 2026-11-04 to print nothing on `master@0e9498ef` and to flag `LogWarning("bad {Key} here")`). The `[LoggerMessage]` templates keep `Message =` on the same line as the template string.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` reports 0 errors; `dotnet test tests/SolTechnology.Core.Cache.Tests` is green with Docker running.

## Open questions
- none

## Deviations
