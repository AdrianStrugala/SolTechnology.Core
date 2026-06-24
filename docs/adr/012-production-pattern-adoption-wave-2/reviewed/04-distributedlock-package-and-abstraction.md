---
adr: 012-production-pattern-adoption-wave-2
step: 04 of 24
status: reviewed
---

# Step 04: A2.1 — `Core.DistributedLock` scaffold + abstraction + file backend (new package)

## Summary
Create the new `SolTechnology.Core.DistributedLock` package with its public abstraction
(`IDistributedLockService`), options, DI entry point (`ModuleInstaller`), and the **file-based**
backend for single-box local dev. This is the minimal, dependency-light first slice: the package
exists, is registrable, and is usable without any external infrastructure. The production
(Medallion) backends land in step 05. New-package decision sub-section: see
[ADR-012](../../012-production-pattern-adoption-wave-2.md).

> **New top-level `src/` folder confirmation: GIVEN.** The maintainer approved
> `src/SolTechnology.Core.DistributedLock/` (and its `tests/` counterpart below) per CLAUDE.md §1.

## Affected components
- `src/SolTechnology.Core.DistributedLock/SolTechnology.Core.DistributedLock.csproj` — new project
  (inherits `src/Directory.Build.props` → `TreatWarningsAsErrors=true`; package metadata mirroring
  the other `src/` `.csproj` files: `Version` `0.1.0`, `Description`, `PackageTags`, icon, readme).
- `SolTechnology.Core.slnx` — add the new `<Project>` entry under the `/src/` folder.
- `src/SolTechnology.Core.DistributedLock/IDistributedLockService.cs` — abstraction.
- `src/SolTechnology.Core.DistributedLock/DistributedLockOptions.cs` — options (backend selection,
  key-namespace prefix, default timeout).
- `src/SolTechnology.Core.DistributedLock/FileSystem/FileDistributedLockService.cs` — local-dev
  backend (`DistributedLock.FileSystem` lands in step 05; this step may use a simple file-based
  lock or stub the file backend and complete it in step 05 — keep the abstraction the contract).
- `src/SolTechnology.Core.DistributedLock/ModuleInstaller.cs` — `AddDistributedLock(...)` DI entry.
- `docs/DistributedLock.md` — new module doc (overview, registration, guard-rails).
- `tests/SolTechnology.Core.DistributedLock.Tests/` — **new** NUnit test project (the package is new,
  so no test project exists). Wire it into `SolTechnology.Core.slnx` under `/Tests/`. (CLAUDE.md §1
  new-top-level-test-folder confirmation **GIVEN** for this wave.)

## Details
- **Abstraction (the contract everything else honours):**
  `ValueTask<IAsyncDisposable?> TryAcquireLockAsync(string name, TimeSpan timeout,
  CancellationToken ct)`. A non-null `IAsyncDisposable` means the lock is held; disposing releases.
  `null` means "not acquired".
- **Guard-rail (acceptance-critical):** acquisition failure returns `null` and logs at a single
  level — it **never throws into the host loop**. Caller-cancellation (the passed `ct`) may surface
  as cancellation, but a backend/timeout failure must degrade to `null`.
- **Key namespacing:** `DistributedLockOptions` carries a namespace prefix; the service composes the
  final key as `{prefix}/{name}` (and the caller is expected to include tenant/principal where
  relevant). Document that lock keys MUST be tenant/principal-namespaced where relevant.
- **DI:** `AddDistributedLock(Action<DistributedLockOptions>?)` binds options with
  `ValidateOnStart()` (ADR-010 G3) and registers the backend chosen by options. In this step the
  only backend is file-based (local dev). `ModuleInstaller` is the single entry point per coding
  guide.
- **Hosting-free:** keep this package free of any hosting concern — the leader-elected poller
  (step 10) is a separate consumer in `Core.Scheduler`, and `Core.Scheduler` references this package
  (not the reverse).
- Keep this package free of `Medallion.Threading` / `DistributedLock.*` references — those arrive in
  step 05. The file backend here must not require them (use a `FileStream`-based mutual-exclusion or
  a minimal local lock; if a clean implementation needs `DistributedLock.FileSystem`, move the file
  backend wholesale to step 05 and ship step 04 as abstraction + options + DI + Medallion-free
  in-memory dev stub).
- `.slnx` wiring (`/src/` and `/Tests/`) + new `.csproj` are part of "the package exists" — they ship
  in this step. The **publish-workflow** packing is deliberately deferred to step 23 (pipeline
  concern).

## Acceptance criteria
- `SolTechnology.Core.DistributedLock` builds under `TreatWarningsAsErrors=true` and is referenced
  by `SolTechnology.Core.slnx` (`/src/`), with `tests/SolTechnology.Core.DistributedLock.Tests` in
  `/Tests/`.
- `IDistributedLockService.TryAcquireLockAsync` returns a disposable handle on success and `null` on
  failure, never throwing for a non-cancellation failure.
- `AddDistributedLock` binds + validates options on start and registers a working local-dev backend.
- Lock keys are namespaced via the options prefix.
- `docs/DistributedLock.md` exists with registration + guard-rail sections.

## Open questions
- Whether the file backend uses `DistributedLock.FileSystem` (then it moves to step 05 with the
  other Medallion packages) or a hand-rolled local lock here. Recommend: hand-rolled minimal local
  lock in step 04 so this slice stays dependency-free; `DistributedLock.FileSystem` optional in 05.

