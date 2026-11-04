---
spec: 2026-11-02-core-2-0-cache
step: 06
status: done
---

# Step 06: Module docs — `docs/Cache.md`

## Summary
`docs/Cache.md` uses `AddLocalCache`, `AddDistributedCache`, `AddLocalLock`, and similar methods,
which do not exist (`logging-cache#15`, `:9-10,31,44,207-212,240-244,334-341`). Every C# snippet
must compile against the 2.0 API.

Former core step 60 (Cache part); split per library on 2026-11-02.

## Affected components
- `docs/Cache.md` — EDIT — `AddSolLocalCache`, `AddSolDistributedCache`, `AddSolLocalLock`, `AddSolDistributedLock`, `AddSolLocalIdempotency`, `AddSolDistributedIdempotency`; `InstanceName` must be non-empty (validated at start, P-07); `:272` lock expiry semantics; `C-13` names

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Cache`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigation (core step 00, 2026-11-02), documentation part:
  - RM-2 (scenario 2): `docs/Cache.md` states that `InstanceName` prefixes cache, lock (`lock:`), and idempotency (`idempotency:`) keys, and that `InstanceName = "SolTechnology:"` keeps the 1.x lock and idempotency keyspace during an upgrade.

## Acceptance criteria
- [x] `grep -nE "\bAdd(Local|Distributed)(Cache|Lock|Idempotency)\b" docs/Cache.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] RM-2: `grep -n "SolTechnology:" docs/Cache.md` matches the new text.

## Open questions
- none

## Deviations

### Page rewritten to the canonical structure

**Original plan:** replace the 1.x method names, state the `InstanceName` rule, the lock expiry semantics, the `C-13`
names, and the RM-2 keyspace note.

**Actual implementation:** the page was rewritten to the `PublicDocumentationGuide.md` structure (lead, Features,
Registration, Configuration, Usage, Testing, Conventions, What ships in DI; 295 lines, down from 430), because nearly
every section needed 2.0 changes. Besides the planned items it documents single flight, fault eviction, the
type-namespaced key layout per store, `ExpirationSeconds > 0` (P-20), the shared multiplexer, `AddSolRedisHealthCheck`,
and the `TimeProvider` used by the local lock; `AddHTTPClient` became `AddSolHTTPClient`. The 1.x key-prefix note is
an "Upgrading from 1.x" paragraph in Configuration. The idempotency middleware recipe stays, with braces and
`StatusCodes.Status409Conflict`; the Testing example builds a `RedisFixture`-backed provider instead of the
non-compiling `TestConfigurationBuilder` fragment.

### Snippet compile check

All 10 C# blocks were extracted into a throwaway Web SDK project in the git-excluded `.scratch/` that references the
Cache, HTTP, and Redis.Testing projects plus Scrutor, NUnit, and FluentAssertions. Statement blocks were wrapped in a
method with `builder`, `services`, and `app` parameters; DreamTravel types (`City`, `IGoogleHTTPClient`,
`GoogleHTTPClient`, `GoogleHTTPOptions`) were stubbed. `dotnet build -p:TreatWarningsAsErrors=true` succeeded after one
fix: the recipe's header copy raised `CS8619`, so it uses a `string[]`-typed lambda. No block is marked illustrative.

