---
name: premortem
description: Imagine the change has shipped and broken production; work backward through SolTechnology.Core module-specific failure modes to identify causes, blast radius, and mitigations before merge.
user-invocable: true
---

# Premortem

Pre-mortem methodology adapted for **SolTechnology.Core** — a collection of NuGet packages
consumed by external apps (e.g. [DreamTravel](../../../sample-tale-code-apps/DreamTravel/),
[TaleCode](../../../sample-tale-code-apps/TaleCode/), [Elsa](../../../sample-tale-code-apps/Elsa/),
plus public consumers of `SolTechnology.Core.*` from NuGet.org).

Unlike a post-mortem (analysing failure after it happens), a pre-mortem **imagines failure before it
happens**, then works backward to identify causes and mitigations.

## When to Run

**Mandatory** before merging any change that touches:

- A public/protected symbol in `src/SolTechnology.Core.*` (NuGet API surface)
- Any `ModuleInstaller.cs` (DI registration contract)
- `Directory.Build.props` / `src/Directory.Build.props` (build / package metadata)
- A persisted contract (Story state shape, message contract, SQL migration)

**Recommended** for any change that crosses module boundaries (e.g. CQRS ↔ Story ↔ Logging).

## Critical Rules

- **No probability percentages.** Use plausibility + impact, not maths.
- **Assume the change shipped as planned.** A failure is not "we forgot to implement X" — it is
  "we shipped X exactly as designed and it still broke something".
- **Cite evidence.** Every scenario must reference a file path (and line where useful) — no
  speculation without code to back it.
- **Think second-order.** Consumers of the NuGet package (sample apps + public users) are part of
  the blast radius; the bug does not stop at the repo boundary.
- **Pair with [blue-red-team](../blue-red-team/SKILL.md)** when the change is a design decision,
  not just a code edit.

## Process

### 1. Frame the Change

Record:

- **Modules touched** (e.g. `SolTechnology.Core.CQRS`, `SolTechnology.Core.Story`)
- **API delta** — added / removed / changed public types, members, signatures, attributes
- **Semver impact** — `MAJOR` (breaking) / `MINOR` (additive) / `PATCH` (internal only). Tie the
  classification to concrete diff evidence.
- **Consumers in the workspace** that compile against the touched modules
  (search `sample-tale-code-apps/**` for `using SolTechnology.Core.<Module>`).
- **External consumers** — assume public NuGet downloaders exist; their code is invisible to us.

### 2. Imagine the Failure

Pick a moment in the future (next release, next quarter). **The change has failed spectacularly.**
Briefly describe the worst-credible end state in one paragraph before enumerating scenarios.
Examples of end states:

- "DreamTravel API returns 500 on every request after upgrading `SolTechnology.Core.Logging`."
- "A community consumer files an issue: their `dotnet build` now fails with NU1605 after we
  bumped a transitive `Microsoft.Extensions.*` reference."
- "Story workflows resumed after deploy crash on deserialization."

### 3. Enumerate Scenarios (Work Backward)

Generate **5–10 concrete scenarios**. For each, answer:

- What went wrong (one sentence)?
- What was the trigger (file:line in the diff)?
- What warning signs were missed?
- What assumption proved incorrect?

Use the module checklists in §6 to seed scenarios — do not rely on memory alone. Read each relevant
checklist before listing scenarios for that module.

### 4. Score Blast Radius, Severity, Likelihood

For each scenario:

- **Blast radius** — internal-only / sample apps / public NuGet consumers / all of the above.
- **Severity** — `L` (cosmetic, workaround obvious) / `M` (functional regression, recoverable) /
  `H` (data loss, breaking API, broken build for consumers).
- **Likelihood** — `L` / `M` / `H` based on how easy it is to hit the trigger in normal use.

No numeric probabilities.

### 5. Map to Existing Controls and Propose Mitigations

For each scenario:

- **Existing control** — test in `tests/SolTechnology.Core.<Module>.Tests/`, guard, ADR, doc, lint,
  build setting. If none exists, write `none — gap`.
- **Mitigation** — one of:
  - Add / extend a test (link the test class).
  - Add a `Guards.*` precondition.
  - Document a breaking change in `docs/adr/` + bump major.
  - Add a runtime check (`ModuleInstaller` validation).
  - Update [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) so the failure mode is
    prevented for future contributors.
  - Defer with explicit rationale ("accepted risk").

### 6. Module Failure-Mode Checklists

Read only the checklists for modules in the diff. Each bullet is a failure mode worth a sentence
of evidence before dismissing.

#### CQRS — `src/SolTechnology.Core.CQRS/`

- Handler not registered in `ModuleInstaller` → MediatR throws at first request, not at startup.
- Result pattern bypassed (throws instead of returning `Result.Failure(...)`) — see
  [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) §3.
- Chain handler ordering changed → silent behaviour drift (no compile-time guard).
- `IRequest<Result<T>>` signature change → consumers' handlers no longer match registration.
- Cancellation token dropped on the boundary.

#### Story — `src/SolTechnology.Core.Story/`

- Persisted state schema delta without a migration path → workflows fail to resume after deploy.
- Step name/identity changed → in-flight workflows resume on the wrong step.
- Idempotency broken: replay of a step causes side effects twice.
- Interactive workflow waiting on user input deadlocks when the input channel changes.
- Serialization contract drift between worker and API host.

#### Logging — `src/SolTechnology.Core.Logging/`

- `logger.Log*` contract change (template, level, scope) violates
  [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) §11.
- Structured field renamed → downstream queries / dashboards silently empty.
- PII leak via new field, exception message, or scope value.
- Performance regression (boxing, string concat) on hot path.
- Shared-framework reference change (`Microsoft.Extensions.Logging`) triggers
  [NU1510](../../../src/Directory.Build.props) noise for pure-NuGet consumers.

#### HTTP / ApiClient — `src/SolTechnology.Core.HTTP/`, `src/SolTechnology.Core.ApiClient/`

- Default timeout / retry policy change → upstream service marked unhealthy.
- Response contract change (added required member, renamed) → consumer deserialization fails.
- `HttpClient` registration leak (transient instead of typed/factory).
- TLS / handler behaviour change on .NET 10 surface.

#### MessageBus — `src/SolTechnology.Core.MessageBus/`

- Message contract drift between producer and consumer versions.
- Poison message handling change → dead-letter loop or silent drop.
- Lock duration / max delivery count change → duplicate processing.
- Topic / subscription naming change → silent missed messages.

#### Sql — `src/SolTechnology.Core.Sql/`

- Migration ordering / missing migration → schema drift between envs.
- Mixed Dapper + EF in one transaction → inconsistent state on failure.
- Connection / transaction leak under exception path.
- Parameter binding change → SQL injection regression.

#### BlobStorage — `src/SolTechnology.Core.BlobStorage/`

- SAS / connection string handling change → permissions either too narrow or too broad.
- ETag / optimistic concurrency removed → last-write-wins regression.
- Streaming vs buffered upload change → memory spike.

#### Cache — `src/SolTechnology.Core.Cache/`

- TTL semantics change (sliding ↔ absolute) → stale data served.
- Key format change → silent cache miss (perf regression, not correctness).
- Cache stampede on cold start with new eviction policy.

#### DI / ModuleInstaller — every module

- Registration removed or renamed → null reference at runtime, build green.
- Lifetime change (singleton ↔ scoped) → captive dependency or state bleed.
- Decorator order change → behaviour drift with no test coverage.

#### Build & NuGet — `Directory.Build.props`, `src/Directory.Build.props`

- `TreatWarningsAsErrors=true` → a new analyzer warning fails consumer builds. Confirm with
  [src/Directory.Build.props](../../../src/Directory.Build.props).
- Demoted warnings (`NU1900`, `NU1510`) hide a real regression.
- Transitive `Microsoft.Extensions.*` 10.0.1 bumped → downstream apps still on `net9.0` get
  NU1605 / runtime mismatch.
- `snupkg` symbol package missing → debugging broken for consumers.
- Author / RepositoryUrl / license metadata regressed.

#### .NET 10 target

- New language / runtime feature used in a public signature → consumers on older TFM cannot
  reference the package.
- Nullable annotations changed on a public surface → consumer warnings turn into errors.

### 7. Decide

Produce one of:

- **Go** — no `H` severity, all `M` covered by existing controls, semver classification matches diff.
- **Go with mitigations** — list the required mitigations in the output; the change ships only
  after each is in place.
- **No-go** — at least one `H` severity scenario with no plausible mitigation; needs redesign or
  an ADR documenting the breaking change.

## Standard Output Format

### Premortem — `<change title>`

#### Frame

- **Modules touched**: <placeholder>list</placeholder>
- **API delta**: <placeholder>added/removed/changed</placeholder>
- **Semver impact**: <placeholder>MAJOR / MINOR / PATCH</placeholder>
- **Consumers in workspace**: <placeholder>list of sample apps / files</placeholder>

#### Imagined Failure

<placeholder>One-paragraph worst-credible end state.</placeholder>

#### Scenarios

| # | Scenario | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation |
|---|---|---|---|---|---|---|---|
| 1 | <placeholder> | <placeholder> | <placeholder> | L/M/H | L/M/H | <placeholder> | <placeholder> |

#### Top 3 Risks

1. <placeholder>scenario # + one-line why it tops the list</placeholder>
2. <placeholder></placeholder>
3. <placeholder></placeholder>

#### Required Mitigations Before Merge

- <placeholder>concrete action with file path / test name</placeholder>

#### Accepted Risks

- <placeholder>scenario # + rationale</placeholder>

#### Decision

**Go / Go with mitigations / No-go** — <placeholder>one-line reason</placeholder>

