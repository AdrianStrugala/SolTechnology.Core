# CLAUDE.md — Operational protocol for AI agents in this repository

This file tells you (Claude Code / Copilot / any agent) **how to behave** in this repo.
Code conventions live in [`docs/ClaudeCodingGuide.md`](docs/ClaudeCodingGuide.md). Module
docs in [`docs/`](docs/ClaudeCodingGuide.md). Architectural decisions in [`docs/adr/`](docs/adr/README.md). One source
of truth per topic — when in doubt, link, don't copy.

---

## §0. Pre-flight — before the first code-writing tool call in a session

1. **MUST** open [`docs/ClaudeCodingGuide.md`](docs/ClaudeCodingGuide.md) and read the
   section(s) relevant to the change (e.g. §11 for `logger.Log*`, §3 for handlers,
   §10 for renames, §4 for Stories).
2. **MUST** state in your reply which sections you consulted and the concrete rules
   you will follow — one sentence. Purpose: defeat the failure mode *"I assumed I
   knew the convention"*.
3. **MUST** read the `SKILL.md` of any skill before invoking it. Skills are not
   pre-loaded; their instructions live only inside the file. Never infer from name.

Applies even when the user's request looks narrow. Logging / naming / structure
rules cross-cut every change.

---

## §1. Forbidden actions — ask, don't act

Never perform any of these without explicit user confirmation in the current turn:

- Rename, move, or delete a **public/protected** symbol in `src/SolTechnology.Core.*`.
- Bump the major version, `<AssemblyVersion>`, or `<PackageVersion>` of any package.
- Edit a published ADR (`docs/adr/*.md`) other than appending a *Supersession* / *Amendment* note.
- Push to `master`, force-push, or rewrite shared history.
- Add a `PackageReference` override that masks a CVE without fixing at source (see §5).
- Disable `TreatWarningsAsErrors`, `Nullable`, or analyser rules in `Directory.Build.props`.
- Introduce a new top-level folder under `src/`, `tests/`, or `docs/`.
- Add a new external NuGet dependency to a package under `src/SolTechnology.Core.*`
  without checking it against [`nuget-stats.json`](nuget-stats.json) and reporting
  the impact.

For each of these, surface the intended change and the reasoning, then wait.

---

## §2. Tool protocol

| Action | Rule |
|---|---|
| After every code edit | Call `get_errors` on the edited file. Fix errors caused by your change; leave pre-existing warnings alone (state which is which). |
| After a logical task | Build the relevant solution (`dotnet build SolTechnology.Core.slnx`; for DreamTravel: `cd sample-tale-code-apps/DreamTravel && dotnet build`). |
| After a build-relevant change | Run the affected tests (`./.github/runTests.ps1` for core; `dotnet test <project>` for a single target). |
| Before asking for Bash permission | Check `.claude/settings.local.json` `allow` list for a matching wildcard. Reuse existing patterns (`-Last 10/20/30`, `timeout 60/120/180/300`) instead of asking for a new one. |
| When invoking a custom agent / skill | Read its instructions first. Do not paraphrase its task; pass the user's intent verbatim where possible. |
| When a specific skill / agent is mandated and unavailable | STOP. Tell the user which skill / agent is required and why the requested work is gated on it. Do **not** produce a freehand substitute (no hand-drafted diagram, no inline mini-plan, no improvised review checklist). |
| When adding a sequence or component diagram | Use the [`diagram`](.github/agents/diagram.agent.md) agent. Output Mermaid only, five canonical layer boxes, immutable file per version. Never hand-draft a diagram inline in a doc / ADR / review. |
| When edits don't persist | Stop. Report it. Do **not** retry blindly — IDE buffers can overwrite tool-applied changes; ask the user to close the file in their IDE. |

---

## §3. Agents and Skills

Two libraries of AI tooling live in `.github/`. **Agents** ([`.github/agents/`](.github/agents/README.md))
own multi-step workflows. **Skills** ([`.github/skills/`](.github/skills/README.md)) are narrow procedures
loaded on demand. Always `read_file` the relevant file before invoking — descriptions in these
indexes are routing hints, not contracts.

### Agents

| Agent | Path | Invoke when |
|---|---|---|
| implementation-planning | [`.github/agents/implementation-planning.agent.md`](.github/agents/implementation-planning.agent.md) | Planning a multi-module or breaking change; produces an ADR + step files under `docs/adr/<NNN>-<feature>/to-do/` per [ADR-006](docs/adr/006-implementation-plan-workflow.md). |
| plan-reviewer | [`.github/agents/plan-reviewer.agent.md`](.github/agents/plan-reviewer.agent.md) | Critiquing a plan in `docs/adr/<NNN>-<feature>/to-do/` before implementation. Writes revised drafts to `reviewed/`, deletes originals from `to-do/`. Never writes production code. |
| diagram | [`.github/agents/diagram.agent.md`](.github/agents/diagram.agent.md) | Authoring a sequence or component diagram under `docs/diagrams/`. Mermaid only, five canonical layer boxes (`Presentation` / `Logic` / `Data` / `Domain` / `External`), immutable file per version. **Required** for every sequence or component diagram added under `docs/`. |

### Skills

| Skill | Path | Invoke when |
|---|---|---|
| roast-me | [`.github/skills/roast-me/SKILL.md`](.github/skills/roast-me/SKILL.md) | Vague request, under-specified intent, before any non-trivial planning. One question per turn with a running ledger. |
| premortem | [`.github/skills/premortem/SKILL.md`](.github/skills/premortem/SKILL.md) | **Mandatory** before merging changes to public NuGet API, `ModuleInstaller.cs`, persisted contracts, or `Directory.Build.props`. |
| blue-red-team | [`.github/skills/blue-red-team/SKILL.md`](.github/skills/blue-red-team/SKILL.md) | Design-level decision / ADR seeding. |
| code-review | [`.github/skills/code-review/SKILL.md`](.github/skills/code-review/SKILL.md) | Reviewing a diff against the Coding Guide and module review templates. |
| commit-message | [`.github/skills/commit-message/SKILL.md`](.github/skills/commit-message/SKILL.md) | Producing a Conventional Commits message with semver footer. |
| documentation-cleanup | [`.github/skills/documentation-cleanup/SKILL.md`](.github/skills/documentation-cleanup/SKILL.md) | Validating docs integrity (module/doc parity, indexes, Mermaid, ADRs). |
| package-management | [`.github/skills/package-management/SKILL.md`](.github/skills/package-management/SKILL.md) | Adding / bumping a `PackageReference` — looks up the canonical version, prevents drift and version-by-memory hallucination. |
| dependency-audit | [`.github/skills/dependency-audit/SKILL.md`](.github/skills/dependency-audit/SKILL.md) | Resolving `NU1901`–`NU1904` CVE warnings or `NU1605` downgrades. Drives the parent-lookup → fix-at-source → override-only-as-last-resort flow from §5. |
| test-writing | [`.github/skills/test-writing/SKILL.md`](.github/skills/test-writing/SKILL.md) | Authoring or extending tests under `tests/` (NUnit) or sample apps (NUnit for DreamTravel). Encodes the FluentAssertions + NSubstitute + AutoFixture stack and the `// Arrange` / `// Act` / `// Assert` convention from `ClaudeCodingGuide.md` §8. |
| command-query-event-story | [`.github/skills/command-query-event-story/SKILL.md`](.github/skills/command-query-event-story/SKILL.md) | Authoring a command, query, fire-and-forget event, or Story (chapters) in any app built on the `SolTechnology.Core.CQRS` / `.Story` NuGet packages — per `ClaudeCodingGuide.md` §0/§3/§4/§11 and the DreamTravel reference app. Covers Stories hosted in `Commands`/`Queries`, domain-model Stories in `DomainServices`, persisted interactive `Workflows`, and `[LogScope]` logging. Routes tests to `test-writing`, review to `code-review`, cleanup to `refactor`. |
| refactor | [`.github/skills/refactor/SKILL.md`](.github/skills/refactor/SKILL.md) | Behaviour-preserving cleanup inside a single module — rename internals, split a class above the §9 size budget, extract a primary constructor, remove `#region`, pay down a §15 anti-pattern. Routes to `implementation-planning` if scope grows past one module or touches a public symbol. |
| implement-plan | [`.github/skills/implement-plan/SKILL.md`](.github/skills/implement-plan/SKILL.md) | Executing one step from an ADR's `to-do/` or `reviewed/` folder. Moves the file to `done/`, updates the summary, optionally records deviations. |

### Premortem gate

Any change that touches a public/protected symbol in `src/SolTechnology.Core.*`, a
`ModuleInstaller.cs`, `Directory.Build.props`, or a persisted contract **must** be
gated by a premortem. Attach the skill's output to the PR. Block on *Go* /
*Go with mitigations* with mitigations in place. Rationale:
[ADR-004](docs/adr/004-ai-agents-and-skills.md).

---

## §4. Quality bar for every finding / report

- **Evidence-based.** Cite file paths and line numbers. No "somewhere in the codebase".
- **Risk-aware.** Consider the impact on NuGet consumers, not just the local diff.
- **Systematic.** Follow the skill's documented process; do not improvise the steps.
- **Factual.** Report what you are changing; let the reader judge correctness.
- **Doc-first.** Search [`docs/`](docs/ClaudeCodingGuide.md) — especially `ClaudeCodingGuide.md`, `adr/`,
  `reviews/` — before analysing code.

Markdown / Mermaid hygiene:

- Links with spaces in the path: `[Text](<path/file.md>)`.
- Verify every link resolves on disk before printing it.
- Mermaid node labels with spaces use `<br>`: `Node[Name<br>With<br>Spaces]`.
- No issue-tracker IDs (Jira, etc.) unless the user supplies them.

---

## §5. Dependency management — fix at source, never mask

The full procedure (parent lookup, source fix, override-as-last-resort, verification, commit
hygiene) lives in the [`dependency-audit`](.github/skills/dependency-audit/SKILL.md) skill —
invoke it whenever an `NU190x` or `NU1605` warning appears. Summary of the rules:

When a CVE warning (`NU1901`–`NU1904`) appears:

1. **Identify root cause** — `dotnet list <project.csproj> package --include-transitive`
   to find which parent pulls the vulnerable dep.
2. **Fix at source** — update the parent (`dotnet add package <Parent> --version <new>`),
   remove the parent if unused (`dotnet remove package <Parent>`), or migrate to the
   modern SDK (e.g. `Microsoft.Azure.ServiceBus` → `Azure.Messaging.ServiceBus`).
3. **Override only as last resort** — direct `PackageReference` override in the project
   that *directly references the problematic parent*. Add it once at the seam; transitive
   children inherit through `ProjectReference`.
4. **Verify** — `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]"` must
   return empty.

`NU1900` is **not** a CVE; it means audit data could not be downloaded (unreachable
feed, auth required). It is demoted to warning in `src/Directory.Build.props` via
`<WarningsNotAsErrors>NU1900</WarningsNotAsErrors>`. Fix the feed locally; do **not**
touch `Directory.Build.props`.

❌ **BAD — masking by override at every leaf**
```xml
<!-- DreamTravel.Commands.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<!-- DreamTravel.Queries.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<!-- DreamTravel.Infrastructure.csproj -->
<PackageReference Include="Hangfire.Core" Version="1.8.16" />   <!-- old, vulnerable -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

✅ **GOOD — bump the parent at the seam**
```xml
<!-- DreamTravel.Infrastructure.csproj -->
<PackageReference Include="Hangfire.Core" Version="1.8.22" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />  <!-- only if still needed -->
```

---

## §6. Repo facts (essentials only)

- **Solution:** `SolTechnology.Core.slnx` (XML format) at repo root.
- **Target framework:** `net10.0` (`global.json` enforces SDK 10.0.100+).
- **Shell:** repo runs on Windows. Pipeline scripts use **PowerShell** syntax
  (`Select-String`, `Select-Object`). On macOS/Linux dev boxes use bash equivalents.
- **Build hierarchy:** `Directory.Build.props` is three-tier (root → `src/` → project).
- **Test runner (core):** `./.github/runTests.ps1` walks every project in `tests/`.
- **`TreatWarningsAsErrors`:** enabled. `Nullable`: enabled. Implicit usings: enabled.
- **Pipelines:** `.github/workflows/publishPackages.yml` (core NuGet), Azure DevOps
  pipelines in `sample-tale-code-apps/DreamTravel/devOps/pipelines/` (DreamTravel).
  When bumping the SDK, update **all** pipeline files in lockstep — search for the
  current version string across the repo.

---

## §7. Cross-references — where to find what

| Topic | Source of truth |
|---|---|
| Project / folder layout, layer dependencies | `docs/ClaudeCodingGuide.md` §1 |
| `ModuleInstaller` pattern | `docs/ClaudeCodingGuide.md` §2 |
| CQRS (commands, queries, validators) | `docs/ClaudeCodingGuide.md` §3 |
| Story Framework (chapters, contexts, persistence) | `docs/ClaudeCodingGuide.md` §4 |
| DataLayer (SQL, HTTP clients, repositories) | `docs/ClaudeCodingGuide.md` §5 |
| Domain layer rules | `docs/ClaudeCodingGuide.md` §6 |
| API / `Program.cs` / controllers | `docs/ClaudeCodingGuide.md` §7 |
| Tests (Unit / Component / E2E split, density) | `docs/ClaudeCodingGuide.md` §8 |
| Class-level rules (size, primary ctor, braces, comments, `#region`) | `docs/ClaudeCodingGuide.md` §9 |
| Naming, acronyms (ALL CAPS) | `docs/ClaudeCodingGuide.md` §10 |
| Logging (placeholders, `[]` brackets, levels) | `docs/ClaudeCodingGuide.md` §11 |
| Validation (FluentValidation, Guards) | `docs/ClaudeCodingGuide.md` §12 |
| Error handling (`Result`, `try/catch` rules) | `docs/ClaudeCodingGuide.md` §13 |
| Configuration binding | `docs/ClaudeCodingGuide.md` §14 |
| Anti-patterns observed in the codebase | `docs/ClaudeCodingGuide.md` §15 |
| Public module documentation structure | `docs/ClaudeCodingGuide.md` §18 |
| Per-module user docs | `docs/<Module>.md` (e.g. `docs/Api.md`, `docs/Log.md`) |
| Per-module review templates | `docs/reviews/<Module>-Review.md` |
| HTTP production rollout | `docs/HTTP-Production-Checklist.md` + [ADR-005](docs/adr/005-http-production-defaults.md) |
| AI agents / skills rationale | [ADR-004](docs/adr/004-ai-agents-and-skills.md) |
| ADR index + status tracker | [`docs/adr/README.md`](docs/adr/README.md) |
| Feature backlog index (non-decision plans) | [`docs/features/README.md`](docs/features/README.md) |
| Multi-step implementation plan layout (`to-do/` / `reviewed/` / `done/`) | [ADR-006](docs/adr/006-implementation-plan-workflow.md) |

If a rule appears here **and** in the guide, the guide is authoritative — this file
intentionally does not duplicate convention text.

---

## §8. Self-improvement protocol

If you learn something new during a task, **update your own instructions in the same
turn before yielding**. Silent retention is forbidden.

Triggers:

- The user corrects you on a convention, naming, structure, or workflow.
- You discover a non-obvious codebase constraint (build quirk, framework rule, DI pitfall).
- A repeated mistake gets called out.
- A new pattern, helper, or framework addition becomes "the way".
- An ADR is written or amended.

Routing:

1. Convention / coding rule → `docs/ClaudeCodingGuide.md` (find the relevant §, append
   an imperative bullet; new section only if none fits).
2. Repo-wide operational / tool / protocol rule → this file (`CLAUDE.md`).
3. Skill-specific lesson → the skill's `SKILL.md`.
4. User-facing docs lesson → `docs/ClaudeCodingGuide.md §18` ("Public module documentation").

In the same reply, mention the update in one sentence: *"Added rule X to §N of <file>."*

---

## §9. Pre-yield checklist

Before declaring a task done:

- [ ] Read the relevant `ClaudeCodingGuide` section(s); cited them in the reply (§0).
- [ ] No forbidden action taken without confirmation (§1).
- [ ] `get_errors` clean (or remaining warnings explicitly noted as pre-existing) (§2).
- [ ] `dotnet build SolTechnology.Core.slnx` green; for DreamTravel changes, sample app build green (§2).
- [ ] Relevant tests green (§2).
- [ ] No new `NU1901`–`NU1904` warnings (§5).
- [ ] Logging follows `ClaudeCodingGuide §11` — every placeholder wrapped in `[]`.
- [ ] No `#region`, no placeholder strings, no swallowed exceptions, no `try/catch` in controllers/handlers.
- [ ] Comments are one-line *why-not-what* (`ClaudeCodingGuide §9.11`).
- [ ] Public types have XML `<summary>` (English).
- [ ] Lessons learned written down per §8.

If any item fails, fix it before yielding.

---

## §10. Common gotchas (repo-operational only)

| Symptom | Cause / fix |
|---|---|
| `RegisterCommands` / `RegisterQueries` / `RegisterStories` find nothing | Called from the wrong assembly. They use `Assembly.GetCallingAssembly()` — invoke from inside the assembly that owns the handlers. |
| `dotnet test` "no tests discovered" | Tests live in `tests/` (outside `src/`). Path: `tests/<Project>.Tests`. |
| AUID JSON round-trip drops the value | The consumer is using `PackageReference` to `SolTechnology.Core.AUID` instead of `ProjectReference` in the sample; `AuidJsonConverter` is missing. |
| Story JSON deserialisation case-sensitive issues | Use `StoryJsonOptions.Default` (`PropertyNameCaseInsensitive = true`, `IncludeFields = true`). |
| Interactive story fails immediately | Ensure `RegisterStories()` is called — it wires `StoryManager` + in-memory persistence by default. For durable persistence, use `.UseStoryRepository<T>()` (see `DreamTravel.SQLite` sample). |
| Workload missing on CI | Run `dotnet workload restore SolTechnology.Core.slnx` before `restore` / `build`. See `.github/workflows/publishPackages.yml`. |
| Edits applied via tool don't show in `git diff` | An IDE buffer is overwriting the file. Ask the user to close the file in their IDE, then re-apply. |

# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.
