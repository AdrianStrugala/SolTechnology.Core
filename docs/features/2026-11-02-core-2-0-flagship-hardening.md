---
status: in-progress
created: 2026-11-02
completed:
---

# SolTechnology.Core 2.0 — flagship hardening before the Sol Technology org transfer

> Historical delivery record. It may not describe the current system.

## Goal

Ship `SolTechnology.Core` 2.0.0 as the Sol Technology organization's flagship open-source
project, presented on the product site next to AvroConvert. Success means:

- `master` restores, builds, and tests green from a clean clone, and CI fails on any test failure.
- No known secret is reachable from the working tree, and every leaked credential is revoked. The
  documented local-only DreamTravel dev credentials (localhost/Docker only) are excluded and stay
  committed (decision B, user, 2026-11-02; AR-6).
- Every module documented as working actually works when resolved from DI.
- Every module doc packed as a NuGet README compiles against the shipped `AddSol*` API.
- The 2.0 public API is deliberate: implementation types are internal, async contracts accept a
  `CancellationToken`, and names follow `ClaudeCodingGuide §10`.
- `master` carries version 2.0.0 and is ready to release; the org transfer and the 2.0.0 publish
  belong to the split-off features listed under "Implementation plan".

## Context

### Evidence

A full-repository code review ran on 2026-11-02 against `master@4097d47f`: 22 packages, 13 test
projects, DreamTravel, docs, and CI. The review artifacts live outside the repository, so this
brief and its steps carry every finding they act on, with `file:line`. Finding IDs used below:

| Prefix | Meaning |
|---|---|
| `S1`–`S6` | P0 security |
| `B1`–`B11` | P0 modules that do not work as documented |
| `C1`–`C3` | P0 CI/release |
| `P1-n` | P1 systemic items (docs drift, DI tests, test gaps, XML docs, Polish text — non-joke only since 2026-11-03, Guide consistency, correctness, Guide §9/§11, hygiene) |
| `<area>#n` | Detailed per-area finding: `cqrs-tale#n`, `http-api#n`, `logging-cache#n`, `infra#n`, `repo#An` / `repo#Bn` (DreamTravel) |

Baseline verified by the review (.NET SDK 10.0.100, Docker available):

- `dotnet build SolTechnology.Core.slnx -c Release` fails at restore with `NU1903`. The cause is
  SSH.NET 2025.1.0, which `Testcontainers@4.12.0` pulls into 10 projects. Planning confirmed from
  the nuget.org nuspecs that `Testcontainers@4.14.0`+ depends on `SSH.NET@2026.0.0`, and that
  `4.16.0` exists for `Testcontainers`, `.PostgreSql`, `.Redis`, `.Azurite`, and `.ServiceBus`.
- Plan review (2026-11-02) found a second `NU1903` with no parent fix: `Microsoft.Data.SqlClient@7.0.1`
  pulls `System.Security.Cryptography.Pkcs@9.0.13` (GHSA-846f-f764-76mx, CVE-2026-84008, High,
  published 2026-10-13; first patched `10.0.13`). Even SqlClient 7.1.1 still declares a vulnerable
  Pkcs.
- `dotnet test` on an existing build: 498 passed, 0 failed, 11 skipped.
- 22 files exceed the Guide §9 hard cap of 150 lines.

### Decisions already made by the user (2026-11-02)

- **D1 — Secrets.** The owner revokes the leaked Google API keys and the Service Bus
  `RootManageSharedAccessKey` (a manual action in GCP/Azure). The legacy zips are removed with
  `git rm`. **Git history is not rewritten**: no `git filter-repo`, no force-push. Org secret
  scanning and push protection are enabled after the transfer (release step 03).
- **D2 — Versioning.** This is a 2.0 major release. The breaking public-API cleanup ships together
  with the fixes. Per `CLAUDE.md §2`, every concrete public/protected rename, move, or delete
  still needs explicit confirmation. They are enumerated below as `C-nn` so they can be confirmed
  in one pass.
- **D3 — Ordering.** Planning comes first. The org transfer (GitHub transfer, nuget.org org
  co-owner, secrets, branch protection, URL updates) is a late phase; since the split it is the
  separate feature [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

### Related feature records

- [`2026-07-06-jwt-bearer-authentication.md`](2026-07-06-jwt-bearer-authentication.md) is still
  `planning`. Its step `02-api-key-hardening` duplicates `S5` and `http-api#4`, and its step
  `04-documentation` duplicates the `docs/Auth.md` drift rewrite (`http-api#11`). **Q5 decision
  (user, 2026-11-02):** this feature owns the constant-time API-key fix (former step 07, now
  api-auth 04) and the `docs/Auth.md` rewrite (former step 59, now api-auth 08). The JWT feature stays JWT-only; its own planner supersedes its
  steps 02 and 04. This feature does not edit that feature's files.
- [`2026-06-29-release-1-0.md`](2026-06-29-release-1-0.md) is the precedent for release mechanics
  (`docs/release-runbook-1.0.md`).

### Design alternatives for hard-to-reverse choices

Each choice was weighed blue-team versus red-team per the `blue-red-team` skill. The
recommendation is the planned default. The plan-reviewer and premortem may overturn it.

| # | Choice | Alternatives | Recommendation and crux |
|---|---|---|---|
| A1 | `Error` polymorphism (`B7`, `src/SolTechnology.Core/Error.cs:13-22`) | (a) add Tale errors to the closed `[JsonDerivedType]` list (impossible: `SolTechnology.Core` cannot reference Tale); (b) `[JsonPolymorphic(UnknownDerivedTypeHandling = FallBackToNearestAncestor)]` only; (c) (b) plus a Tale-owned `TypeInfoResolver` modifier in `TaleJsonOptions.Default` that registers `TaleCancelledError`/`TalePausedError` discriminators | **(c).** Blue: no throw for consumer subtypes, Tale errors round-trip in persistence. Red: unknown consumer subtypes lose their runtime type on deserialize. Crux: does any consumer rely on round-tripping its own subtype? No evidence in the workspace, so fallback is acceptable and documented. |
| A2 | Scope of automatic Tale persistence (`B8`, `Tale/ModuleInstaller.cs:108`) | (a) persist only runs started through `TaleManager`; (b) keep persisting all runs but add TTL/eviction to `InMemoryTaleRepository` | **(a).** Blue: removes the leak and PII retention at the source and matches the docs' "direct handler usage (no persistence)". Red: changes what lands in user repositories. Crux: no workspace consumer reads non-`TaleManager` runs from the repository (verify in premortem). |
| A3 | `ITaleRepository` concurrency (`cqrs-tale#8`) | (a) `TaleInstance.Version` + `Save` returning `Task<Result>` with `ConflictError` on mismatch; (b) `Save` throws `TaleConcurrencyException`; (c) no concurrency, documented | **(a).** Uses the existing `ConflictError` and the Result-everywhere rule (Guide §13). Red: every custom repository must implement a conditional write. Accepted for 2.0. |
| A4 | `IUnitOfWork.Begin` (`B3`) | (a) add `IUnitOfWork Begin(IsolationLevel)` to the interface and make it extend `IDisposable`; (b) `Begin` returns an `IAsyncDisposable` transaction scope; (c) open the scope in the constructor (scoped lifetime) | **(a).** Smallest change that makes the DI-resolved contract usable. (b) is cleaner but forces every caller to restructure `using` blocks. Confirmation `C-15`. |
| A5 | `IMessageBusBroker` resolution (`B1`) | (a) make the constructor public; (b) register with a factory lambda and keep the constructor internal | **(b) in former step 12 (messagebus 02)**, then make the types internal in former step 48 (messagebus 04, `C-14`). Adds no public surface that 2.0 would remove again. |
| A6 | Hangfire Smart Retry for plugin jobs (`infra#10`) | (a) `IJob.Execute` returns `Task<Result>` so `SmartRetryJobFilter` sees a `Result`; (b) keep `Task` and remove the claim from docs | **(a)** if `C-19` is confirmed, otherwise (b). Aligns jobs with Guide §13. |
| A7 | `preventOverlap` (`B5`) | (a) implement through `PreventOverlapJobFilter` (already in `Hangfire/Filters/`); (b) delete the parameter | **(a).** Non-breaking, and the docs already promise it. |
| A8 | Weekly NuGet stats bot (`repo#A5`) | (a) commit to a `stats` branch; (b) drop the bot and use shields.io NuGet badges; (c) keep committing to `master` with a ruleset bypass | **(c)**, decided by the user (Q7, 2026-11-02). `CLAUDE.md §2` keeps reading `nuget-stats.json` from `master`. Since decision A (user, 2026-11-02) `master` has no ruleset, so the bot pushes directly without a bypass actor (release feature). |
| A9 | Leaked secrets in history (`S2`) | (a) revoke and leave history as is; (b) `git filter-repo` plus force-push | **(a)**, decided by the user (D1). |
| A10 | `IncludeResponseBodyInException` per client (`http-api#6`) | (a) flow the named policy through a `DelegatingHandler`; (b) document it as per-request only | **(b)** in former step 20 (http 03) (simplicity). (a) is a follow-up. |

## Scope

### In scope

- **Phase A — Build and CI** (`C1`–`C3`): fix `NU1903` at source (plus the user-confirmed Pkcs
  override), make the runner exit code honest, publish only from tags, bump the version to 2.0.0.
- **Phase B — Security** (`S1`, `S3`–`S6`): unsafe-verb retry, upstream URI disclosure,
  constant-time API-key compare, DreamTravel Development-only exception page, leaked
  zip and personal settings removal. The DreamTravel credential scrub (`repo#B6`) was dropped on
  2026-11-02 (decision B, AR-6).
- **Phase C — DI safety net** (`P1-2`): one `ValidateOnBuild` + `ValidateScopes` resolution test
  per DI-registering module, written before the fixes.
- **Phase D — Broken modules** (`B1`–`B11`), grouped by module.
- **Phase E — P1 correctness** (`P1-7` and the per-area Major/Minor findings), grouped by module.
- **Phase F — 2.0 breaking cleanup** (D2), one step per module and gated on the `C-nn` confirmations.
- **Phase G — Documentation** (`P1-1`, `P1-4`): XML docs shipped through
  `GenerateDocumentationFile`, and every packed module doc compiling against the 2.0 API. Guide
  self-consistency (`P1-6`) moved to showcase step 04 at the split.
- **Phase H — Guide §9/§11 conformance**: size caps, primary constructors, `extension` blocks,
  braces and comments, and `[{Placeholder}]` log templates.
- **Close** — retrospective (former step 71; now core 11 plus one retrospective per library feature).

Since the per-library regroup (user, 2026-11-02) phases A, B (DreamTravel and secrets), the
zero-dependency package, the central XML-docs switch, and the close stay here; every library-scoped
item of phases B–H is delivered by the library features listed under "Per-library regroup".

Split off on 2026-11-02 (RQ8): the former phase I (flagship hygiene) and Guide self-consistency
form [`2026-11-02-flagship-showcase-hygiene`](2026-11-02-flagship-showcase-hygiene.md); the former phases J–K (org transfer, release 2.0.0) form [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

### Out of scope

- Rewriting git history (D1).
- Trimming/AOT annotations (`[RequiresUnreferencedCode]`) on `AddSolCQRS`/`AddSolTale`. This is
  additive and becomes a follow-up.
- Splitting the CQRS → Logging → ASP.NET Core shared-framework dependency (`cqrs-tale#18`, second
  half).
- A general outdated-package sweep (OpenTelemetry 1.15→1.20, StackExchange.Redis 3.0→3.4,
  WireMock 2.13→2.21). Only CVE-driven bumps are in scope.
- Moving `wroclaw_drive.graphml` (10.2 MB, used by `DreamTravel.Aspire/Program.cs`) and
  `research_documentation.docx` to Git LFS or release assets.
- Renames listed under "Not planned" below.
- New test projects: neither `tests/SolTechnology.Core.Blob.Tests` nor a test project for the
  `*.Testing` packages (Q6, user, 2026-11-02; accepted risk AR-1).
- Deleting `SolTechnology.Core.Guards`, `SolTechnology.Core.Scheduler`,
  `tests/SolTechnology.Core.Guards.Tests`, `docs/Guards.md`, or `docs/Cron.md` (Q1, user,
  2026-11-02; `C-25` rejected), and fixing their known defects (accepted risk AR-2).
- Moving the NuGet stats bot off `master` and changing `CLAUDE.md §2` (Q7, user, 2026-11-02).
- Scrubbing the local-only DreamTravel dev credentials (decision B, user, 2026-11-02; AR-6).
- Deleting the DreamTravel Azure DevOps pipelines, `devOps/scripts/`, or
  `devOps/infrastructure/legacy_AzureResourceGroup/` (Q9, user, 2026-11-02: kept as a memento).
- A DreamTravel `UseNuGet` MSBuild switch (Q4, user, 2026-11-02).
- AUID per-process monotonic counter (`logging-cache#13`). 2.0 documents the collision bound only.
- Community health files, root README quick start, workflow SHA pinning and Dependabot, DreamTravel
  showcase cleanup and CI job, and Guide self-consistency (moved to [`2026-11-02-flagship-showcase-hygiene`](2026-11-02-flagship-showcase-hygiene.md)).
- GitHub and nuget.org org transfer, license and authorship, repository URLs, repository security
  settings (no `master` ruleset, decision A, user, 2026-11-02),
  Trusted Publishing, and publishing 2.0.0 (moved to [`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md)).

## Implementation plan

The steps are the execution units. Gates: review and premortem are both required (see
`summary.md`). The premortem (step `00`) covers former steps 01–70, because they edit
`ModuleInstaller.cs` files, `src/Directory.Build.props`, and persisted contracts
(`CLAUDE.md §4`). Since the per-library regroup it also covers the library features' steps.

### Per-library regroup (user, 2026-11-02)

**Scope change 2026-11-02 (user: "podziel implementację feature 1 na każdą bibliotekę osobno"):**
the implementation steps 05–70 were regrouped into one feature per library, each with its own
dated brief, working folder, and retrospective, delivered end-to-end: correctness and broken-module
fixes → 2.0 breaking API cleanup → XML docs and module docs → Coding Guide conformance and
`[{Placeholder}]` log templates for that library. This feature keeps step 00 (premortem, `done`),
01 (`done`), the foundation steps 02–04, the cross-cutting steps 05–08, and the umbrella
retrospective 09. No step content was dropped; multi-library steps (former 10, 11, 17, 52, 55–61,
65–71) were split into per-library pieces with their content preserved. This brief keeps the
shared decision tables (`C-nn`, `P-nn`, `L-nn`, `A`, `AR`, `Q`, `RQ`); each library brief copies
only the rows it owns.

Gates of the library features: the core premortem verdict `go-with-mitigations (2026-11-02)` covers
the regrouped steps (user decision, 2026-11-02), so every library records
`premortem: skipped (… — user, 2026-11-02)`, except testing kits, which has no `CLAUDE.md §4`
trigger and records `waived (… — planner, 2026-11-02)`. Each `RM-n` mitigation stays verbatim in
the step that carries it. `review` is `pending` in every library summary: the planner may not waive
a required gate, and no user skip reason was recorded.

Notation: an unqualified step number is a step of this feature; `core NN` is also this feature;
`result NN`, `logging NN`, `cqrs NN`, `auid NN`, `tale NN`, `http NN`, `api-auth NN`, `cache NN`,
`sql NN`, `blob NN`, `messagebus NN`, `hangfire NN`, and `testing-kits NN` name step `NN` of the
library features below; `showcase NN` and `release NN` name the split-off features. "Former step
NN" and numbers in the historical tables of this brief (`Q`, `RQ`, `AR`, "Plan-review revisions",
"Split", the phase table) are pre-regroup numbers; translate them with the table below.

Delivery order (dependency-driven). Every library feature starts after core 02–04 are `done`.
AUID moved ahead of Tale: `SolTechnology.Core.Tale` references `SolTechnology.Core.AUID`, and
tale 14 guards the Tale JSON test that auid 03 adds.

| Order | Short | Feature brief | Packages | Steps | Former steps | Builds on |
|---|---|---|---|---|---|---|
| 1 | `result` | [`2026-11-02-core-2-0-core-result`](2026-11-02-core-2-0-core-result.md) | `SolTechnology.Core` | 6 | 17, 38, 55, 60, 66, 70, 71 | core 02–04 |
| 2 | `logging` | [`2026-11-02-core-2-0-logging`](2026-11-02-core-2-0-logging.md) | `SolTechnology.Core.Logging` | 8 | 10, 14, 30, 31, 52, 60, 66, 67, 70, 71 | core 02–04 |
| 3 | `cqrs` | [`2026-11-02-core-2-0-cqrs`](2026-11-02-core-2-0-cqrs.md) | `SolTechnology.Core.CQRS` | 7 | 10, 23, 24, 55, 60, 66, 70, 71 | core 02–04; result, logging |
| 4 | `auid` | [`2026-11-02-core-2-0-auid`](2026-11-02-core-2-0-auid.md) | `SolTechnology.Core.AUID` | 7 | 19, 35, 44, 55, 60, 67, 70, 71 | core 02–04 |
| 5 | `tale` | [`2026-11-02-core-2-0-tale`](2026-11-02-core-2-0-tale.md) | `SolTechnology.Core.Tale` | 17 | 10, 17, 18, 25, 26, 27, 39, 40, 41, 42, 43, 55, 60, 62, 63, 70, 71 | core 02–04; result, auid |
| 6 | `http` | [`2026-11-02-core-2-0-http`](2026-11-02-core-2-0-http.md) | `SolTechnology.Core.HTTP` | 11 | 05, 10, 20, 21, 45, 46, 56, 59, 64, 70, 71 | core 02–04; result, logging |
| 7 | `api-auth` | [`2026-11-02-core-2-0-api-auth`](2026-11-02-core-2-0-api-auth.md) | `SolTechnology.Core.API`, `SolTechnology.Core.Auth` (renamed from `SolTechnology.Core.Authentication` by api-auth 01, `C-28`) | 12 | 06, 07, 10, 22, 52, 56, 59, 65, 66, 70, 71 (00 and 01 new on 2026-11-03) | core 02–04; result, logging, http |
| 8 | `cache` | [`2026-11-02-core-2-0-cache`](2026-11-02-core-2-0-cache.md) | `SolTechnology.Core.Cache` | 9 | 11, 28, 29, 47, 57, 60, 67, 70, 71 | core 02–04 |
| 9 | `sql` | [`2026-11-02-core-2-0-sql`](2026-11-02-core-2-0-sql.md) | `SolTechnology.Core.SQL` | 8 | 11, 15, 33, 49, 57, 61, 69, 70, 71 | core 02–04; result |
| 10 | `blob` | [`2026-11-02-core-2-0-blob`](2026-11-02-core-2-0-blob.md) | `SolTechnology.Core.Blob` | 6 | 16, 50, 57, 61, 69, 70, 71 | core 02–04 |
| 11 | `messagebus` | [`2026-11-02-core-2-0-messagebus`](2026-11-02-core-2-0-messagebus.md) | `SolTechnology.Core.MessageBus` | 9 | 11, 12, 34, 48, 57, 61, 68, 70, 71 | core 02–04; logging |
| 12 | `hangfire` | [`2026-11-02-core-2-0-hangfire`](2026-11-02-core-2-0-hangfire.md) | `SolTechnology.Core.Hangfire` | 8 | 11, 13, 32, 51, 57, 61, 69, 70, 71 | core 02–04; result, logging, cqrs |
| 13 | `testing-kits` | [`2026-11-02-core-2-0-testing-kits`](2026-11-02-core-2-0-testing-kits.md) | `.Testing`, `.HTTP.Testing`, `.API.Testing`, `.SQL.Testing`, `.Redis.Testing`, `.Blob.Testing`, `.ServiceBus.Testing` | 7 | 36, 37, 53, 56, 58, 59, 60, 61, 65, 68, 69, 70, 71 | core 02–04; http, api-auth, sql, cache, blob, messagebus |

Remaining foundation steps: core 05 and 06 may run at any time; core 07 (zero-dependency base
package) runs after core 04; core 08 (central `GenerateDocumentationFile`) runs after every library
XML-docs step; core 11 closes this feature last, after every library feature is `completed`.

Former → current step mapping (`*` = only the library-scoped part of the former repository-wide
`[{Placeholder}]` and empty-`catch` check, carried in that library's conformance step):

| Former | Title | Now |
|---|---|---|
| 00 | Run premortem (gate) | core 00 |
| 01 | Clear NU1903 — Testcontainers and DreamTravel parent bumps, Pkcs override in SQL | core 01 |
| 02 | Fail CI on any test-project failure | core 02 |
| 03 | Publish to nuget.org only from `v*` tags behind a protected environment | core 03 |
| 04 | Bump every package to 2.0.0 | core 04 |
| 05 | HTTP — never retry unsafe verbs after an exception or timeout | http 02 |
| 06 | Keep upstream URIs and query strings out of `ProblemDetails` and exception messages | api-auth 03 |
| 07 | Authentication — constant-time API-key comparison and per-instance logger | api-auth 04 |
| 08 | DreamTravel — Development-only developer exception page | core 05 |
| 09 | Remove leaked-secret artifacts and personal settings; record revocation | core 06 |
| 10 | DI resolution tests — application modules | logging 01, cqrs 01, tale 01, http 01, api-auth 02 |
| 11 | DI resolution tests — infrastructure modules | cache 01, sql 01, messagebus 01, hangfire 01 |
| 12 | MessageBus — resolvable broker, registered health-check client, correlation service | messagebus 02 |
| 13 | Hangfire — implement `preventOverlap`; register the correlation service | hangfire 02 |
| 14 | Logging — `AddSolLogging(IConfiguration)` registers its middleware dependencies | logging 02 |
| 15 | SQL — `Begin` on `IUnitOfWork` | sql 02 |
| 16 | Blob — thread-safe container cache with async creation | blob 01 |
| 17 | Core — open `Error` polymorphism; `AggregateError` round-trip | result 01, tale 02 |
| 18 | Tale — per-run state isolation; persist only `TaleManager`-driven runs | tale 03 |
| 19 | AUID — culture-invariant formatting, range-checked parsing | auid 01 |
| 20 | HTTP — request pipeline correctness | http 03 |
| 21 | HTTP — upstream health-check disposal and options validation | http 04 |
| 22 | Api — keep 201/202 in `ResultConversionFilter`; real filter registration | api-auth 05 |
| 23 | CQRS — descriptive missing-handler errors and interface-based dispatch | cqrs 02 |
| 24 | CQRS — log failed `Result`s as failures | cqrs 03 |
| 25 | Tale engine — caller-only cancellation, no resume of failed tales, deterministic tests | tale 04 |
| 26 | Tale — registration and schema safety, correct log scope | tale 05 |
| 27 | Tale controller — consistent JSON options, no internal detail in responses | tale 06 |
| 28 | Cache — local cache fault eviction, single flight, and a correct local lock | cache 02 |
| 29 | Cache — type-namespaced keys, validated non-empty `InstanceName`, validated options | cache 03 |
| 30 | Logging — segment-aware path matching; one Error entry per failure | logging 03 |
| 31 | Logging — sanitize inbound correlation IDs; safe timings and masking | logging 04 |
| 32 | Hangfire — restore ambient correlation state; async, cancellable event dispatch | hangfire 03 |
| 33 | SQL — transient-only connection retry, safer error translation, options validation | sql 03 |
| 34 | MessageBus — receiver settlement matches its contract; publish→receive round-trip | messagebus 03 |
| 35 | AUID — standards-compliant converters; honest uniqueness and ordering docs | auid 02 |
| 36 | API and HTTP testing kits — dispose the test host; strict WireMock by default | testing-kits 01 |
| 37 | Container fixtures — per-instance state; honour Ryuk settings | testing-kits 02 |
| 38 | Core — immutable `Result` and a typed `Result<T>.FailAsTask` | result 02 |
| 39 | Tale — `CancellationToken` on chapters | tale 07 |
| 40 | Tale — `ITaleRepository` 2.0 signatures (cancellation and names) | tale 08 |
| 41 | Tale — optimistic concurrency on `ITaleRepository.Save` | tale 09 |
| 42 | Tale — chapter history keyed by step index and full type name | tale 10 |
| 43 | Tale — explicit HTTP exposure; internal plumbing types | tale 11 |
| 44 | AUID — `SolTechnology.Core.AUID` namespace; no process-wide `TimeProvider` | auid 03 |
| 45 | HTTP — own namespace for `HttpClientExtensions`; factory-based upstream health check | http 05 |
| 46 | HTTP — drop `Async` from `RequestBuilder` verbs (optional) | http 06 |
| 47 | Cache — drop `Async` from lock and idempotency contracts | cache 04 |
| 48 | MessageBus — internal implementation types | messagebus 04 |
| 49 | SQL — internal implementations; safe-by-default DACPAC deployment | sql 04 |
| 50 | Blob — 2.0 surface (own namespace, cancellation, atomic metadata, internal factory) | blob 02 |
| 51 | Hangfire — recurring job contract for 2.0 | hangfire 04 |
| 52 | Logging, Authentication, and Api — public surface cleanup | logging 05, api-auth 06 |
| 53 | Testing kits — 2.0 surface (optional `Async` renames, WireMock fields) | testing-kits 03 |
| 54 | Packaging — a truly zero-dependency `SolTechnology.Core` | core 07 |
| 55 | XML docs shipped — Core, CQRS, Tale, AUID | result 03, cqrs 04, auid 04, tale 12 |
| 56 | XML docs shipped — HTTP, HTTP.Testing, Api, API.Testing, Authentication | http 07, api-auth 07, testing-kits 04 |
| 57 | XML docs shipped — Cache, MessageBus, SQL, Blob, Hangfire | cache 05, sql 05, blob 03, messagebus 05, hangfire 05 |
| 58 | XML docs shipped — testing kits, then one central switch | core 08, testing-kits 04 |
| 59 | Module docs — Api, Clients, HTTP checklist, API/HTTP testing, Auth | http 08, api-auth 08, testing-kits 05 |
| 60 | Module docs — Core, CQRS, Tale, AUID, Cache, Log, Redis.Testing | result 04, logging 06, cqrs 05, auid 05, tale 13, cache 06, testing-kits 05 |
| 61 | Module docs — Bus, Hangfire, SQL, Blob, Testing kits | sql 06, blob 04, messagebus 06, hangfire 06, testing-kits 05 |
| 62 | Guide conformance — Tale engine split | tale 14 |
| 63 | Guide conformance — Tale controller, manager, handler, `Tale`, and tests | tale 15 |
| 64 | Guide conformance — HTTP | http 09 |
| 65 | Guide conformance — Api, Authentication file names, HTTP.Testing | api-auth 09, testing-kits 06 |
| 66 | Guide conformance — CQRS, Core, Authentication | result 05, logging 07, cqrs 06, api-auth 09 |
| 67 | Guide conformance — Logging, Cache, AUID | logging 07, auid 06, cache 07 |
| 68 | Guide conformance — MessageBus and ServiceBus.Testing | messagebus 07, testing-kits 06 |
| 69 | Guide conformance — SQL, Blob, Hangfire, and the remaining testing kits | sql 07, blob 05, hangfire 07, testing-kits 06 |
| 70 | Log templates — `[{Placeholder}]` everywhere (log contract change L-03) | tale 16, http 10, api-auth 10, cache 08, messagebus 08, result 05*, logging 07*, cqrs 06*, auid 06*, sql 07*, blob 05*, hangfire 07*, testing-kits 06* |
| 71 | Retrospective | core 11, result 06, logging 08, cqrs 07, auid 07, tale 17, http 11, api-auth 11, cache 09, sql 08, blob 06, messagebus 09, hangfire 08, testing-kits 07 |

**Scope change 2026-11-02:** credential scrub dropped (user decision B: "czy DreamTravel nie miało
haseł tylko lokalnych w repo? Jeżeli tak, to zostaw."); premortem mitigations referring to it are
void. Former step 08 (now core 05) keeps only the `S6` Development-only exception page (and its fix-on-touch CORS
`chore:`, whose rename was reverted on 2026-11-03; see the 2026-11-03 joke scope change below); AR-6 records the accepted risk. Rows that described the `master` ruleset and stats-bot
bypass now point at user decision A (2026-11-02, no `master` ruleset; see the release feature).
`review` stays `done (2026-11-02)` and the step 00 verdict stands: both changes only remove
user-decided scope and add no step, file, contract, or dependency.

**Split-off features (RQ8, user, 2026-11-02):** [`2026-11-02-flagship-showcase-hygiene`](2026-11-02-flagship-showcase-hygiene.md) (depends on
core 02, 03, 04, 05, 07, result 02, tale 03, 07, 11, api-auth 05, and auid 03; former steps 02,
03, 04, 08, 18, 22, 38, 39, 43, 44, 54) and
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) (depends on this feature, every library feature, the showcase feature, and
[`2026-11-02-logging-chronicle`](2026-11-02-logging-chronicle.md) being `completed`). Each has its own review, premortem, and retrospective.

Original phase layout (former step numbers; see "Per-library regroup" for the current location):

| Phase | Steps | Notes |
|---|---|---|
| A — Build, CI, version | 01–04 | `master` must build before anything else merges. 01 carries the user-confirmed Pkcs leaf override (RQ1). 04 bumps to 2.0.0 right after tag-only publishing (RQ3). |
| B — Security | 05–09 | 07 is owned here (Q5). 09 includes the owner's manual revocation record. |
| C — DI safety net | 10–11 | Known-broken registrations are added as `[Ignore("B1 …")]` tests; phase D removes each `Ignore`. |
| D — Broken modules | 12–19 | 15 implements `C-15`. 16 has no automated test (Q6, AR-1). 17 makes `TaleJsonOptions` public (`C-26`). |
| E — P1 correctness | 20–37 | Behavioural fixes with negative tests, no public renames except the additive `C-27` in 34. |
| F — 2.0 breaking cleanup | 38–54 | Every `C-nn` item in these steps was confirmed on 2026-11-02. 50 has no automated test (Q6, AR-1). `master` already carries 2.0.0 (step 04), and publishing is tag-only (step 03), so no 1.x package ever contains a break. |
| G — Documentation | 55–61 | Runs after F so docs describe the final 2.0 API. 59 owns the `docs/Auth.md` rewrite (Q5). |
| H — Guide conformance | 62–70 | Runs after E/F under the new tests. 70 changes log templates (L-03). |
| Close | 71 | Retrospective; records the step 03 and step 09 owner dates and the release-notes inputs in this brief for the release feature. |

Steps removed on 2026-11-02 after the user's answers, with later steps renumbered:

- Former 74 "Move the NuGet stats bot off `master`" — dropped (Q7).
- Former 76 "Delete the dead Guards and Scheduler projects" — dropped (Q1, `C-25` rejected). Its only
  non-deletion item, the Guards replacement text in the deprecated-package table (`dontreadme.md`, kept under its name since 2026-11-03), moved to step 74
  (now showcase 01).
- Former 81 "DreamTravel — optionally consume the published packages" — dropped (Q4). Step 77
  (now showcase 06) already documents the `PackageReference` equivalents in the DreamTravel README.

(The step numbers in the list above are the pre-review numbers.)

### Split (RQ8, user, 2026-11-02)

The post-review steps `00`–`83` were split into three features. Steps 00–70 keep their numbers
here. Post-review → current location:

| Post-review | Current | Note |
|---|---|---|
| 00–70 | 00–70 (this feature) | Step 00 now covers 01–70 only. |
| 71, 72, 73 | showcase 01, 02, 03 | — |
| 74 | showcase 04 | Boundary adjustment: Guide self-consistency must follow showcase 03 (DreamTravel controllers); keeping it here would make the two features depend on each other. |
| 75, 76, 77 | showcase 05, 06, 07 | — |
| 78, 79, 80, 81, 82 | release 01, 02, 03, 04, 05 | — |
| 83 | 71 (this feature) | Retrospective trimmed to this feature; showcase 08 and release 06 are the other retrospectives. |

### Plan-review revisions (2026-11-02)

The plan-reviewer's critique was answered by the user on 2026-11-02 (`RQ1`–`RQ12` below) and
applied to the temporary step files. Restructuring (RQ3, RQ11) renumbered the steps from
`00`–`84` to `00`–`83`. Pre-review → post-review numbers (the split above moved 71–83 again):

| Pre-review | Post-review (pre-split) | Change |
|---|---|---|
| 00–03 | 00–03 | — |
| 38 | 04 | Moved right after 03 (RQ3). |
| 04–26 | 05–27 | Shifted. |
| 27 + 28 | 28 | Merged (Cache local cache and lock). |
| 29–37 | 29–37 | — |
| 39, 40 | 38, 39 | Shifted. |
| 41 | 40, 41 | Split: `ITaleRepository` signatures / optimistic concurrency. |
| 42–51 | 42–51 | — |
| 52 + 53 | 52 | Merged (Logging, Authentication, Api public surface). |
| 54, 55 | 53, 54 | Shifted. |
| 56–59 | 55–58 | Shifted. |
| 60 + 61 | 59 | Merged (Api/Clients/HTTP/Auth docs). |
| 62 + 63 | 60 | Merged (Core/CQRS/Tale/AUID/Cache/Log docs). |
| 64 | 61 | Shifted. |
| 65 | 74 | Moved after the DreamTravel controller step. |
| 66 | 62, 63 | Split: engine / controller, manager, handler, tests. |
| 67 | 64, 65 | Split: HTTP / Api, Authentication file names, HTTP.Testing. |
| 68, 69 | 66, 67 | Shifted; 67 also covers three Logging files over 150 lines. |
| 70 | 68, 69 | Split: MessageBus + ServiceBus.Testing / SQL, Blob, Hangfire, testing kits. |
| 71 | 70 | Shifted. |
| 72 + 74 | 71 | Merged (community health files and root README). |
| 73, 75, 76, 77, 78 | 72, 73, 75, 76, 77 | Shifted. |
| 80 | 78 | Transfer runbook. |
| 79 + 81 | 79 | Merged (license, authorship, URLs; one `Directory.Build.props` PR after the transfer). |
| 82 | 80, 81 | Split: ruleset and stats-bot bypass / Trusted Publishing. |
| 83, 84 | 82, 83 | Shifted. |

Material scope changes applied in the same pass:

- Step 01 adds the Pkcs leaf override in SQL and SQL.Testing, bumps DreamTravel's Aspire parents
  to 13.6.4 (clears MessagePack), swaps `SQLitePCLRaw.bundle_green` for `bundle_e_sqlite3 2.1.14`,
  and removes the stale `NuGetAuditSuppress` entries.
- Step 02 no longer adds Guards.Tests to the slnx; the runner iterates slnx test projects and
  fails at the end.
- Step 03 requires the `nuget-release` environment before its PR merges; post-review step 81
  (now release step 04) drops the `v2.0.0-rc.1` dry run.
- Test projects gain the references their new tests need: SQL.Tests → `SQL.Testing` (15),
  Cache.Tests → `Redis.Testing` (29), MessageBus.Tests → `ServiceBus.Testing` (34),
  API.Tests → `API.Testing` (36), Hangfire.Tests → `Hangfire.InMemory 1.0.0` (11),
  Logging.Tests → `Microsoft.AspNetCore.TestHost 10.0.9` (14).
- Step 12's shared `ServiceBusClient` is a keyed singleton built from options with the existing
  transport and retry settings; the broker stops disposing it.
- Step 32 keeps the 1.x `DispatchInScope(IEvent)` overload for 2.x; step 51 removes legacy
  `Type.Name` recurring IDs automatically.
- Step 52: CQRS owns its `ActivitySource` on `OperationsName`.
- Post-review step 83 (now step 71) creates five new `docs/architecture/` pages (Core errors,
  infrastructure modules, Cache, Logging, AUID).

### Public API changes requiring `CLAUDE.md §2` confirmation

Confirm or decline each ID. A declined item is removed from its step by the plan-reviewer.
"Optional" marks items with high consumer churn and a weak payoff.

**Decision (user, 2026-11-02):** `C-01`…`C-24` are confirmed, including the optional `C-12`,
`C-23`, and `C-24`. `C-25` is **rejected**: the user's blanket "all" confirmation is overridden by
the specific Q1 answer to keep Guards and Scheduler. The additive `C-26` and `C-27` were confirmed
in the plan-review answers (RQ5, RQ6, user, 2026-11-02).

**Decision (user, 2026-11-03):** `C-28` is confirmed (`CLAUDE.md §2`): `SolTechnology.Core.Authentication`
becomes `SolTechnology.Core.Auth` in 2.0, because authorization joins the package later.

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-01 | all | `src/Directory.Build.props` `<Version>1.0.0</Version>` → `2.0.0`; delete `<Version>1.2.0</Version>` from `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj:4` (Logging 1.2.0 → 2.0.0) | core 04 |
| C-02 | Core | `Result.IsSuccess { get; init; }`, `Result.Error { get; init; }`, `Result<T>.Data { get; init; }` (`Result.cs:11,17,52`) → `private init`; NEW `Result<T>.FailAsTask(Error)` returning `Task<Result<T>>` | result 02 |
| C-03 | Tale | `IChapter<TContext>.Read(TContext)` (`IChapter.cs:25`) and `Chapter<TContext>.Read(TContext)` (`Chapter.cs:37`) → `Read(TContext context, CancellationToken cancellationToken)` | tale 07 |
| C-04 | Tale | `IInteractiveChapter<TContext,TChapterInput>.ReadWithInput(TContext, TChapterInput)` (`IInteractiveChapter.cs:22`) and `InteractiveChapter<,>.ReadWithInput` (`InteractiveChapter.cs:28`) → add `CancellationToken cancellationToken` | tale 07 |
| C-05 | Tale | `ITaleRepository`: `FindById(Auid)` → `FindById(Auid, CancellationToken)`; `FindByIdempotencyKey(string)` → `(string, CancellationToken)`; `ListAsync(...)` → `List(..., CancellationToken)`; `SaveAsync(TaleInstance)` → `Task<Result> Save(TaleInstance, CancellationToken)` (returns `ConflictError` on version mismatch); `DeleteAsync(Auid)` → `Delete(Auid, CancellationToken)`; the same on `InMemoryTaleRepository`; NEW `TaleInstance.Version` (`long`) | tale 08 (signatures), tale 09 (`Version`, `ConflictError`) |
| C-06 | Tale | DELETE `TaleOptions.RestrictControllerToRegisteredHandlers` (`TaleOptions.cs:24`); `TaleHandlerRegistry.Register(Type)` (`ModuleInstaller.cs:21`) → `internal`; NEW `ITaleBuilder.ExposeOverHttp<THandler>()` opt-in | tale 11 |
| C-07 | Tale | `SolTechnology.Core.Tale.Models.SchemaBuilder` (`Models/DataField.cs:36`) → `internal` | tale 11 |
| C-08 | AUID | `Auid`, `AuidTypeConverter`, `AuidJsonConverter` move from the global namespace to `SolTechnology.Core.AUID`; both converters become `sealed` | auid 03 |
| C-09 | AUID | DELETE static `Auid.TimeProvider { get; set; }` (`Auid.cs:40`); NEW `Auid.New(string? code, TimeProvider timeProvider)` and `Auid.New<T>(TimeProvider timeProvider)` | auid 03 |
| C-10 | HTTP | `System.Net.Http.HttpClientExtensions` (`HttpClientExtensions.cs:4`, both `CreateRequest` overloads) → `SolTechnology.Core.HTTP.HttpClientExtensions` | http 05 |
| C-11 | HTTP | `BaseUpstreamServiceHealthCheck<TReport>` primary constructor: `HttpClient` parameter → `IHttpClientFactory httpClientFactory, string httpClientName`; `AddSolUpstreamHttpHealthCheck<TCheck>` stops registering `TCheck` as a singleton that captures an `HttpClient` | http 05 |
| C-12 | HTTP (optional) | `RequestBuilder.GetAsync/PostAsync/PutAsync/PatchAsync/DeleteAsync` (plus the `<TResponse>` overloads) and `TryGetAsync/TryPostAsync/TryPutAsync/TryPatchAsync/TryDeleteAsync<TResponse>` lose the `Async` suffix | http 06 |
| C-13 | Cache | `IDistributedLockService.TryAcquireLockAsync` → `TryAcquireLock`; `IIdempotencyStore.TryAddAsync/GetAsync/SetResponseAsync/RemoveAsync` → `TryAdd/Get/SetResponse/Remove` | cache 04 |
| C-14 | MessageBus | `MessageBusBroker`, `IMessageBusBroker` (with `EnsureInitializedAsync`), `MessagePublisher`, `MessageBusReceiver` → `internal` | messagebus 04 |
| C-15 | SQL | `IUnitOfWork` gains `IUnitOfWork Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)` and extends `IDisposable`; `UnitOfWork.Begin` return type `UnitOfWork` → `IUnitOfWork` | sql 02 |
| C-16 | SQL | `SQLConnectionFactory`, `UnitOfWork` → `internal` | sql 04 |
| C-17 | SQL | `SQLProjectDeployer.DeployDacpacAsync(string, string, string, CancellationToken)` → `DeployDacpac(string dacpacPath, string connectionString, string databaseName, ILogger logger, DacDeployOptions? deployOptions = null, CancellationToken ct = default)`, with `BlockOnPossibleDataLoss = true` by default | sql 04 |
| C-18 | Blob | `BlobConnectionFactory` → `internal`; `BlobContainerClientExtensions` namespace `Azure.Storage.Blobs` → `SolTechnology.Core.Blob`; `ReadFromBlob<T>` and both `WriteToBlob` overloads gain `CancellationToken ct = default` | blob 02 |
| C-19 | Hangfire | `IJob.Execute(CancellationToken)` returns `Task<Result>` instead of `Task`; `AddSolRecurringJob<TJob>` gains optional `string? jobId = null, TimeZoneInfo? timeZone = null` (binary break) | hangfire 04 |
| C-20 | Logging | `ILoggerExtensions` (`ILoggerExtensions.cs:12`) → `LoggerExtensions` with an `extension(ILogger logger)` block; `CoreLoggingActivitySources.Operations` (`CoreLoggingActivitySources.cs:32`) → `internal`; `OperationsName` stays public and CQRS creates its own `ActivitySource` with that name (RQ9) | logging 05 |
| C-21 | Authentication | `ApiKeyAuthenticationSchemeOptions.ApiKey` public field → `{ get; set; }` property; `ApiKeyAuthenticationHandler` constructor drops the `ILogger<ApiKeyAuthenticationHandler> logger` parameter (uses the inherited `Logger`) | api-auth 06 |
| C-22 | Api | `ConfigureSwaggerOptions` (`Api/ModuleInstaller.cs:247`) → `internal sealed` | api-auth 06 |
| C-23 | Testing kits (optional) | Drop `Async`: `AzuriteFixture.InitializeAsync/ClearAsync/CreateBlobContainerAsync`, `RedisFixture.InitializeAsync/FlushAsync`, `SQLFixture.InitializeAsync/ResetAsync`, `ServiceBusFixture.InitializeAsync` (also gains `CancellationToken ct = default`), `ContainerLifecycleHelper.EnsureRunningAsync/WaitForAmqpReadyAsync/WaitForHealthyAsync` | testing-kits 03 |
| C-24 | HTTP.Testing (optional) | `WireMockStartup.WireMockServer` public field → get-only property; DELETE unused abstract `FakeApiBase.BaseUrl` | testing-kits 03 |
| C-26 | Tale (additive) | NEW public `SolTechnology.Core.Tale.Persistence.TaleJsonOptions` with `JsonSerializerOptions Default { get; }` (moved from the internal `Orchestration.TaleJsonOptions`, read-only instance); used by `TaleInstance.Clone()` and documented for custom repositories (RQ5) | tale 02 |
| C-27 | MessageBus (additive) | NEW `MessageBusConfiguration.MaxDeliveryCount` (`int`, default 10) used by the receiver to dead-letter on the last attempt (RQ6) | messagebus 03 |
| C-28 | Authentication → Auth (user, 2026-11-03) | NuGet `PackageId`, `AssemblyName`, project `src/SolTechnology.Core.Authentication/SolTechnology.Core.Authentication.csproj` → `src/SolTechnology.Core.Auth/SolTechnology.Core.Auth.csproj`, test project `tests/SolTechnology.Core.Authentication.Tests` → `tests/SolTechnology.Core.Auth.Tests`, and namespace `SolTechnology.Core.Authentication` → `SolTechnology.Core.Auth`; `ModuleInstaller.AddSolAuthentication(IServiceCollection, AuthenticationConfiguration)` → `AddSolAuth(IServiceCollection, AuthConfiguration)`, the one entry for authentication and authorization; `AuthenticationConfiguration` → `AuthConfiguration`; `ApiKeyAuthenticationHandler` and `ApiKeyAuthenticationSchemeOptions` keep their names (`C-21` still applies). The 1.x ID is deprecated on nuget.org with the alternative `SolTechnology.Core.Auth` (release 05, manual owner action). | api-auth 01 |
| C-29 | Tale (additive, user D1, 2026-11-03) | NEW `TaleInstance.LeaseExpiresAt` (`DateTimeOffset?`, UTC), the persisted expiry of a `Running` tale's lease; `TaleOptions.RunningLeaseTimeout` (added by the tale post-completion fix T5) now accepts 1 second to 49 days | tale post-completion fix T7 |
| C-25 | Guards, Scheduler | **Rejected (Q1, user, 2026-11-02).** Was: DELETE projects `src/SolTechnology.Core.Guards`, `src/SolTechnology.Core.Scheduler`, `tests/SolTechnology.Core.Guards.Tests` (all public symbols) | — (step removed) |

**Not planned** (recorded so the omission is deliberate, per `naming-and-public-api.md`):
`SqlErrorTranslator`/`SqlHealthCheck`/`SqlHealthCheckExtensions`/`AddSolSqlHealthCheck` acronym
renames; `Auid` → `AUID` type rename; `ApiKey*`/`FakeApiBase`/`Http*` type renames (file-name
alignment only, in phase H); moving `IEvent` into `SolTechnology.Core`; `HttpClientMetrics`
constructor visibility; `ServiceCallErrorMapper.FromDeserializationFailure` unused `body`
parameter; `UntilAsync`, `CheckHealthAsync`, `InvokeAsync`, `StartAsync`/`StopAsync`, and
`OnResultExecutionAsync` (framework-imposed names, Guide §10).

### Behavioural and persisted-contract breaks (premortem focus, no symbol change)

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-01 | `Error` subtypes not registered serialize as their nearest ancestor; Tale errors gain discriminators through `TaleJsonOptions.Default`, which `TaleInstance.Clone()` and the DreamTravel SQLite repository now use | JSON shape for unknown subtypes; no more `NotSupportedException`; custom repositories must use `TaleJsonOptions.Default` to keep Tale error types (`C-26`) | result 01, tale 02 |
| P-02 | Only `TaleManager`-driven runs are persisted | Custom repositories stop receiving direct-handler runs | tale 03 |
| P-03 | Chapter history keyed by step index + version-free full type name (namespace, name and generic arguments, no assembly information), stored in the new `ChapterInfo.StepIndex` (`int?`) and `ChapterInfo.ChapterTypeName`; `ChapterId` is display only | In-flight persisted tales created by 1.x cannot resume: `ResumeStory` returns an error that says the tale was created by 1.x and must be restarted (the stored tale is left unchanged); migration note | tale 10 |
| P-04 | `TaleInstance.Version` persisted; conditional save | Custom repositories add a `Version` column (existing rows default to `0`) and make `Save` conditional: write only when the stored version (a missing tale counts as `0`) equals `TaleInstance.Version`, then store and return `Version + 1`; on a mismatch, or when a new tale reuses another tale's idempotency key, return `ConflictError` without writing. DreamTravel's SQLite file gains the column on start | tale 09 |
| P-05 | New event jobs target `Task DispatchInScope(IEvent, CancellationToken)` (internal type, persisted job signature); the 1.x `DispatchInScope(IEvent)` overload stays for 2.x (RQ10) | Already-enqueued 1.x event jobs still run; the legacy overload is removed in 3.0 | hangfire 03 |
| P-06 | Recurring job id `Type.Name` → `Type.FullName`; stale tracked ids and legacy `Type.Name` ids removed at start (RQ10) | Old recurring entries are removed automatically and re-created under new ids; no double firing | hangfire 04 |
| P-07 | Cache keys namespaced by `TKey`/`TItem`: `ISingletonCache` and `IRedisCache` keys are `{TKey type}:{TItem type}:{JSON key}` with version-free type names (namespace, name, and generic arguments, no assembly information; RM-3), and Redis prepends `InstanceName`. `DistributedCacheConfiguration.InstanceName` has no default and is validated non-empty at start, like `ConnectionString` (no `required` keyword). `InstanceName` also prefixes the Redis lock (`{InstanceName}lock:{name}`) and idempotency (`{InstanceName}idempotency:{key}`) keys, whose composition is unchanged (RM-2) | Existing cache entries are orphaned (cold cache); config without `InstanceName` or `ConnectionString` fails at start. `InstanceName = "SolTechnology:"` (the 1.x default) keeps the 1.x lock and idempotency keys; any other value makes 1.x and 2.x instances miss each other's locks and idempotency records during a rolling deploy | cache 03 |
| P-08 | `AuidJsonConverter` throws `JsonException` on invalid input; empty string no longer becomes `Auid.Empty` | Error type at deserialization | auid 02 |
| P-09 | The `ProblemDetails.title` of every mapped exception is a fixed text (5xx: fixed texts; 4xx: the HTTP reason phrase, amended by the api-auth code review 2026-11-04); `HttpRequestFailedException.Message` has no query string | Clients that parsed the title | api-auth 03, api-auth code review fixes |
| P-10 | POST/PATCH not retried after an exception or timeout | Fewer retries for unsafe verbs | http 02 |
| P-11 | `WireMockStartup` `AllowPartialMapping` defaults to `false` | Loose tests start failing (intended) | testing-kits 01 |
| P-12 | `TaleController` serves only handlers opted in via `ExposeOverHttp<T>()` | Endpoints that worked implicitly return 404 | tale 11 |
| P-13 | `SolTechnology.Core` no longer brings `Microsoft.Extensions.*` transitively | Consumers relying on the transitive reference must add it | core 07 |
| P-14 | Inbound `X-Correlation-Id` outside `[A-Za-z0-9._:-]` is replaced | Clients sending such IDs see a new ID | logging 04 |
| P-15 | A failed request logs one Error entry, not two | Error-rate alerts change | logging 03 |
| P-16 | API key in the request is compared as a SHA-256 hash in constant time; a blank configured scheme key never authenticates (RM-19), and `AddSolAuth` rejects a null, empty or whitespace `ApiKey` at registration (amended by the api-auth code review 2026-11-04) | A whitespace-only key stops the host at registration instead of answering 401 to every request | api-auth 04, api-auth code review fixes |
| P-17 | The receiver dead-letters a failing message when `DeliveryCount >= MessageBusConfiguration.MaxDeliveryCount` (default 10) instead of always abandoning; the dead-letter reason is `HandlerFailedMaxDelivery` and the description is the handler exception's message (instead of the broker's `MaxDeliveryCountExceeded`) | Set `MaxDeliveryCount` to the entity's value; a lower value dead-letters earlier than the broker would; dead-letter tooling that filters on `MaxDeliveryCountExceeded` must also accept `HandlerFailedMaxDelivery` | messagebus 03 |
| P-18 | `TaleInstance.LeaseExpiresAt` persisted (tale T7): every `Running` save stores the writer's now + `RunningLeaseTimeout`, every other status stores `null`; `ResumeStory` takes over a `Running` tale only once it has passed; without it, once `LastUpdatedAt` + the reader's `RunningLeaseTimeout` has passed (T8); renewals save through a fresh DI scope and leave `LastUpdatedAt` alone | Custom repositories add a nullable `LeaseExpiresAt` column and store the same instant on every `Save`, inserts and updates alike (no database default, no local-time conversion); existing `Running` rows without it are held for one timeout after their last save, then resumable. A one-time read-back logs an error when a repository stores another value (T8). DreamTravel's SQLite file gains the column on start | tale post-completion fixes T7, T8 |
| P-19 | The custom options of `AddSolHTTPClient<TIClient, TClient, TOptions>` (bound from `HTTPClients:{name}:Options`) are validated with `ValidateDataAnnotations().ValidateOnStart()` (premortem RM-13) | Options that 1.x accepted although they break their own data annotations (`[Range]`, `[Required]`, …) now stop the host at start with `OptionsValidationException`; option types without annotations are unaffected | http 04 |
| P-20 | `CacheConfiguration.ExpirationSeconds` and `DistributedCacheConfiguration.ExpirationSeconds` are validated `> 0` at start (premortem RM-13). A partial `CacheConfiguration` passed to `AddSolLocalCache(config)` without `ExpirationSeconds` fails at start; it does not fall back to the 20-minute default | A `Cache` section with only `ExpirationMode` (seconds `0`), which 1.x accepted and then failed on the first `GetOrAdd`, now stops the host with `OptionsValidationException`; `AddSolLocalCache()` without a configuration keeps the 20-minute absolute default | cache 03 |
| P-23 | `SQLConfiguration.ConnectionString` is validated non-empty and parsable by `SqlConnectionStringBuilder` at start (premortem RM-13; parsing added by the code review 2026-11-05); `ISQLConnectionFactory.CreateConnection` retries only a transient `SqlException` (the numbers of EF Core's `SqlServerTransientExceptionDetector` plus `-2`, `53`, `258` connect timeout, and `10061` connection refused, checked on every error of the exception; code review 2026-11-05) and disposes the connection when it gives up; `AddSolSQL(null)` throws `ArgumentNullException`; `SqlErrorTranslator` puts a fixed text per error class into `Error.Description` instead of the SQL Server message, and `Execute` translates only `2627`, `2601`, `1205`, and `-2` (code review 2026-11-05) | An empty, whitespace, or malformed (for example an unknown keyword) connection string stops the host with `OptionsValidationException` instead of failing at the first connection; a non-transient failure (a wrong password, any non-`SqlException`) surfaces at once instead of after about 39 s of retries, while a refused, unreachable, or timed-out server is still retried as in 1.x; callers that showed or parsed `Description` no longer see constraint, table, or value names; a `SqlException` with any other number (for example `208`, `229`, `547`) propagates from `SqlErrorTranslator.Execute` instead of becoming a failed `Result` | sql 03 |
| P-24 | `MessageBusConfiguration.ConnectionString` is validated non-empty and `MessageBusConfiguration.MaxDeliveryCount` greater than zero at start (premortem RM-13) | An empty or whitespace `ConnectionString`, which 1.x reported as `ArgumentException` from the broker constructor, and a `MaxDeliveryCount` of zero or less now stop the host with `OptionsValidationException` | messagebus 03 |

### Log contract changes (Guide §15 "report only", accepted for 2.0)

- **L-01** `LoggingPipelineBehavior` logs `Result.Fail` as a failure with the error type
  (`CQRS/PipelineBehaviors/LoggingPipelineBehavior.cs:55-57`). Step: cqrs 03.
- **L-02** `HttpPolicyFactory` retry log passes the exception as the first argument, so the
  `{Exception}` placeholder disappears (`HttpPolicyFactory.cs:140-143`). Step: http 03.
- **L-03** `[{Placeholder}]` brackets in every module:
  - Tale: `TaleEngine.cs`, `TaleHandler.cs`, `TaleManager.cs`, `TaleController.cs`
    (`cqrs-tale#24`).
  - Api: `ExceptionFilter.cs:67,93`.
  - HTTP: `ModuleInstaller.cs:143` and `BaseUpstreamServiceHealthCheck.cs:110,119,126`, where
    `{HealthPath}` becomes `{UpstreamURL}`: the log property is renamed, so queries on `HealthPath`
    must use `UpstreamURL` (http 10).
  - Cache: the lock and idempotency services (`logging-cache#20`): `{LockKey}` becomes `[{LockKey}]`, and
    `{Key}` becomes `[{IdempotencyKey}]`: the log property is renamed, so queries on `Key` must use
    `IdempotencyKey` (cache 08).
  - MessageBus: the receiver and publisher (`infra#20`): 11 templates now bracket `MessageType`, `MessageId`,
    `Entity`, `DeliveryCount`, `Source`, `Namespace` and `Message`; no property is renamed (messagebus 08).

  Steps: tale 16, api-auth 10, http 10, cache 08, messagebus 08; every other library checks its
  templates in its conformance step (result 05, logging 07, cqrs 06, auid 06, sql 07, blob 05,
  hangfire 07, testing-kits 06).
- **L-04** `SQLProjectDeployer` logs through `ILogger` instead of `Console.WriteLine`. Step: sql 04.
- **L-05** Tale log scope carries the real `TaleId` instead of `Auid.Empty`. Step: tale 05.
- **L-06** The HTTP circuit-breaker-opened warning passes the exception as the log entry's
  exception instead of an `[{Exception}]` placeholder; the template is
  `Circuit breaker opened for [{ClientName}]` (`CircuitBreakerOptionsBuilder.cs`). Step: http code
  review fixes (2026-11-03).
- **L-07** While the shared Redis connection is down, Cache logs `Redis not connected; skipping
  [{Operation}] for [{Key}]` (Warning on the first skip per operation, then Debug for 30 s) instead of the
  per-call "… failed …" warnings. Record: `2026-11-05-cache-redis-fail-fast`.

### Release-notes inputs (consumed by release 05)

Release 05 writes `CHANGELOG.md` and `docs/migration-2.0.md` from this brief after this feature's
working folder is deleted. Its inputs are the `C-nn`, `P-nn`, and `L-nn` tables above, as amended
by the `Deviations` of this brief and of every library brief (see "Per-library regroup"), plus
these notes from the steps:

- auid 01 (former step 19, premortem RM-11): Hosts that ran 1.x under a non-Gregorian current culture (for example
  th-TH or ar-SA) wrote AUID strings with a shifted year. 2.0 rejects them in `TryParse` and in JSON, so records that
  store such strings cannot be loaded until the IDs are migrated.
- hangfire 03 (former step 32, P-05): 1.x event jobs keep running in 2.x; the legacy one-argument overload is removed
  in 3.0. No queue drain is needed.
- tale 09 (former step 41, P-04): custom repositories add a `Version` column and a conditional write (the P-04 row
  spells out the contract); DreamTravel's SQLite file is migrated on start. `TaleManager` now stores a new tale, with
  its idempotency key, before the first chapter runs, so a concurrent `StartStory` with the same key returns the
  first tale, and a `CancelStory` during a run is no longer overwritten (the run stops with `ConflictError`).
- tale 10 (former step 42, premortem RM-5): Cut Tale hosts over in one step. Stop every 1.x host before a 2.x host
  serves tale traffic. Do not roll back to 1.x after a 2.x host has written tale state: 2.x writes Tale-error
  discriminators, `StepIndex`, and `Version` that 1.x cannot read or preserve.
- core 04: 1.x hotfixes, if ever needed, branch from the `v1.0.0` tag, never from `master`.
- core 04 (premortem RM-1): Upgrade every `SolTechnology.Core.*` package to 2.0 together. A mixed 1.x/2.x graph
  compiles but fails at runtime (`FieldAccessException`, `MissingMethodException`, `TypeLoadException`).
  Group Dependabot/Renovate updates for `SolTechnology.Core*`. 2.x packages warn with `SOLCORE001` when the
  resolved `SolTechnology.Core.*` packages span more than one major version.
- api-auth 01 (`C-28`, user, 2026-11-03): `SolTechnology.Core.Authentication` is now `SolTechnology.Core.Auth`.
  Replace the package reference, `using SolTechnology.Core.Authentication;` → `using SolTechnology.Core.Auth;`,
  `AddSolAuthentication(...)` → `AddSolAuth(...)`, and `AuthenticationConfiguration` → `AuthConfiguration`
  (`IOptions<AuthConfiguration>`). Behaviour, the `X-API-KEY` header, and the `ApiKey` scheme are unchanged.
  `SolTechnology.Core.Authentication` stays at 1.x and is deprecated on nuget.org; keeping it next to 2.x packages
  warns with `SOLCORE001`.
- api-auth (code review, 2026-11-04, amends P-09 and P-16): the `ProblemDetails.title` of a mapped
  4xx exception is the HTTP reason phrase ("Bad Request", "Forbidden", "Not Found"; "The request was
  rejected" for a code without one) instead of the exception message, because BCL messages carry
  file paths, internal keys and parameter names. The message is still under `extensions.exception`
  when `IncludeExceptionDetails` is on; a message meant for the client goes into an `Error` on the
  `Result` path, whose `Message` stays the title. `FluentValidation.ValidationException` keeps its
  title and field errors. `AddSolAuth` now throws `ArgumentException` for a whitespace-only `ApiKey`
  as well as a null or empty one.
- result (post-completion review, 2026-11-03): `Result` and `Result<T>` have no public constructor
  (`new Result()` no longer compiles; build results through the factories). Deserializing JSON whose
  `IsSuccess` and `Error` disagree — `{}`, `{"IsSuccess":false}`, or a success carrying an error —
  throws `JsonException`; every shape 1.x and 2.0 write still reads.
- logging (post-completion review, 2026-11-03, amends P-14): correlation id resolution is now
  inbound trace context (`traceparent`, or an `Activity` with a remote parent) → a valid
  `X-Correlation-Id` → the host's request `Activity` trace id → a generated id. Before, any
  `Activity` won, and ASP.NET Core creates one whenever logging is on, so a caller's
  `X-Correlation-Id` was replaced in production; it is now used and echoed (without a
  `traceparent`). Callers that send both headers keep getting the trace id. A client that disconnects
  while an `IRequiresRequestBody` enricher reads the body now produces the usual single abort
  Warning instead of an unlogged exception from scope building.
- auid (post-completion review, 2026-11-03, amends P-08): `AuidTypeConverter.ConvertFrom` throws
  `FormatException` for an invalid string, as 1.x did (the interim `NotSupportedException` is
  dropped), so MVC model binding still reports "The value '…' is not valid…". `Auid.New` with a
  `TimeProvider` encodes the UTC instant even when the provider reports a non-zero offset (1.x and
  the 2.0 preview used the offset-local clock time), and a clock before 2001-01-01 now throws
  `InvalidOperationException` instead of producing a wrong ID.
- tale (post-completion review, 2026-11-03): a chapter or inline step that throws no longer
  exposes the exception text: the result, the HTTP body and the stored `ChapterInfo.Error` read
  "Chapter [X] failed unexpectedly." (or "Tale [X] failed unexpectedly.") with the trace ID as
  `CorrelationId`; the exception is logged. A run cancelled by its token between saves stores
  `Cancelled` and an exception outside a chapter stores `Failed` (both could store `Completed`). A
  synchronous throw from a non-async `ReadWithInput` is classified like an async one.
  `GET`/`DELETE /api/tale/{id}` return the "Tale not found" 404 for a tale whose handler is not
  exposed over HTTP, and `DELETE` of a missing tale returns 404 instead of 400.
- tale (post-completion review, user decision 2026-11-03, amends the `tale 09` note): new
  `TaleOptions.RunningLeaseTimeout` (default 5 minutes). `ResumeStory` takes over a `Running` tale
  whose `LastUpdatedAt` is older than the timeout, re-running its interrupted chapter, so chapters
  with side effects must be idempotent. Custom repositories must store `LastUpdatedAt` unchanged in
  UTC; no schema change.
- tale (post-completion review, user decision 2026-11-03, heartbeat): a persisted run renews its
  `Running` lease every third of `RunningLeaseTimeout` while its chapters and steps execute, so the
  timeout no longer has to exceed the longest chapter; it bounds how long a crashed tale stays
  blocked. Each renewal is an ordinary conditional `Save` (only `LastUpdatedAt` and `Version`
  change), so repositories see one extra write per running tale per third of the timeout. A run
  whose renewal finds the tale taken over cancels the chapter's `CancellationToken`, saves nothing
  more, and returns `ConflictError`; chapters receive that token, so they can see a cancellation
  that the caller did not request. Direct `Handle` runs never renew.
- tale (premortem follow-up, user decisions D1/D2 2026-11-03, amends the two notes above, `C-29`,
  `P-18`): the lease is the new persisted `TaleInstance.LeaseExpiresAt` (`DateTimeOffset?`, UTC),
  not `LastUpdatedAt`. The claim save and every renewal store the writer's clock plus its
  `RunningLeaseTimeout`, any other status stores `null`, and `ResumeStory` takes over a `Running`
  tale once the expiry has passed; a `Running` row without it counts as expired. Hosts with
  different timeouts therefore agree, and renewals no longer change `LastUpdatedAt`, which again
  means the last state save. Migration for custom repositories: add a nullable `LeaseExpiresAt`
  column, store and return the same instant on every `Save`, renewals included (no database
  default, no local-time conversion), and throw when a save's outcome is unknown instead of
  reporting a conflict. Renewals resolve the repository from a fresh DI scope, so a `Scoped`
  repository is never used by a renewal and a chapter at once. `RunningLeaseTimeout` accepts
  1 second to 49 days. A chapter running for three timeouts logs one warning; a hung but alive
  chapter keeps its lease and is recovered with `CancelStory`. A `StartStory` with an idempotency
  key whose run lost the tale returns success with the new owner's stored state.
- tale (LeaseExpiresAt premortem follow-up T8, D3/D4 decided by the implementer, delegated by the
  user 2026-11-03, amends the note above and `P-18`): a `Running` row without `LeaseExpiresAt` no
  longer counts as expired. `ResumeStory` holds it until its `LastUpdatedAt` plus the reader's
  `RunningLeaseTimeout`, answers `ConflictError` meanwhile and logs a warning naming the
  `ITaleRepository.Save` contract, then takes it over. Once per host, after the first claim of a new
  tale, the first claim of a resumed tale and the first lease renewal, the run reads the tale back
  and logs an error ("... ITaleRepository.Save must store LeaseExpiresAt unchanged, or another host
  may take over a live tale") when the stored `LeaseExpiresAt` differs from the written one by more
  than one second at the same `Version`; the run continues. Migration for custom repositories:
  store `LeaseExpiresAt` on updates as well as inserts, prefer a `datetimeoffset` / `timestamptz`
  column, and with a `DateTime` column write `UtcDateTime` and read it back with
  `DateTime.SpecifyKind(value, DateTimeKind.Utc)`; treat the read-back error as a repository bug.
- http (code review, 2026-11-03, L-06): `HttpPolicyConfiguration` ranges now match Polly 8 and are
  checked at host start: `RequestTimeout` and `OverallRequestBudget` 10 ms to one day,
  `CircuitBreakerDelayDuration` and `CircuitBreakerSamplingDuration` 500 ms to one day,
  `RetryInitialDelay` 0 ms and `RetryTimeout` 1 ms to one day. A value outside them used to start
  the host and fail every request; it now stops the host with `OptionsValidationException`.
  `MaxRequestRetries = 0` now sends each request once and keeps the circuit breaker and timeouts
  (before, every request failed). A cancellation of the caller's own token is no longer counted by
  the circuit breaker and logs no retry warning or retry metric; other cancellations, such as
  `HttpClient.Timeout`, still count. The `Try*` verbs map an open circuit (`BrokenCircuitException`,
  `IsolatedCircuitException`) to a recoverable `Error` "Upstream circuit open" instead of the
  non-recoverable "Unexpected service call error". The circuit-opened warning is
  `Circuit breaker opened for [{ClientName}]` with the exception as the log entry's exception
  (L-06). The `soltechnology.core.http.retries` counter tags exception retries with the request
  method instead of `?`. An unexpected exception (not a transport read failure) while reading an
  error body now propagates instead of being swallowed.
- cache (code review, 2026-11-04): `AddSolDistributedCache` opens its shared Redis connection with
  `abortConnect=false`, whatever the connection string says, so Redis being down at start no longer
  makes the lock, the idempotency store and the health check throw on resolution, or every
  `IRedisCache.GetOrAdd` retry a blocking connect; they fail open (the cache returns the factory
  result, the lock returns `null`, `TryAdd` returns `true`, the health check reports `Unhealthy`)
  while the connection retries in the background. During an outage each Redis call waits up to the
  connection's timeout before failing open. `IDistributedLockService.TryAcquireLock` throws
  `ArgumentOutOfRangeException` for an expiry that is zero, negative, or longer than 365 days, in both
  implementations; before, the in-process lock handed out a lock that expired at once (or logged a
  backend failure for `TimeSpan.MaxValue`) and the Redis lock returned `null`. Releasing a Redis lock
  that expired before its release logs the new Warning `Lock [{LockKey}] expired before release`
  instead of the Debug `Lock released: [{LockKey}]`, and disposing a Redis lock handle twice releases
  once.
- cache (Redis fail-fast, 2026-11-05, [`2026-11-05-cache-redis-fail-fast`](2026-11-05-cache-redis-fail-fast.md)): while
  the shared Redis connection is down, `IRedisCache.GetOrAdd`, the Redis lock (`TryAcquireLock`), and the Redis
  idempotency store skip Redis at once and fail open with the same results as on a Redis error, instead of waiting
  5–11 seconds for the StackExchange.Redis timeouts. StackExchange.Redis reconnects the same connection by itself, and
  the consumers use Redis again as soon as it does. Each skip logs the new template
  `Redis not connected; skipping [{Operation}] for [{Key}]` without an exception: Warning for the first skip of an
  operation in 30 seconds, Debug for the rest. During an outage this entry replaces the per-call failure warnings
  (`Distributed cache GET failed …`, `Lock acquisition failed …`, `Idempotency … failed …`), so alerts on those
  templates should also match the new one. The health check still reports `Unhealthy`. No public API or DI change.
- sql (code review, 2026-11-05, amends P-23): `SqlErrorTranslator.Execute` translates only the errors it names
  (`2627`, `2601`, `1205`, `-2`). A `SqlException` with any other number (for example `208` invalid object, `229`
  permission denied, `547` foreign key) now propagates to the caller and the exception filter, which logs it, instead
  of becoming a failed `Result` whose `Error` says only "SQL Server returned error number N." and is never logged.
  Callers that branched on that plain `Error` catch the exception instead. `CreateConnection` retries the numbers of
  EF Core's `SqlServerTransientExceptionDetector` plus `-2`, `53`, `258`, and `10061`, so a refused, unreachable, or
  timed-out server is retried as in 1.x. A connection string that `SqlConnectionStringBuilder` rejects (for example
  `Server=x;Foo=bar`) now stops the host at start instead of failing at the first connection.
  `SolTechnology.Core.SQL` and `SolTechnology.Core.SQL.Testing` now depend on `Microsoft.Identity.Client` 4.92.0:
  DacFx 170.x still needs MSAL, which `Microsoft.Data.SqlClient` 7 no longer brings, and without it a DACPAC
  deployment that hits a SQL error threw `FileNotFoundException` instead of `DacServicesException`. Connection strings
  with a SQL login need no change; Entra ID authentication needs `Microsoft.Data.SqlClient.Extensions.Azure` in the
  consumer's project.
- blob (code review, 2026-11-06): `AzuriteFixture` in `SolTechnology.Core.Blob.Testing` starts
  `mcr.microsoft.com/azure-storage/azurite:3.37.0` by default instead of `3.35.0`, which rejected every request of
  `Azure.Storage.Blobs` 12.29.0 (service version `2026-06-06`) with 400 `InvalidHeaderValue`. Tests that pass their own
  `image` keep it and need 3.37.0 or later for the 2.0 packages. `WriteToBlob` throws `ArgumentOutOfRangeException`
  for a `SerializationFormat` value that is not defined, before sending any request; 2.0 before this fix (and 1.x)
  uploaded an empty blob instead. `AddSolBlob` now validates `BlobConfiguration.ConnectionString` at host start
  (premortem RM-13): a null, empty, or whitespace value, or one that `Azure.Storage.Blobs` cannot parse (for example
  `AccountName=x`), stops the host with `OptionsValidationException` instead of failing at the first `GetConnection`.
  No Blob `P-nn` row exists, so this entry is the record.
- messagebus (concurrent resource creation, coordinator 2026-11-09): with `CreateResources = true`, several instances
  that start at once no longer fail when another instance creates the same queue, topic, or subscription between the
  existence check and the create; the "already exists" error is logged at Debug and start continues. Scale-out
  guidance (competing consumers, a per-instance subscription for fan-out, at-least-once delivery, idempotent handlers)
  is in `docs/Bus.md` "Running several instances". Non-breaking.
- messagebus (code review, 2026-11-09, [`2026-11-02-core-2-0-messagebus`](2026-11-02-core-2-0-messagebus.md)): four
  non-breaking fixes. (1) A handler that returns after the host starts stopping now completes its message. Before,
  complete, abandon and dead-letter used the processor's cancellation token, which the SDK cancels when shutdown
  starts, so the message went back to the queue and another instance handled it a second time on every rolling
  deploy; handlers still receive that cancelled token. (2) With `CreateResources = true`, the queues and subscriptions
  the module creates get `MessageBusConfiguration.MaxDeliveryCount` instead of the broker default of 10; existing
  entities are not changed. (3) When completing a message whose handler succeeded fails, for example on a lost lock,
  the receiver logs the new Warning `Failed to complete message [{MessageId}]; lock will expire naturally` and the
  message is delivered again. Before, the failure was logged as `Handler failure …` at Error and the message was
  abandoned, or dead-lettered with reason `HandlerFailedMaxDelivery` on its last delivery. (4) A broker
  initialisation that fails halfway, for example on a provisioning error, can be retried. Before, the retry, such as
  the next publish, added a second sender for each publisher already created, so every message was published twice,
  and receiver registration threw "already registered". No public API change.

### Resolved questions (user answers, 2026-11-02)

No question is open. The recommended defaults were offered on 2026-11-02; the decision column
records what the user chose.

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| Q1 | Delete `SolTechnology.Core.Guards`, `SolTechnology.Core.Scheduler`, `tests/SolTechnology.Core.Guards.Tests`, `docs/Guards.md`, `docs/Cron.md` from `master`? | **No.** All five stay. `C-25` rejected; former step 76 dropped. The Guide §12 fix to BCL guards stays (showcase 04); `dontreadme.md` (kept under its name, user, 2026-11-03) names BCL guards for Guards (showcase 01). | 02, 04, 58, 69; showcase 01, 04 |
| Q2 | Copyright holder and `Authors` | **"Adrian Strugala / Sol Technology"** in `LICENSE`, `<Copyright>`, and `<Authors>`. | release 02 |
| Q3 | GitHub organization slug and nuget.org organization name | GitHub **`sol-technology`**; nuget.org **`SolTechnology`**. | release 01–04 |
| Q4 | DreamTravel `UseNuGet=true` switch? | **No switch.** The DreamTravel README documents the `PackageReference` equivalents (showcase 06); former step 81 dropped. | showcase 06 |
| Q5 | Owner of `S5`/`http-api#4` and the `docs/Auth.md` rewrite | **This feature.** The JWT feature stays JWT-only; its files are not edited here. | 07, 59 |
| Q6 | New test project `tests/SolTechnology.Core.Blob.Tests` (and `*.Testing` tests)? | **No new test projects.** No existing test project references Blob, so steps 16 and 50 validate with build and source checks; accepted risk AR-1. | 11, 16, 50 |
| Q7 | Weekly NuGet stats bot | **Keep committing weekly to `master` as-is.** Former step 74 and its `CLAUDE.md` change dropped. No bypass actor: `master` has no ruleset (decision A, user, 2026-11-02). | release 02 |
| Q8 | `SECURITY.md` channel, Code of Conduct, `CODEOWNERS` | **GitHub Private Vulnerability Reporting; Contributor Covenant 2.1; `* @AdrianStrugala`.** | showcase 01; release 03 |
| Q9 | DreamTravel Azure DevOps pipelines and `legacy_AzureResourceGroup/` | **Keep them (memento); do not delete.** Showcase 07 adds only the new `dreamtravel-tests` GitHub Actions job. | showcase 07 |
| Q10 | NuGet publishing credential after the transfer | **NuGet Trusted Publishing (OIDC)**; no long-lived `NUGET_API_KEY`. | release 01, 04 |

### Plan-review answers (user, 2026-11-02)

The plan-reviewer's critique raised twelve questions. The user answered all of them on
2026-11-02; step numbers are the current ones (after the split).

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| RQ1 | `System.Security.Cryptography.Pkcs` CVE via `Microsoft.Data.SqlClient`, no parent fix | **Leaf override confirmed (`CLAUDE.md §2/§6`):** direct `PackageReference System.Security.Cryptography.Pkcs 10.0.13` (first patched `10.0.x`, outside `[10.0.0, 10.0.12]`) in `SolTechnology.Core.SQL` and `SolTechnology.Core.SQL.Testing`, with a one-line comment and the follow-up "remove when SqlClient bumps". DreamTravel is fixed by bumping parents (`Aspire.Hosting.*` → 13.6.4, which brings `MessagePack` 2.5.307); the stale `SQLitePCLRaw` suppress goes if the patched bundle resolves it. | 01 |
| RQ2 | Package version comes from props, not from the tag | **(c)** Remove the `v2.0.0-rc.1` dry run; no tag-derived versioning. Residual risk recorded as AR-5. | 03; release 04, 05 |
| RQ3 | Move the 2.0.0 bump earlier? | **Yes**, right after step 03 (now step 04). | 04 |
| RQ4 | Guards.Tests in CI | **Not added.** Guards is no longer maintained and stays in the repository for reference only. `runTests.ps1` iterates only the slnx test projects, collects all exit codes, and fails at the end. | 02, AR-2 |
| RQ5 | Tale error serialization outside the engine | **(a)** `TaleJsonOptions.Default` becomes public (`C-26`), used by `TaleInstance.Clone()` and the DreamTravel SQLite repository, documented for custom repositories. | 17, 60 |
| RQ6 | Source of `MaxDeliveryCount` | **(a)** New `MessageBusConfiguration.MaxDeliveryCount`, default 10, additive (`C-27`, P-17). | 34 |
| RQ7 | `master` ruleset with one maintainer | ~~**(a)** Pull request and required checks, 0 approvals.~~ **Superseded by decision A (user, 2026-11-02):** no `master` ruleset or branch protection; CI runs the test suite on push and PR; direct pushes stay allowed. | release 03 |
| RQ8 | Premortem scope over 80+ steps | **(b)** The planning agent splits the feature into separate features, each with its own premortem. No "premortem refresh" steps are added here. Done on 2026-11-02 (see "Split"). | 00; showcase 00; release 00 |
| RQ9 | `CoreLoggingActivitySources.Operations` read by CQRS | **(a)** CQRS creates its own `ActivitySource` named `OperationsName`; the field becomes internal. | 52 |
| RQ10 | Hangfire persisted contracts | **(a)** Keep the legacy one-argument `DispatchInScope(IEvent)` for 2.x and remove legacy `Type.Name` recurring IDs automatically, with tests. | 32, 51 |
| RQ11 | Step restructuring | **Yes.** Splits of former 41, 66, 67, 70, 82; merges of former 27+28, 52+53, 60+61, 62+63, 72+74, 79+81 (see "Plan-review revisions"). | all |
| RQ12 | Uncontroversial fixes | **Yes, all:** `API` path casing, test-project references, the seven unassigned files over 150 lines, validation instead of `required` for `InstanceName`, Guide self-consistency after the DreamTravel controllers, `dontreadme.md` link repairs, SHA pins and `needs` for the DreamTravel CI job, the real DACPAC call site, dead `Remove` blocks, the XML-docs count, the containment-guard allowlist, a credential grep without literal values, the S5/S6 criteria, new architecture pages, environment-before-merge ordering, and the complete premortem trigger list. | many |

### Accepted risks (consequences of the 2026-11-02 decisions)

The retrospectives (core 11 and the library retrospectives; former step 71) carry each item they own
into `Follow-ups`; AR-3 and the release half
of AR-5 are carried by the release feature, and AR-4 also by the showcase feature.

| ID | Risk | Source | Mitigation in this plan |
|---|---|---|---|
| AR-1 | `B6`, `infra#6`, and `infra#7` fixes in Blob ship without automated tests, and Blob has no DI resolution test. | Q6 | Steps 16 and 50 gate on a Release build and source greps; a manual Azurite check is recommended in the PR. Follow-up: a Blob test project. |
| AR-2 | Guards and Scheduler are reference-only and no longer maintained. They keep their known defects (`Guards.IsValid()` inverted, `logging-cache#4`; Scheduler `async void`, recursion, repeated `BuildServiceProvider()`, static state, `infra#17`). `Guards.Tests` stays on xUnit, outside `SolTechnology.Core.slnx`, and is not run in CI. | Q1, RQ4 | Both projects stay `[Obsolete]`, `IsPackable=false`, version `0.5.0`, and excluded from the pack guard; step 02 makes the runner iterate only slnx test projects, so Guards.Tests can neither fail nor hide CI results. |
| AR-3 | The stats bot keeps writing weekly commits directly to `master`; `master` has no ruleset (decision A, user, 2026-11-02). | Q7, decision A | Moved to the release feature: no bypass actor exists; release 03 verifies a stats-bot run after the repository security settings are applied. |
| AR-4 | The DreamTravel Azure DevOps pipelines and legacy ARM folder stay without CI coverage. | Q9 | Showcase 07 runs DreamTravel tests in GitHub Actions. Their local-only credential values stay (AR-6). |
| AR-5 | The package version comes from `<Version>` in `src/Directory.Build.props`, not from the tag, so any `v*` tag on a `master` at 2.0.0 would publish the final 2.0.0 packages; Trusted Publishing is first exercised by the real `v2.0.0` release. | RQ2 | Publishing is tag-only through the protected `nuget-release` environment (step 03); the required reviewer checks that the tag equals `<Version>` before approving (step 03; release 04, 05); 1.x hotfixes branch from the `v1.0.0` tag (step 04); the interim `NUGET_API_KEY` is revoked only after the `v2.0.0` publish succeeds (release 05). |
| AR-6 | Local-only DreamTravel dev credentials stay committed (user decision 2026-11-02): the SQL `sa` password in `appsettings*.json` (including `appsettings.prod.json`, which targets `localhost,1404`), Neo4j at `bolt://localhost`, the Aspire `sql-password` parameter, the database `Dockerfile` `ARG`, and the `devOps/` psake script and `build&test.yml` `docker run`. All target local Docker/localhost; `deploy.yml` uses pipeline variables. | Decision B | No scrub (step 08 keeps only `S6`). The Google API keys and the Service Bus SAS key are real secrets, not covered by decision B; step 09 still removes them and records revocation. Secret scanning after the transfer (release 03) may flag the local values; dismiss those alerts as "used in tests". |

Scope change 2026-11-03 (user): step 09 "Central Package Management" added after the plan review; the retrospective moved to step 10.
Scope change 2026-11-03 (user): step 10 "Per-package NuGet icons" added; the retrospective moved to step 11. It changes no package version.

Scope change 2026-11-03 (user, `C-28`): `SolTechnology.Core.Authentication` is renamed to `SolTechnology.Core.Auth` in 2.0, because authorization joins it later; `AddSolAuthentication` → `AddSolAuth`, `AuthenticationConfiguration` → `AuthConfiguration`, namespace `SolTechnology.Core.Auth`. New api-auth 01 does the rename (and moves `docs/icons/SolTechnology.Core.Authentication.png` to `docs/icons/SolTechnology.Core.Auth.png`); a new api-auth 00 runs its premortem; former api-auth 01–10 are now 02–11, and this brief's api-auth references are renumbered. The release feature adds `SolTechnology.Core.Authentication` to the nuget.org deprecations with the alternative `SolTechnology.Core.Auth`. Core 08's "api-auth step 06" reference (`steps/08-xml-docs-central-switch.md`) now means api-auth 07; that foundation step file is not edited in this pass.

Scope change 2026-11-03 (user: "nie dropuj joke z kodu"): jokes in the code (humorous comments, messages, names, file names) stay exactly as written; recorded in `docs/ClaudeCodingGuide.md` §9.10 and §7 (the §15 "dupa" row is removed). `P1-5` now covers only non-joke Polish text. Affected places:
- core 05 (`done`): the `"dupa"` CORS rename was reverted by commit `7601de9b`; only the `avroconvertonline.azurewebsites.net` origin removal remains (step 05 `Deviations`).
- `src/SolTechnology.Core.CQRS/Nothing.cs`: `//I'm just void. Or nothing` restored (cqrs feature, already completed).
- auid 01 (former 19): the Polish overflow message at `src/SolTechnology.Core.AUID/Auid.cs:186` stays verbatim; only the culture-invariant formatting and range-checked parse fixes remain.
- showcase 01: `dontreadme.md` keeps its name; the move to `docs/deprecated-packages.md` and its link rewrites are dropped (also release 01's reference).
- showcase 04: the "dupa" §15/§7 edits are dropped.
- Release-notes inputs: no entry describes a joke removal; the AUID overflow message is not a 2.0 change.

## Acceptance criteria

- [ ] `dotnet restore SolTechnology.Core.slnx && dotnet build SolTechnology.Core.slnx -c Release`
      succeeds from a clean clone.
- [ ] `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `pwsh ./.github/runTests.ps1` runs every test project listed in `SolTechnology.Core.slnx` and
      exits non-zero when any single one fails, after running the rest (verified once with a
      deliberately failing test on a throwaway branch).
- [ ] `System.Security.Cryptography.Pkcs` is pinned to a patched version in `SolTechnology.Core.SQL`
      and `SQL.Testing` with a removal comment, and the removal is listed under `Follow-ups`.
- [ ] `publishPackages.yml` pushes to nuget.org only for `refs/tags/v*`, through a GitHub
      environment that has required reviewers and existed before the workflow change merged.
- [ ] Owner confirmation is recorded in core 06, and copied into `Completion summary` by core 11,
      that both zip keys, the four history Google keys, and the `talecodeservicebus` SAS key are
      revoked or their resources deleted.
- [ ] `git ls-files | grep -E "\.zip$|\.idea/|settings\.local\.json"` prints nothing.
- [ ] Every DI-registering module except Blob (AR-1) and the reference-only Guards and Scheduler
      (AR-2) has a resolution test with `ValidateOnBuild = true` and `ValidateScopes = true`, and
      none is `[Ignore]`d at feature close.
- [ ] Every finding `S3`, `S4`, and `B1`–`B11` except `B6` (AR-1) has a regression test that fails
      on `4097d47f` and passes at feature close.
- [ ] `S5` (timing leak, not functionally observable) is covered by a source check
      (`CryptographicOperations.FixedTimeEquals` present, no string equality on the key) plus the
      handler unit-test matrix (api-auth 04).
- [ ] `S6` is covered by a DreamTravel component test that runs the API in the `Production`
      environment and asserts that an unhandled exception returns no developer exception page
      (core 05).
- [ ] The confirmed `C-01`…`C-24`, `C-26`, `C-27`, and `C-28` shipped, and `C-25` (rejected) did not: `src/SolTechnology.Core.Guards`,
      `src/SolTechnology.Core.Scheduler`, and `tests/SolTechnology.Core.Guards.Tests` still exist.
- [ ] Every `docs/<Module>.md` referenced by a `PackageReadmeSource` compiles: each C# snippet
      builds in a throwaway console app against the 2.0 packages (recorded in each library feature's
      module-docs step).
- [ ] `GenerateDocumentationFile` is `true` for every packable project via `src/Directory.Build.props`.
- [ ] No `src/SolTechnology.Core.*/**/*.cs` file exceeds 150 lines
      (`find src -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150'` lists none
      except `total`).
- [ ] `dotnet msbuild src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj -getProperty:Version`
      prints `2.0.0`, and no `v2.*` tag exists (publishing 2.0.0 is release 05).

- [ ] Every library feature brief listed under "Per-library regroup" is `status: completed`.

The community-file, README quick-start, and Dependabot criteria moved to the showcase feature;
the license, organization, Trusted Publishing, published-2.0.0, and stats-bot criteria moved to
the release feature (split, 2026-11-02).

## Completion summary

## Deviations

## Follow-ups

- Remove the `System.Security.Cryptography.Pkcs 10.0.13` override from `SolTechnology.Core.SQL` and
  `SolTechnology.Core.SQL.Testing` once `Microsoft.Data.SqlClient` depends on a patched Pkcs
  (recorded at planning time, RQ1; the retrospective adds the rest).

## Post-completion review fixes (2026-11-03)

An independent code review of the completed core 02 (`runTests.ps1`) work found one issue.

- **C2 — finding:** `.github/runTests.ps1` ignored the exit code of `dotnet sln ... list`, matched
  projects with `-like 'tests/*'` (forward slash only), and resolved the slnx from the working
  directory. Run from another directory it printed "All 0 test projects passed." and exited `0`.
  **Fix:** the script resolves the slnx from `$PSScriptRoot/..`, exits `1` when the list command
  fails, matches `^tests[\\/]`, exits `1` with a message when no test project is found, and passes
  absolute project paths to `dotnet test`.
  **Verification (pwsh 7):** the committed script, run from `/Users/.../GitHub` (outside the repo),
  printed "Could not find solution" and exited `0`; after the fix the same run executed all 12 test
  projects and exited `0`; a temporary `Assert.Fail` test (reverted) made it list
  `tests/SolTechnology.Core.Tests/SolTechnology.Core.Tests.csproj` and exit `1`; a copy of the
  script placed where no slnx exists exited `1` with "Listing projects ... failed".
