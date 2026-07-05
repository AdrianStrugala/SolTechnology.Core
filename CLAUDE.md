# CLAUDE.md — Operational protocol for AI agents in this repository

This file tells you (Claude Code / Copilot / any agent) **how to behave** in this repo.
Code conventions live in [`docs/ClaudeCodingGuide.md`](docs/ClaudeCodingGuide.md).
AI-doc authoring rules live in [`docs/AIDocsGuide.md`](docs/AIDocsGuide.md).
Module docs in [`docs/`](docs/ClaudeCodingGuide.md). Architectural decisions in
[`docs/adr/`](docs/adr/README.md). One source of truth per topic — when in doubt,
link, don't copy.

---

## §0. Pre-flight — before the first code-writing tool call in a session

1. **MUST** open [`docs/ClaudeCodingGuide.md`](docs/ClaudeCodingGuide.md) and read the
   section(s) relevant to the change (e.g. §11 for `logger.Log*`, §3 for handlers,
   §10 for renames, §4 for Tales).
2. **MUST** state in your reply which sections you consulted and the concrete rules
   you will follow — one sentence. Purpose: defeat the failure mode *"I assumed I
   knew the convention"*. This is the single canonical statement of this rule;
   other docs link here.
3. **MUST** read the `SKILL.md` / `.agent.md` of any skill or agent before invoking
   it. They are not pre-loaded; their instructions live only inside the file.
   NEVER infer behaviour from the name. This is the single canonical statement of
   this rule; other docs link here.

Applies even when the user's request looks narrow. Logging / naming / structure
rules cross-cut every change.

---

## §1. Behavioral core

Highest-priority rules. They govern every task, before any convention applies.

1. **State assumptions before implementing.** If multiple interpretations exist,
   present them — NEVER pick one silently. If a simpler approach exists, say so.
   If something is unclear, stop, name what is confusing, ask.
2. **Simplicity first.** Minimum code that solves the problem. NEVER add features
   beyond what was asked, abstractions for single-use code, unrequested
   configurability, or error handling for impossible scenarios.
3. **Surgical changes.** Every changed line MUST trace directly to the user's
   request. Match existing style even if you would do it differently. Remove
   imports/variables/functions that **your** change orphaned; leave pre-existing
   dead code alone (report it, don't delete it).
   - **Sole exception — Guide §15 anti-patterns:** entries marked *fix on touch*
     MUST be fixed when they sit in code you already edit for the task (same
     method/class), in a **separate `chore:` commit**. Entries marked
     *report only* (they change an observable contract: log templates,
     serialization, public types) are NEVER auto-fixed — report them instead.
4. **Goal-driven execution.** Before starting, transform the task into verifiable
   success criteria ("fix the bug" → "a test reproduces it, then passes"). For
   multi-step tasks state a brief `step → verify` plan and loop until verified.

---

## §2. Forbidden actions — ask, don't act

NEVER perform any of these without explicit user confirmation in the current turn:

- Rename, move, or delete a **public/protected** symbol in `src/SolTechnology.Core.*`.
  (Confirmation is the only gate — a premortem is NOT required for symbol changes; see §4.)
- Bump the major version, `<AssemblyVersion>`, or `<PackageVersion>` of any package.
- Edit a published ADR (`docs/adr/*.md`) other than appending a *Supersession* / *Amendment* note.
- Push to `master`, force-push, or rewrite shared history.
- Add a `PackageReference` override that masks a CVE without fixing at source (see §6).
- Disable `TreatWarningsAsErrors`, `Nullable`, or analyser rules in `Directory.Build.props`.
- Introduce a new top-level folder under `src/`, `tests/`, or `docs/`.
- Add a new external NuGet dependency to a package under `src/SolTechnology.Core.*`
  without checking it against [`nuget-stats.json`](nuget-stats.json) and reporting
  the impact.

For each of these, surface the intended change and the reasoning, then wait.

---

## §3. Tool protocol

| Action | Rule |
|---|---|
| After every code edit | Call `get_errors` on the edited file. Fix errors caused by your change; leave pre-existing warnings alone (state which is which). |
| After the last code edit before yielding, and after any `.csproj` / `.props` change | Build the relevant solution (`dotnet build SolTechnology.Core.slnx`; for DreamTravel: `cd sample-tale-code-apps/DreamTravel && dotnet build`). |
| After a build-relevant change | Run the affected tests (`./.github/runTests.ps1` for core; `dotnet test <project>` for a single target). |
| Before asking for Bash permission | Check `.claude/settings.local.json` `allow` list for a matching wildcard. Reuse existing patterns (`-Last 10/20/30`, `timeout 60/120/180/300`) instead of asking for a new one. |
| When invoking a custom agent / skill | Read its file first (§0.3). Pass the user's intent verbatim where possible. |
| When a mandated skill / agent is unavailable | STOP. Tell the user which skill / agent is required and why the work is gated on it. NEVER produce a freehand substitute (no hand-drafted diagram, no inline mini-plan, no improvised review checklist). |
| When adding a sequence or component diagram | Use the `diagram` agent — see its row in §4. |
| When edits don't persist | Stop and report — see §11 (IDE buffer gotcha). NEVER retry blindly. |

---

## §4. Agents and Skills

Two libraries of AI tooling live in `.github/`. **Agents** ([`.github/agents/`](.github/agents/README.md))
own multi-step workflows. **Skills** ([`.github/skills/`](.github/skills/README.md)) are narrow
procedures loaded on demand. Descriptions in these tables are routing hints, not
contracts — read the file before invoking (§0.3).

### Agents

| Agent | Path | Invoke when |
|---|---|---|
| implementation-planning | [`.github/agents/implementation-planning.agent.md`](.github/agents/implementation-planning.agent.md) | Planning a multi-module or breaking change; produces an ADR + step files under `docs/adr/<NNN>-<feature>/to-do/` per [ADR-006](docs/adr/006-implementation-plan-workflow.md). |
| plan-reviewer | [`.github/agents/plan-reviewer.agent.md`](.github/agents/plan-reviewer.agent.md) | Critiquing a plan in `docs/adr/<NNN>-<feature>/to-do/` before implementation. Writes revised drafts to `reviewed/`, deletes originals from `to-do/`. NEVER writes production code. |
| diagram | [`.github/agents/diagram.agent.md`](.github/agents/diagram.agent.md) | **Required** for every sequence or component diagram added under `docs/`. Mermaid only, five canonical layer boxes (`Presentation` / `Logic` / `Data` / `Domain` / `External`), immutable file per version. NEVER hand-draft a diagram inline in a doc / ADR / review. |

### Skills

| Skill | Path | Invoke when |
|---|---|---|
| premortem | [`.github/skills/premortem/SKILL.md`](.github/skills/premortem/SKILL.md) | **Mandatory** for the changes listed in the premortem gate below. |
| blue-red-team | [`.github/skills/blue-red-team/SKILL.md`](.github/skills/blue-red-team/SKILL.md) | Design-level decision / ADR seeding. |
| code-review | [`.github/skills/code-review/SKILL.md`](.github/skills/code-review/SKILL.md) | Reviewing a diff against the Coding Guide and module review templates. |
| commit-message | [`.github/skills/commit-message/SKILL.md`](.github/skills/commit-message/SKILL.md) | Producing a Conventional Commits message with semver footer. |
| documentation-cleanup | [`.github/skills/documentation-cleanup/SKILL.md`](.github/skills/documentation-cleanup/SKILL.md) | Validating docs integrity (module/doc parity, indexes, Mermaid, ADRs). |
| package-management | [`.github/skills/package-management/SKILL.md`](.github/skills/package-management/SKILL.md) | Adding / bumping a `PackageReference` — looks up the canonical version, prevents drift and version-by-memory hallucination. |
| dependency-audit | [`.github/skills/dependency-audit/SKILL.md`](.github/skills/dependency-audit/SKILL.md) | Resolving `NU1901`–`NU1904` CVE warnings or `NU1605` downgrades. Owns the full procedure summarised in §6. |
| test-writing | [`.github/skills/test-writing/SKILL.md`](.github/skills/test-writing/SKILL.md) | Authoring or extending tests (NUnit everywhere). Encodes the FluentAssertions + NSubstitute + AutoFixture stack and Guide §8 conventions. |
| command-query-event-tale | [`.github/skills/command-query-event-tale/SKILL.md`](.github/skills/command-query-event-tale/SKILL.md) | Authoring a command, query, fire-and-forget event, or Tale in any app built on `SolTechnology.Core.CQRS` / `.Tale` — per Guide §0/§3/§4/§11. Details live in the skill. |
| refactor | [`.github/skills/refactor/SKILL.md`](.github/skills/refactor/SKILL.md) | Behaviour-preserving cleanup inside a single module (Guide §9 budgets, §15 debt). Routes to `implementation-planning` if scope grows past one module or touches a public symbol. |
| implement-plan | [`.github/skills/implement-plan/SKILL.md`](.github/skills/implement-plan/SKILL.md) | Executing one step from an ADR's `to-do/` or `reviewed/` folder. Moves the file to `done/`, updates the summary, optionally records deviations. |

### Premortem gate

A premortem is **mandatory** before merging a change that touches any of:

- a `ModuleInstaller.cs`,
- `Directory.Build.props`,
- a persisted contract.

Attach the skill's output to the PR. Block on *Go* / *Go with mitigations* with
mitigations in place. Public/protected symbol changes are NOT premortem-gated —
they require user confirmation only (§2). Rationale:
[ADR-004](docs/adr/004-ai-agents-and-skills.md).

---

## §5. Quality bar for every finding / report

- **Evidence-based.** Cite file paths and line numbers. No "somewhere in the codebase".
- **Risk-aware.** Consider the impact on NuGet consumers, not just the local diff.
- **Systematic.** Follow the skill's documented process; NEVER improvise the steps.
- **Doc-first.** Search [`docs/`](docs/ClaudeCodingGuide.md) — especially `ClaudeCodingGuide.md`,
  `adr/`, `reviews/` — before analysing code.

Markdown / Mermaid hygiene rules live in Guide §21.

---

## §6. Dependency management — fix at source, NEVER mask

Rule: when a CVE warning (`NU1901`–`NU1904`) or a downgrade (`NU1605`) appears,
fix the **parent** dependency at the seam; a direct `PackageReference` override is
the last resort and lives only in the project that directly references the
problematic parent. The full procedure (parent lookup, fix options, BAD/GOOD
examples, commit hygiene) lives in the
[`dependency-audit`](.github/skills/dependency-audit/SKILL.md) skill — invoke it.

Verify after the fix (repo shell is PowerShell, §7):

```powershell
dotnet build SolTechnology.Core.slnx 2>&1 | Select-String "NU190[1-4]"   # must be empty
```

(bash dev boxes: `... 2>&1 | grep -E "NU190[1-4]"`)

`NU1900` is **not** a CVE; it means audit data could not be downloaded (unreachable
feed, auth required). It is demoted to warning in `src/Directory.Build.props` via
`<WarningsNotAsErrors>NU1900</WarningsNotAsErrors>`. Fix the feed locally; NEVER
touch `Directory.Build.props` for it.

---

## §7. Repo facts (essentials only)

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

## §8. Cross-references — where to find what

| Topic | Source of truth |
|---|---|
| Project / folder layout, layer dependencies | `docs/ClaudeCodingGuide.md` §1 |
| `ModuleInstaller` pattern | `docs/ClaudeCodingGuide.md` §2 |
| CQRS (commands, queries, validators) | `docs/ClaudeCodingGuide.md` §3 |
| Tale Framework (chapters, contexts, persistence) | `docs/ClaudeCodingGuide.md` §4 |
| DataLayer (SQL, HTTP clients, repositories) | `docs/ClaudeCodingGuide.md` §5 |
| Domain layer rules | `docs/ClaudeCodingGuide.md` §6 |
| API / `Program.cs` / controllers | `docs/ClaudeCodingGuide.md` §7 |
| Tests (Unit / Component / E2E split, density) | `docs/ClaudeCodingGuide.md` §8 |
| Class-level rules (size, primary ctor, braces, comments, `#region`) | `docs/ClaudeCodingGuide.md` §9 |
| Naming, acronyms (ALL CAPS) | `docs/ClaudeCodingGuide.md` §10 |
| Logging (placeholders, `[]` brackets, levels) | `docs/ClaudeCodingGuide.md` §11 |
| Validation (FluentValidation, Guards) | `docs/ClaudeCodingGuide.md` §12 |
| Error handling (`Result`, throw/catch layer rules) | `docs/ClaudeCodingGuide.md` §13 |
| Configuration binding | `docs/ClaudeCodingGuide.md` §14 |
| Anti-patterns + fix-on-touch / report-only policy | `docs/ClaudeCodingGuide.md` §15 |
| Public module documentation structure | `docs/ClaudeCodingGuide.md` §18 |
| AI-only documentation authoring | [`docs/AIDocsGuide.md`](docs/AIDocsGuide.md) |
| Markdown / Mermaid hygiene | `docs/ClaudeCodingGuide.md` §21 |
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

## §9. Self-improvement protocol

If you learn something new during a task, **update your own instructions in the same
turn before yielding**. Silent retention is forbidden. This is the single canonical
statement of the protocol; Guide §20 defines only *how* to append to the guide.

Triggers:

- The user corrects you on a convention, naming, structure, or workflow.
- You discover a non-obvious codebase constraint (build quirk, framework rule, DI pitfall).
- A repeated mistake gets called out.
- A new pattern, helper, or framework addition becomes "the way".
- An ADR is written or amended — also update [`docs/adr/README.md`](docs/adr/README.md)
  in the same change.

Routing:

1. Convention / coding rule → `docs/ClaudeCodingGuide.md` (per its §20).
2. Repo-wide operational / tool / protocol rule → this file (`CLAUDE.md`).
3. AI-doc authoring rule → `docs/AIDocsGuide.md`.
4. Skill-specific lesson → the skill's `SKILL.md`.
5. User-facing docs lesson → `docs/ClaudeCodingGuide.md` §18.

In the same reply, mention the update in one sentence: *"Added rule X to §N of <file>."*

---

## §10. Pre-yield checklist (operational)

Convention checks live in Guide §16 — run both lists. Before declaring a task done:

- [ ] Pre-flight done: relevant Guide sections read and cited in the reply (§0).
- [ ] No forbidden action taken without confirmation (§2).
- [ ] Diff is surgical; §15 fixes (if any) sit in a separate `chore:` commit (§1.3).
- [ ] `get_errors` clean (or remaining warnings explicitly noted as pre-existing) (§3).
- [ ] `dotnet build SolTechnology.Core.slnx` green; for DreamTravel changes, sample app build green (§3).
- [ ] Relevant tests green (§3).
- [ ] No new `NU1901`–`NU1904` / `NU1605` warnings (§6).
- [ ] Guide §16 convention checklist passed.
- [ ] Lessons learned written down per §9.

If any item fails, fix it before yielding.

---

## §11. Common gotchas (repo-operational only)

| Symptom | Cause / fix |
|---|---|
| `RegisterCommands` / `RegisterQueries` / `AddSolTale` find nothing | Called from the wrong assembly. They use `Assembly.GetCallingAssembly()` — invoke from inside the assembly that owns the handlers. |
| `dotnet test` "no tests discovered" | Tests live in `tests/` (outside `src/`). Path: `tests/<Project>.Tests`. |
| AUID JSON round-trip drops the value | The consumer is using `PackageReference` to `SolTechnology.Core.AUID` instead of `ProjectReference` in the sample; `AuidJsonConverter` is missing. |
| Tale JSON deserialisation case-sensitive issues | Use `TaleJsonOptions.Default` (`PropertyNameCaseInsensitive = true`, `IncludeFields = true`). |
| Interactive tale fails immediately | Ensure `AddSolTale()` is called — it wires `TaleManager` + in-memory persistence by default. For durable persistence, use `.UseTaleRepository<T>()` (see `DreamTravel.SQLite` sample). |
| Workload missing on CI | Run `dotnet workload restore SolTechnology.Core.slnx` before `restore` / `build`. See `.github/workflows/publishPackages.yml`. |
| Edits applied via tool don't show in `git diff` | An IDE buffer is overwriting the file. Stop, report, ask the user to close the file in their IDE, then re-apply. NEVER retry blindly. |