---
status: in-progress
created: 2026-11-02
completed:
---

# SolTechnology.Core 2.0 — Api and Auth

> Historical delivery record. It may not describe the current system.

## Goal

Deliver the 2.0 hardening of `SolTechnology.Core.API` and `SolTechnology.Core.Auth` (renamed from
`SolTechnology.Core.Authentication`, `C-28`) end-to-end, as one library of the SolTechnology.Core 2.0
flagship release. Success means:

- `SolTechnology.Core.Authentication` is `SolTechnology.Core.Auth` (project, assembly, namespace, NuGet ID), `AddSolAuthentication` is `AddSolAuth`, and `AuthenticationConfiguration` is `AuthConfiguration` (`C-28`).

- Upstream URIs and query strings never reach `ProblemDetails` or exception messages (`S4`, P-09).
- The API key is compared in constant time against a SHA-256 hash, and an empty configured key never authenticates (`S5`, P-16, RM-19).
- `ResultConversionFilter` keeps 201/202 and the filters are really registered (`http-api#7`).
- The 2.0 surface: `ApiKeyAuthenticationSchemeOptions.ApiKey` is a property, the handler uses the inherited logger, and `ConfigureSwaggerOptions` is internal (`C-21`, `C-22`).
- Api and Auth ship XML docs, rewritten `docs/Api.md` and `docs/Auth.md` that compile against 2.0, conform to the Coding Guide, and log with `[{Placeholder}]` templates.

## Context

**Depends on:**

- Core 02, 03, and 04 of [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) are `done` (CI fails on any test failure, tag-only publishing, version 2.0.0 on `master`).
- [`2026-11-02-core-2-0-core-result`](2026-11-02-core-2-0-core-result.md) is `completed`.
- [`2026-11-02-core-2-0-logging`](2026-11-02-core-2-0-logging.md) is `completed`.
- [`2026-11-02-core-2-0-http`](2026-11-02-core-2-0-http.md) is `completed`.

Delivery order position 7 of 13 in the umbrella brief's "Per-library regroup" section. The
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md) feature
depends on this feature being `completed`.

### Origin

Regrouped on 2026-11-02 from [`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md) (user: "podziel implementację feature 1 na
każdą bibliotekę osobno"). Each step names its former core step in a "Former core step" line; the
umbrella brief's "Per-library regroup" table maps every former number. The evidence is the
full-repository code review of 2026-11-02 against `master@4097d47f`; finding IDs follow the
umbrella brief's "Evidence" table. Findings acted on here: `S4`, `S5`, `P1-1`, `P1-2`, `P1-4`, `cqrs-tale#16`, `http-api#2`, `#4`, `#7`, `#11`, `#11-14`, `#18`, `#19`, `#22`–`#24`.

### Decisions owned by this feature

The umbrella brief keeps the shared decision tables and the user's confirmations (`C-01`…`C-24`,
`C-26`, `C-27` confirmed, `C-25` rejected, 2026-11-02; `C-28` confirmed 2026-11-03). The rows below are copied from it. Step
numbers in copied `A`, `AR`, `Q`, and `RQ` rows are former core numbers.

Public API changes (`CLAUDE.md §2`, confirmed by the user on 2026-11-02):

| ID | Package | Exact change | Step |
|---|---|---|---|
| C-21 | Authentication | `ApiKeyAuthenticationSchemeOptions.ApiKey` public field → `{ get; set; }` property; `ApiKeyAuthenticationHandler` constructor drops the `ILogger<ApiKeyAuthenticationHandler> logger` parameter (uses the inherited `Logger`) | api-auth 06 |
| C-22 | Api | `ConfigureSwaggerOptions` (`Api/ModuleInstaller.cs:247`) → `internal sealed` | api-auth 06 |
| C-28 | Authentication → Auth | NuGet ID, `AssemblyName`, project and test-project folders/files, and namespace `SolTechnology.Core.Authentication` → `SolTechnology.Core.Auth`; `ModuleInstaller.AddSolAuthentication(IServiceCollection, AuthenticationConfiguration)` → `AddSolAuth(IServiceCollection, AuthConfiguration)` (one entry for authentication and the authorization that joins later); `AuthenticationConfiguration` → `AuthConfiguration`; `ApiKeyAuthenticationHandler` and `ApiKeyAuthenticationSchemeOptions` keep their names. Confirmed by the user on 2026-11-03. | api-auth 01 |

Behavioural and persisted-contract breaks:

| ID | Change | Consumer impact | Step |
|---|---|---|---|
| P-09 | 5xx `ProblemDetails.title` is a fixed text; `HttpRequestFailedException.Message` has no query string | Clients that parsed the title | api-auth 03 |
| P-16 | API key in the request is compared as a SHA-256 hash in constant time | None functional | api-auth 04 |

Log contract changes:

- **L-03** (Api part): `ExceptionFilter.cs:67,93`. Step 10.

User decisions (2026-11-02):

| ID | Question | Decision (user, 2026-11-02) | Steps |
|---|---|---|---|
| Q5 | Owner of `S5`/`http-api#4` and the `docs/Auth.md` rewrite | **This feature.** The JWT feature stays JWT-only; its files are not edited here. | 04, 08 (former core 07, 59) |

Premortem mitigations carried verbatim from core 00: RM-19 (step 04).

Q5: this feature owns `S5`/`http-api#4` and the `docs/Auth.md` rewrite; [`2026-07-06-jwt-bearer-authentication`](2026-07-06-jwt-bearer-authentication.md) stays JWT-only and its files are not edited here. Step 03 edits HTTP exception code, so HTTP is delivered first. The API.Testing kit is the testing-kits feature.

## Scope

### In scope

- Source: `src/SolTechnology.Core.API`, `src/SolTechnology.Core.Auth` (`src/SolTechnology.Core.Authentication` until step 01), and the matching test projects; plus
  `src/SolTechnology.Core.HTTP/HttpRequestFailedException.cs` and its HTTP.Tests case (step 03, `S4`).
- The `C-28` rename and its in-repository consumers (step 01): `SolTechnology.Core.slnx`, DreamTravel `DreamTravel.Api`, `README.md`, `dontreadme.md`, `docs/ClaudeCodingGuide.md`, the premortem skill's Authentication checklist, and `docs/icons/SolTechnology.Core.Auth.png`.
- Module docs: `docs/Api.md`, `docs/Auth.md`.
- Phases, in order: the `C-28` rename → correctness and broken-module fixes → 2.0 breaking API cleanup → XML docs and
  module docs → Coding Guide conformance and `[{Placeholder}]` log templates → retrospective.

### Out of scope

- JWT bearer authentication ([`2026-07-06-jwt-bearer-authentication`](2026-07-06-jwt-bearer-authentication.md)).
- `ApiKey*` type renames ("Not planned" in the core brief).
- Authorization features (roles, policies) in `SolTechnology.Core.Auth`; `C-28` only prepares the package name and the `AddSolAuth` entry for them.
- nuget.org deprecation of `SolTechnology.Core.Authentication` (release 05, manual owner action).
- Renaming `docs/Auth.md` or `docs/architecture/authentication.md` (both names already fit).
- Work owned by the foundation feature (CI, publishing, version, DreamTravel exception page, secret
  artifacts, zero-dependency base package, central `GenerateDocumentationFile`), the other library
  features, the showcase feature, and the release feature.

## Implementation plan

Steps live in the temporary working folder `2026-11-02-core-2-0-api-auth/` (see its `summary.md`). Gates: `review` is
`pending` (it was `done (2026-11-02)` before the 2026-11-03 scope change); `premortem` is `pending` for step 01 only — the
core premortem verdict `go-with-mitigations (2026-11-02)` still covers steps 02–10 (user skip, 2026-11-02).

**Scope change 2026-11-03 (user: rename `SolTechnology.Core.Authentication` to `SolTechnology.Core.Auth` in 2.0, because
authorization joins the package later; confirmed under `CLAUDE.md §2` as `C-28`):** new step 01 renames the project,
test project, assembly, namespace, NuGet ID, `AddSolAuthentication` → `AddSolAuth`, and `AuthenticationConfiguration` →
`AuthConfiguration` (the user asked the plan to carry the configuration rename inside `C-28`), and moves the package
icon. It runs first so later steps edit the new paths; former steps 01–10 became 02–11, and their paths, commands, and
acceptance criteria now name `src/SolTechnology.Core.Auth` and `tests/SolTechnology.Core.Auth.Tests`. The user skipped
review and premortem for the rename (2026-11-03); no step 00 was added. Alternatives considered for the rename
mechanics: (a) a clean rename with no forwarding package (chosen; 2.0 is already MAJOR, the old ID stays at 1.x and is
deprecated on nuget.org in release 05 with the alternative `SolTechnology.Core.Auth`); (b) keep shipping a 2.0
`SolTechnology.Core.Authentication` that depends on `SolTechnology.Core.Auth` with `[Obsolete]` forwarders (rejected: a
second package to maintain for a rename the release notes cover, and the precedent `BlobStorage` → `Blob` used (a)).

Plan review (2026-11-02): the regrouping was checked against the pre-regroup plan (`dddac21c`) and
found complete. User answers recorded, not re-asked: the delivery order with AUID before Tale is
confirmed (Tale references AUID); the per-library split, the premortem reuse, and the `C-01`…`C-24`,
`C-26`, `C-27` confirmations stand (`C-25` rejected). Review fixes applied here (step numbers as of 2026-11-02, before the 2026-11-03 renumbering): `In scope` names the HTTP exception edit of step 02; step 07 owns the `docs/Api.md:387-389` health-check names that only the former step 61 `docs/*.md` grep covered; test paths use the tracked `API` casing; the retrospective's DreamTravel test command is runnable.

Notation: an unqualified step number is a step of this feature; `core NN` names step `NN` of
[`2026-11-02-core-2-0-flagship-hardening`](2026-11-02-core-2-0-flagship-hardening.md); other short names (`result`, `logging`, `cqrs`, `auid`, `tale`, `http`, `api-auth`,
`cache`, `sql`, `blob`, `messagebus`, `hangfire`, `testing-kits`) name the library features listed
in the umbrella brief; `release NN` names a step of
[`2026-11-02-org-transfer-and-2-0-release`](2026-11-02-org-transfer-and-2-0-release.md).

| Step | Title | Former core step(s) |
|---|---|---|
| 00 | Run premortem (gate) — step 01 rename | — (new 2026-11-03) |
| 01 | Auth — rename SolTechnology.Core.Authentication to SolTechnology.Core.Auth | — (new 2026-11-03, `C-28`) |
| 02 | DI resolution test — Api and Auth | 10 |
| 03 | Keep upstream URIs and query strings out of `ProblemDetails` and exception messages | 06 |
| 04 | Auth — constant-time API-key comparison and per-instance logger | 07 |
| 05 | Api — keep 201/202 in `ResultConversionFilter`; real filter registration | 22 |
| 06 | Auth and Api — public surface cleanup | 52 |
| 07 | XML docs shipped — API, Auth | 56 |
| 08 | Module docs — Api and Auth | 59 |
| 09 | Guide conformance — Api and Auth | 65, 66 |
| 10 | Log templates — `[{Placeholder}]` in Api (L-03) | 70 |
| 11 | Retrospective | 71 |

Steps run in number order; each is one PR.

## Acceptance criteria

- [ ] Every step's acceptance criteria are checked, or the gap is recorded under `Deviations`.
- [ ] The `S4` regression test fails on `4097d47f` and passes.
- [ ] `S5`: `CryptographicOperations.FixedTimeEquals` is present in the handler, no string equality compares the key, and the handler unit-test matrix (step 04) is green.
- [ ] `C-21`, `C-22`, and `C-28` shipped.
- [ ] `C-28`: `dotnet pack SolTechnology.Core.slnx -c Release -o /tmp/pack` produces `SolTechnology.Core.Auth.2.0.0.nupkg` and no `SolTechnology.Core.Authentication.*.nupkg`; the Auth package contains `docs/icon.png` and `buildTransitive/SolTechnology.Core.Auth.targets`, and `SOLCORE001` fires for a graph that mixes it with `SolTechnology.Core.Authentication` 1.x (step 01).
- [ ] `git grep -n "SolTechnology.Core.Authentication" -- ':!docs/features' ':!nuget-stats.json'` prints only the intended historical mentions in `dontreadme.md`, `docs/Auth.md` (migration note), and the `<Description>` of `src/SolTechnology.Core.Auth/SolTechnology.Core.Auth.csproj`.
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green, and `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.Auth.Tests` are green.
- [ ] The module resolution test uses `ValidateOnBuild = true` and `ValidateScopes = true`, and no resolution test is `[Ignore]`d.
- [ ] `find src/SolTechnology.Core.API src/SolTechnology.Core.Auth -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] Every packed `.nupkg` of this feature contains `lib/net10.0/*.xml`.
- [ ] Every C# snippet in `docs/Api.md`, `docs/Auth.md` compiles in a throwaway console project against the 2.0 API, as recorded in the module-docs step.
- [ ] The library-scoped `[{Placeholder}]` template check of the conformance or log-template step prints nothing.

## Completion summary

## Deviations

## Follow-ups
