---
adr: 008-testing-framework-companions
step: 06 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/05-redis-testing.md. Step-01 references → step 02;
     no test project. -->

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
- `dotnet build src/SolTechnology.Core.Redis.Testing` succeeds.
- A documented manual smoke confirms the fixture starts a container and a `PING` succeeds.
- `FlushAsync()` clears keys (documented manual smoke).

## Open questions
- Confirm `Testcontainers.Redis` version via `package-management` skill.

