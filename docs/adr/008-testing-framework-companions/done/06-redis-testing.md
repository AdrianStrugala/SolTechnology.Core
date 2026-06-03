---
adr: 008-testing-framework-companions
step: 06 of 11
status: done
---

<!-- Reviewed: renumbered from to-do/05-redis-testing.md. Step-01 references → step 02;
     no test project. -->

<!-- IMPLEMENTATION DECISIONS (done):
  1. `RedisFixture` is an `IAsyncDisposable` POCO (matches `SQLFixture` style; the plan's `IAsyncLifetime`
     mention was xUnit-flavoured — this repo's companions expose `InitializeAsync` + `DisposeAsync`
     directly, NUnit-friendly).
  2. Exposes `HostName` (`host:port` for `Redis:HostName`) and `ConnectionString` (StackExchange shape),
     plus `WithNetwork(network, alias)` for parity with `SQLFixture`.
  3. Reuse: defers to `TestContainersContext.ReuseContainers` (step 02). `WithCleanUp(!Reuse)`, and when
     reuse is on → `WithName(containerName).WithReuse(true)`; `DisposeAsync` is a no-op. No hand-rolled
     reuse cache.
  4. `FlushAsync()` — SMOKE CAUGHT A REAL BUG: `FLUSHALL` is a server/admin command, rejected by
     StackExchange.Redis unless `AllowAdmin = true`. Fixed by parsing the connection string into
     `ConfigurationOptions` with `AllowAdmin = true` before connecting. Multiplexer is cached + disposed.
  5. Packages: `Testcontainers.Redis` 3.9.0 (match Testcontainers family) + `StackExchange.Redis` 2.8.16.
     Both CVE-clean (validate_cves). Added to canonical-versions.md.
  6. No coupling to `SolTechnology.Core.Cache` runtime types — fixture only provides a running Redis.

  Validation: build -c Release → 0 errors. Manual smoke (throwaway console, since no test project):
  container boots, PING ok, SET/GET ok, FlushAsync clears the key, container disposes → SMOKE_OK EXIT=0.
  Smoke project deleted after the run (nothing added to tests/). -->

# Step 06: `SolTechnology.Core.Redis.Testing`

## Summary
Package the Redis Testcontainer fixture currently inlined in the MTS `ApiFixture` and KYC
infrastructure into a standalone companion of the cache/Redis stack. Single small PR — one fixture,
one concern.

## Affected components
- `src/SolTechnology.Core.Redis.Testing/SolTechnology.Core.Redis.Testing.csproj` — new package (`Testcontainers.Redis`), version `0.1.0`. Depends on `SolTechnology.Core.Testing`.
- `src/SolTechnology.Core.Redis.Testing/RedisFixture.cs` — `IAsyncLifetime`/`IAsyncDisposable` POCO exposing `HostName`/`ConnectionString` (generalise MTS `RedisBuilder` usage: `redis:7-alpine`, random host port, cleanup + auto-remove).
- `docs/Cache.md` / `docs/Redis.md` — note the companion (full pass in step 11).
- `SolTechnology.Core.slnx` — add project.

## Details
- Expose connection string in the shape apps wire today (`Redis:HostName` = `host:port`, `Redis:Enabled`).
- **Consume the shared lifetime model from step 02**: booted once by the consumer's assembly-level `[OneTimeSetUp]` (within-run reuse free); across-run reuse via `TestContainersContext`'s `TESTCONTAINERS_REUSE` policy (Testcontainers-native `.WithReuse(true)` + stable name); dispose no-op when reuse on. No `ContainerReuse` helper — do not hand-roll a reuse cache.
- No coupling to `SolTechnology.Core.Cache` runtime types — fixture only provides a running Redis + connection string.
- Optional `FlushAsync()` helper for between-test reset (the reset path when the container is reused).
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.Redis.Testing.Tests`; validation is build-based plus a documented manual smoke (container starts, `PING` succeeds). Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.Redis.Testing` succeeds. ✅
- A documented manual smoke confirms the fixture starts a container and a `PING` succeeds. ✅
- `FlushAsync()` clears keys (documented manual smoke). ✅

## Open questions
- Confirm `Testcontainers.Redis` version via `package-management` skill. → **3.9.0** (matches Testcontainers family).

