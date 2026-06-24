---
adr: 012-production-pattern-adoption-wave-2
step: 13 of 24
status: reviewed
---

# Step 13: A1.3 — Redis idempotency store via new glue package `SolTechnology.Core.Api.Idempotency.Redis`

## Summary
Add a distributed `IIdempotencyStore` implementation backed by the Redis `IDistributedCache` shipped
in ADR-010 (`Core.Cache`), as a **thin, opt-in glue package** `SolTechnology.Core.Api.Idempotency.Redis`
that references **both** `Core.Api` (for the public `IIdempotencyStore` + `StoredResponse` from
step 11) **and** `Core.Cache` (for `IDistributedCache`/Redis). It ships an `AddRedisIdempotencyStore()`
opt-in registration that replaces the in-memory default from step 12.

> **Blocker 2 resolution — option (ii), reviewer-approved.** `Core.Api` itself stays **Redis-free**:
> it does **not** take a `ProjectReference` on `Core.Cache`. The Redis dependency lives only in this
> separate glue package, so consumers who do not use Redis idempotency never pull `Core.Cache` into
> their API. This is the **third** new `src/` package in the wave.
>
> **New top-level `src/` folder confirmation: GIVEN.** The maintainer approved
> `src/SolTechnology.Core.Api.Idempotency.Redis/` (and its `tests/` counterpart) per CLAUDE.md §1.
>
> **Prerequisite (step 11):** `IIdempotencyStore` and `StoredResponse` MUST be **`public`** in
> `Core.Api` (they already are, per step 11 — "public where step 13's external store impl must
> implement `IIdempotencyStore`"), because this glue package is a **separate assembly** with no
> `InternalsVisibleTo`.

## Affected components
- `src/SolTechnology.Core.Api.Idempotency.Redis/SolTechnology.Core.Api.Idempotency.Redis.csproj` —
  **new** project (inherits `src/Directory.Build.props` → `TreatWarningsAsErrors=true`; `Version`
  `0.1.0`; metadata mirroring sibling `.csproj`; `ProjectReference`s to **both**
  `SolTechnology.Core.API` and `SolTechnology.Core.Cache`).
- `SolTechnology.Core.slnx` — add the new `<Project>` entry under `/src/`.
- `src/SolTechnology.Core.Api.Idempotency.Redis/RedisIdempotencyStore.cs` — `IIdempotencyStore` over
  `IDistributedCache` (serialise `StoredResponse`; atomic add via Redis `SET NX` semantics / the
  cache's add primitive).
- `src/SolTechnology.Core.Api.Idempotency.Redis/IdempotencyRedisServiceCollectionExtensions.cs` —
  `AddRedisIdempotencyStore()` that swaps the default store registration from step 12.
- `docs/Api.md` — document the Redis store option, that it ships as the separate
  `SolTechnology.Core.Api.Idempotency.Redis` package (so `Core.Api` stays Cache-free), and when to
  choose it (multi-instance).
- `tests/SolTechnology.Core.Api.Idempotency.Redis.Tests/` — **new** NUnit test project: distributed
  round-trip + atomic-add concurrency test (use the `Core.Redis.Testing` companion fixture per
  `package-management`). Wire it into `SolTechnology.Core.slnx` under `/Tests/`. (This additional new
  test folder is covered by the maintainer's blanket CLAUDE.md §1 approval of new test folders for
  this wave — **flagged here for transparency** since it was not named individually.)

## Details
- **Why a glue package, not a `Core.Api → Core.Cache` reference (Blocker 2).** A direct reference
  would force `Core.Cache` (and its `StackExchange.Redis` transitive surface) onto **every**
  `Core.Api` consumer, including single-box hosts that use only the in-memory store. The glue package
  keeps `Core.Api` dependency-honest; Redis arrives only when a host adds this package and calls
  `AddRedisIdempotencyStore()`.
- **Atomic add is the crux:** in a multi-instance deployment two nodes can receive the same
  `Idempotency-Key` simultaneously. The Redis store MUST use an atomic "add if absent" so exactly one
  node proceeds and the other sees the in-flight/duplicate state (mirrors the in-memory atomic add
  from step 11).
- **Serialisation:** persist `StoredResponse` with `System.Text.Json` (static options per ADR-010
  G2). Ensure the captured body bytes + headers round-trip.
- **TTL:** honour the `IdempotencyOptions` TTL from step 12 as the Redis key expiry.
- **Concern boundary:** this step is purely the alternate store + its registration in the new package
  — **no middleware changes** (the middleware from step 12 is store-agnostic via `IIdempotencyStore`)
  and **no change to `Core.Api`'s own csproj** (it stays Cache-free). `AddRedisIdempotencyStore()`
  re-registers `IIdempotencyStore` over the registration step 12 put in place.
- **Cross-references updated by this decision:** the publish workflow (step 23) gains a `Pack` step
  for this package, and the `00` premortem lists `IIdempotencyStore` / `StoredResponse` + this
  package as new public NuGet surface.

## Acceptance criteria
- `RedisIdempotencyStore` (in `SolTechnology.Core.Api.Idempotency.Redis`) round-trips a
  `StoredResponse` through `IDistributedCache`.
- Concurrent duplicate keys across two logical callers result in exactly one execution (atomic add
  verified).
- TTL from `IdempotencyOptions` is applied as the Redis expiry.
- `AddRedisIdempotencyStore()` replaces the in-memory default; the step-12 middleware is unchanged.
- `Core.Api` takes **no** `ProjectReference` on `Core.Cache`; the Redis dependency is isolated to the
  glue package, which is in `SolTechnology.Core.slnx` (`/src/`) with its tests in `/Tests/`.
- `docs/Api.md` documents the Redis option and the separate-package model.

## Open questions
- none — the placement is resolved (separate `SolTechnology.Core.Api.Idempotency.Redis` glue package;
  `Core.Api` stays Redis-free).

