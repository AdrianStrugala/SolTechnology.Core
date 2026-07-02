---
name: premortem
description: Imagine the change has shipped and broken production; work backward through SolTechnology.Core module-specific failure modes to identify causes, blast radius, and mitigations before merge.
---

# Premortem

Pre-mortem methodology adapted for **SolTechnology.Core** — a collection of NuGet packages
consumed by external apps (e.g. [DreamTravel](../../../sample-tale-code-apps/DreamTravel/),
[TaleCode](../../../sample-tale-code-apps/TaleCode/), [Elsa](../../../sample-tale-code-apps/Elsa/),
plus public consumers of `SolTechnology.Core.*` from NuGet.org).

Unlike a post-mortem (analysing failure after it happens), a pre-mortem **imagines failure before it
happens**, then works backward to identify causes and mitigations.

## When to use

**Mandatory** before merging any change that touches:

- A public/protected symbol in `src/SolTechnology.Core.*` (NuGet API surface)
- Any `ModuleInstaller.cs` (DI registration contract)
- `Directory.Build.props` / `src/Directory.Build.props` (build / package metadata)
- A persisted contract (Story state shape, message contract, SQL migration)

**Recommended** for any change that crosses module boundaries (e.g. CQRS ↔ Story ↔ Logging).

## Critical rules

- **No probability percentages.** Use plausibility + impact, not maths.
- **Assume the change shipped as planned.** A failure is not "we forgot to implement X" — it is
  "we shipped X exactly as designed and it still broke something".
- **Cite evidence.** Every scenario must reference a file path (and line where useful) — no
  speculation without code to back it.
- **Think second-order.** Consumers of the NuGet package (sample apps + public users) are part of
  the blast radius; the bug does not stop at the repo boundary.
- **Pair with [blue-red-team](../blue-red-team/SKILL.md)** when the change is a design decision,
  not just a code edit.
- **Cite Coding Guide sections by number** (§3, §11, §18) — `ClaudeCodingGuide.md` sections are
  stable; if a reference looks wrong, the *reference* is stale, not the section.

## Process

### 1. Frame the change

Record:

- **Modules touched** (e.g. `SolTechnology.Core.CQRS`, `SolTechnology.Core.Story`)
- **API delta** — added / removed / changed public types, members, signatures, attributes
- **Semver impact** — `MAJOR` (breaking) / `MINOR` (additive) / `PATCH` (internal only). Tie the
  classification to concrete diff evidence.
- **Consumers in the workspace** that compile against the touched modules
  (search `sample-tale-code-apps/**` for `using SolTechnology.Core.<Module>`).
- **External consumers** — assume public NuGet downloaders exist; their code is invisible to us.

### 2. Imagine the failure

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

### 4. Score blast radius, severity, likelihood

For each scenario:

- **Blast radius** — internal-only / sample apps / public NuGet consumers / all of the above.
- **Severity** — `L` (cosmetic, workaround obvious) / `M` (functional regression, recoverable) /
  `H` (data loss, breaking API, broken build for consumers).
- **Likelihood** — `L` / `M` / `H` based on how easy it is to hit the trigger in normal use.

No numeric probabilities.

### 5. Map to existing controls and propose mitigations

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

### 6. Module failure-mode checklists

Lazy-loaded — read **only** the references that match the modules in the diff. Each file lists
failure modes specific to that module; every bullet is worth one sentence of evidence before
dismissing it.

| Module(s) in diff | Reference file |
|---|---|
| `SolTechnology.Core.CQRS` | [`references/cqrs.md`](references/cqrs.md) |
| `SolTechnology.Core.Tale` | [`references/story.md`](references/story.md) |
| `SolTechnology.Core.Logging` | [`references/logging.md`](references/logging.md) |
| `SolTechnology.Core.HTTP`, `SolTechnology.Core.ApiClient` | [`references/http.md`](references/http.md) |
| `SolTechnology.Core.MessageBus` | [`references/messagebus.md`](references/messagebus.md) |
| `SolTechnology.Core.SQL` | [`references/sql.md`](references/sql.md) |
| `SolTechnology.Core.Blob` | [`references/blob.md`](references/blob.md) |
| `SolTechnology.Core.Cache` | [`references/cache.md`](references/cache.md) |
| Any `ModuleInstaller.cs` change | [`references/di.md`](references/di.md) |
| `Directory.Build.props`, `src/Directory.Build.props`, TFM bump | [`references/build-and-nuget.md`](references/build-and-nuget.md) |

If the diff touches a module without a reference file, write one (same shape as the existing
files) in the **same** PR so the next contributor inherits the checklist.

### 7. Decide

Produce one of:

- **Go** — no `H` severity, all `M` covered by existing controls, semver classification matches diff.
- **Go with mitigations** — list the required mitigations in the output; the change ships only
  after each is in place.
- **No-go** — at least one `H` severity scenario with no plausible mitigation; needs redesign or
  an ADR documenting the breaking change.

## Output format

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

