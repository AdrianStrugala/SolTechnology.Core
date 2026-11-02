---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 60
status: to-do
---

# Step 60: Module docs — Core, CQRS, Tale, AUID, Cache, Log, Redis.Testing

## Summary
Several docs no longer match the code. The Cache/Log/Redis.Testing docs are merged into this step
during plan review (RQ11):

- `docs/CQRS.md` still uses `AddCQRS` (`:9,166`). It claims each event handler runs on its own
  background task, but the code runs them in sequence in one scope (`:92`).
- `docs/Tale.md` documents direct handler persistence (now false after step 18, `:369-385`) and
  the dead option `RestrictControllerToRegisteredHandlers` (`:75`). Its example injects the
  handler with `CancellationToken.None`. The log category it documents is wrong (`:519-523`).
- `SolTechnology.Core` packs the root `README.md`, whose relative images do not render on
  nuget.org and which describes the repo, not the Result pattern (`repo#A9`).
- `docs/Cache.md` uses `AddLocalCache`, `AddDistributedCache`, `AddLocalLock`, and similar
  methods, which do not exist (`logging-cache#15`, `:9-10,31,44,207-212,240-244,334-341`).
- `docs/Log.md` must reflect the fixed configuration overload (step 14), segment-aware path
  matching, and correlation-ID sanitization.

## Affected components
- `docs/CQRS.md` — EDIT — `AddSolCQRS`; event dispatch semantics; `:164,170,171` URLs are left for release step 02 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md))
- `docs/Tale.md` — EDIT — persistence scope (P-02); `ExposeOverHttp<T>()` (step 43); chapter `CancellationToken` (step 39); `ITaleRepository` 2.0 contract, optimistic concurrency, and migration (steps 40–42); custom repositories serialize `TaleInstance` parts with the public `TaleJsonOptions.Default` (step 17, `C-26`); route the example through `IMediator`; log category
- `docs/AUID.md` — EDIT — `using SolTechnology.Core.AUID;` (step 44); `TimeProvider` overloads
- `docs/Core.md` — NEW — `Result`, `Result<T>`, `Error` hierarchy, JSON polymorphism (P-01)
- `src/SolTechnology.Core/SolTechnology.Core.csproj` — EDIT — `:14` delete the `..\..\README.md` `None` item and the `mini-logo.png` item; add `<PackageReadmeSource>Core.md</PackageReadmeSource>` (the shared `src/Directory.Build.props` `ItemGroup` then packs both)
- `docs/Cache.md` — EDIT — `AddSolLocalCache`, `AddSolDistributedCache`, `AddSolLocalLock`, `AddSolDistributedLock`, `AddSolLocalIdempotency`, `AddSolDistributedIdempotency`; `InstanceName` must be non-empty (validated at start, P-07); `:272` lock expiry semantics; `C-13` names
- `docs/Log.md` — EDIT — `:44,81` the overload works; P-14; P-15; `LoggerExtensions` (`C-20`); CQRS emits on the same `OperationsName` source
- `docs/Redis.Testing.md` — EDIT — `Flush` name (`C-23`, confirmed 2026-11-02)
- `SolTechnology.Core.slnx` — EDIT — add `docs/Core.md`, `docs/CQRS.md`, `docs/Tale.md` to `/Docs/` if missing

## Changes
- Snippet verification as in step 59.
- Premortem mitigations (step 00, 2026-11-02), documentation part:
  - RM-2 (scenario 2): `docs/Cache.md` states that `InstanceName` prefixes cache, lock (`lock:`), and idempotency (`idempotency:`) keys, and that `InstanceName = "SolTechnology:"` keeps the 1.x lock and idempotency keyspace during an upgrade.
  - RM-5 (scenario 5): the `docs/Tale.md` migration section requires a single-version cutover of Tale hosts and rules out a rollback to 1.x after 2.x has written tale state.
  - RM-15 (scenario 15): `docs/Log.md` states that a failed `Result` is logged at Warning with the error type and `Error.Message`, so `Error.Message` must not contain PII.
  - RM-17 (scenario 17): `docs/Tale.md` states that a direct-handler run that pauses is not persisted and cannot be resumed; interactive tales start through `TaleManager`.
  - RM-20 (scenario 20): `docs/Core.md` states that the `Result`/`Error` JSON contract is System.Text.Json only, and that `Result` must not travel inside Newtonsoft-serialized payloads (MessageBus messages, Hangfire job arguments).

## Acceptance criteria
- [ ] `grep -nE "\bAddCQRS\b|RestrictControllerToRegisteredHandlers|CancellationToken\.None" docs/CQRS.md docs/Tale.md` prints nothing.
- [ ] `grep -nE "\bAdd(Local|Distributed)(Cache|Lock|Idempotency)\b" docs/Cache.md` prints nothing.
- [ ] `grep -n "TaleJsonOptions.Default" docs/Tale.md` matches.
- [ ] `dotnet pack src/SolTechnology.Core/SolTechnology.Core.csproj` contains `docs/readme.md` equal to `docs/Core.md`.
- [ ] Snippets compile in the throwaway project.
- [ ] RM-2, RM-5, RM-15, RM-17, RM-20: `grep -n "SolTechnology:" docs/Cache.md`, `grep -n "roll back" docs/Tale.md`, `grep -n "Error.Message" docs/Log.md`, `grep -n "TaleManager" docs/Tale.md`, and `grep -n "Newtonsoft" docs/Core.md` each match the new text.

## Open questions
- none

## Deviations
