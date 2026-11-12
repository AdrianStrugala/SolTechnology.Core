# AI Docs Guide — authoring `CLAUDE.md`, `ClaudeCodingGuide.md`, `*.agent.md`, `SKILL.md`

A third class of docs lives in this repo: files read **exclusively by AI agents**
(`CLAUDE.md` at root, `docs/ClaudeCodingGuide.md`, every `.github/agents/*.agent.md`,
every `.github/skills/*/SKILL.md` — and this file). They are not user-facing; they are
not narrative; they are the agent's operational and convention memory. Optimise them
for four things, in this order:

1. **Routing speed** — the agent must find the rule for the task in the file's first section.
2. **Compliance verification** — both agent and reviewer must be able to point at "you
   broke §X" without ambiguity.
3. **Token efficiency** — every sentence competes with code for context window space.
4. **Self-update loop** — agents must be able to append rules without breaking sections
    that other docs, architecture pages, and skills cite.

Everything else (prose, motivation, history) is waste.

## File roles (one role per file)

| File | Role | Size budget |
|---|---|---|
| `CLAUDE.md` (root) | Operational protocol: how the agent behaves (pre-flight, behavioral core, tool usage, forbidden actions, dependency management, self-improvement routing). | ≤ 300 lines |
| `docs/ClaudeCodingGuide.md` | C# conventions: what the agent writes (project structure, CQRS, naming, logging, anti-patterns). | indexable; sections are stable cite-targets |
| `docs/PublicDocumentationGuide.md` | Public module documentation structure and authoring rules. | task-focused; link instead of copying |
| `.github/copilot-instructions.md` | Copilot entry point: a pointer to `CLAUDE.md`, no rules. | ≤ 10 lines |
| `.github/agents/*.agent.md` | One role with a multi-step workflow and handoffs (planning, plan review, diagrams). Loaded on demand. | as short as the workflow allows |
| `.github/skills/*/SKILL.md` | One task, end-to-end (code-review, premortem, commit message). Loaded on demand. | as short as the task allows |

One rule = one place. Other files **link**, never copy.

## Hard rules

### A. Form and tone

1. **Imperative, present tense.** "Use primary constructors." not "You should consider
   using primary constructors". "Never throw in `Tell()`." not "Throwing here is
   usually a bad idea".
2. **One term per concept.** If you say "chapter", do not later say "step" / "stage" /
   "phase". LLMs split semantics on synonyms and hallucinate distinctions.
3. **`MUST` / `NEVER` / `PREFER` in caps** for critical rules; ordinary text for the
   rest. The caps build a force hierarchy without prose.
4. **No history.** "We used to have a `RegisterCommands` overload that…" → delete.
    State the current rule. Delivery history goes to dated feature records.
5. **No marketing / praise.** "Beautifully designed Tale Code framework…" → delete.
   The agent does not need motivation; it needs the rule.
6. **No `> Note:` / `> Tip:` / `> IMPORTANT:` blockquotes.** Either the info matters
    (regular sentence) or it doesn't (delete). Same rule as
    [`PublicDocumentationGuide.md`](PublicDocumentationGuide.md#hard-rules).

### B. Structure

7. **Stable section numbering.** §0, §1, …, §N. Numbers are cite-targets — `CLAUDE.md`,
   architecture docs, and skills reference "§9.10", "§18", "§4". **NEVER renumber existing sections.**
   New rules append at the end; if a section grows beyond cohesion, split it but keep
   the old number with a "moved to §M" pointer. Update every citation in the same change;
   delete the pointer once a repo-wide search for the old number returns no hits.
8. **Front-load.** Rules with the highest cost of violation (layer boundaries, secrets
   in config, missing primary ctor, forbidden actions) go first. Edge cases at the end.
9. **Decision tree at the entry.** First section answers "what am I writing?" in 5–7
   points. The agent routes before the first edit.
10. **Tables over prose for matrices.** Layer → references, exception → status, topic →
    source-of-truth, anti-pattern → fix. The public-doc equivalent lives in
    [`PublicDocumentationGuide.md`](PublicDocumentationGuide.md#hard-rules); agents parse tables
    faster than paragraphs.
11. **Workflow / pre-yield checklist at the end.** `- [ ]` list the agent ticks before
    handing control back. Lets the agent self-verify. Operational items belong in
    `CLAUDE.md` §10; convention items in Coding Guide §16 — never both.

### C. Concrete over abstract

12. **BAD/GOOD code pairs for every non-trivial rule.** Few-shot is the strongest signal
    for an LLM — stronger than the prose rule above it. "Comments ≤ 1 line" + ❌/✅
    block (Coding Guide §9.10) beats an essay every time.
13. **Cite-able names.** Types, files, options, methods — always in `backticks`,
    exact-string-searchable. "Inject `ILogger<TSelf>`" not "inject a logger of the
    self type".
14. **Anti-patterns with locations.** "`FetchTrafficHandler` logs `"Skipped " + s.Name`"
    — the agent knows where to fix it on the next pass.
    Mythical "somewhere in the codebase" → delete.
15. **Concrete numbers, not adjectives.** "≤ 100 lines, hard cap 150." not "classes
    should be small". LLMs have no intuition for "small". No "~", no "about".
16. **Deterministic triggers only.** A rule the agent cannot evaluate from its own
    context ("if the IDE flags it", "if the request feels vague") is not a rule —
    replace with an observable condition or an unconditional instruction.

### D. Agent-specific mechanics

17. **Evidence-of-consumption.** Canonical statement: `CLAUDE.md` §0.2. State it once;
    everywhere else, link. Without the rule, the agent silently forgets conventions by
    turn 4; with three copies of it, the copies drift.
18. **Forbidden-action list.** Explicit list of actions requiring user confirmation
    lives in `CLAUDE.md` §2 — one list, no per-doc restatements.
19. **Tool-usage hints next to the rule that needs the tool.** "After editing a file,
    check IDE diagnostics." — the agent knows it is part of the protocol, not a suggestion.
    Name the action first, then each tool's name for it (rule 27).
20. **Self-improvement clause with explicit triggers.** Canonical statement:
    `CLAUDE.md` §9. The Coding Guide's §20 defines only the local how-to-append.
21. **Token-budget hint.** Single section ≤ 150 lines. Above that the LLM starts
    losing earlier fragments. If a section grows — split it into a
    separate file or move detail into a skill.

### E. Cross-file discipline

22. **Cross-link, never copy-paste.** Logging convention lives in Coding Guide §11.
    `CLAUDE.md` says "follow §11 for any `logger.Log*`". The `code-review` skill says
    "check §11". One source of truth.
23. **Skills are situational, not pre-loaded.** Not every rule lives in the guide.
    Skills load on demand — that saves tokens in sessions that don't need them.
    Read-before-invoke rule: `CLAUDE.md` §0.3.

### F. What to avoid

24. **No emoji decoration** beyond `✅` / `❌` in BAD/GOOD pairs and `- [ ]` in
    checklists. LLMs handle `**bold**` and headings well; pictograms are noise.
25. **No "you can" / "usually" / "in most cases".** Either a rule or an exception.
    Exceptions are explicit and name what they cover: "Exception: HTTP-client partials
    (`GoogleHTTPClient.<MethodName>.cs`)."
26. **No `TODO` / "we should later".** Either it's an issue in the tracker or it
    doesn't exist. AI-only docs are not a todo board.

### G. Maintenance

27. **Tool-neutral instructions.** These docs serve GitHub Copilot and Claude Code. State
    the action, then each tool's name for it: "check IDE diagnostics (Copilot:
    `get_errors`; Claude Code: `getDiagnostics`)". NEVER make a rule depend on one tool's
    function name or on a per-machine file. Per-tool invocation of agents and skills:
    `CLAUDE.md` §4.
28. **Frontmatter `description` is the routing text.** It is the only part of an agent or
    skill loaded before invocation. Write what it does, then "Use when <observable
    trigger>".
29. **Code examples match the repo.** Type and method names come from a file that exists,
    and the example obeys every Guide rule. An example that breaks a rule overrides the
    rule (rule 12).
30. **Replace, never stack.** Before adding a rule, search the file for an existing rule on
    the topic and rewrite it in place. `CLAUDE.md` §9 routes the lesson; this rule keeps
    one statement.

## Regression prompts

After changing a rule in `CLAUDE.md`, a guide, an agent, or a skill, run every prompt whose row
names the changed file, in a fresh session of each tool (Copilot, Claude Code). The change is
accepted only when every expected behaviour is observed.

| Prompt | Exercises | Expected behaviour |
|---|---|---|
| "Add a query that returns cities by country to DreamTravel." | `deliver` trigger, `CLAUDE.md` §0, Guide §3, `command-query-event-tale` | Sizes the request as Small and edits directly: no `deliver`, no baseline run, no branch, no commit; cites the consulted sections; one use-case folder; validator in the query file; handler returns `Result<T>`; no per-type DI line; tests handed to `test-writing`. |
| "Rename `ISingletonCache` to `IGlobalCache`." | `CLAUDE.md` §2 | Stops and asks for confirmation before any edit. |
| "Log the city name when the city search fails." | Guide §11 | `[{CityName}]` placeholder in brackets; exception as the first argument; `checkConventions.ps1` passes. |
| "Draw the sequence of `CalculateBestPathTale`." | `diagram` agent | Reads the agent file; traces source; six layer boxes; one file under `docs/diagrams/`. |
| "Bump `FluentValidation`." | `package-management` | Reads the skill; takes the version from `canonical-versions.md`, never from memory. |
| "Fix a typo in `docs/Cache.md`." | `CLAUDE.md` §0 docs-only tier | No build, no tests; runs `checkDocs.ps1`. |
| "Implement refresh tokens for `SolTechnology.Core.Auth`." | `deliver`, `code-review`, delivery workflow | Starts `deliver` without being told to; checks the baseline first; sizes the request as a Feature; stops only for the planner's intake questions and plan approval; runs only a step's own tests inside the step; runs `verify.ps1` and the code review once, in a fresh context, before the retrospective; ends with the 8-part report; no push. |
| "Plan refresh tokens for `SolTechnology.Core.Auth`." | `implementation-planning`, delivery workflow | Runs the Plan phase only, not `deliver`; one dated brief from the template; asks one batched intake round before planning; gate fields set; returns the plan for the user's approval. |

## Package-companion skills

A package-companion skill encodes the usage of a shipped NuGet package (e.g.
[`command-query-event-tale`](../.github/skills/command-query-event-tale/SKILL.md) for
`SolTechnology.Core.CQRS` / `.Tale`). Repo contributors get it from the workspace like every
skill; package consumers opt in through a link in the package README.

- The skill file MUST live in `.github/skills/<name>/SKILL.md` — NEVER under `src/` (it loses
  discovery) and NEVER inside the `.nupkg` (it lands in `~/.nuget`, where no agent reads it).
- The package README (`docs/<Module>.md`) MUST link the skill from a `### Working with AI Agent`
  section, with an absolute `https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/…`
  URL ([`PublicDocumentationGuide.md`](PublicDocumentationGuide.md#hard-rules), rule 11).
- Delivery is pull, never push. NEVER ship a build `.targets`, analyzer, or tool that writes a
  skill into a consumer's repository without an explicit, documented opt-in command.
- Link, never copy: the skill cites the Coding Guide §-numbers, and the README links the skill.

## When refactoring an AI-only doc

- Run `./.github/checkDocs.ps1` before and after; it must pass.

- Strip every blockquote (`> Note:`, `> IMPORTANT:`, `> 🚨`).
- Replace soft-language ("usually", "consider", "it's good to") with imperative.
- Pull every convention rule out of `CLAUDE.md` into the Coding Guide; replace with a
  cross-link in `CLAUDE.md` §8's table.
- Collapse any rule stated in more than one place into one canonical section + links.
- Verify section numbers in the Coding Guide and skills still match every
  cross-reference — a dangling "§9.11" is worse than no reference.
- Compare workflow behavior before and after every agent or skill refactor. Preserve intake
    triggers, state transitions, gates, refusal paths, artifact templates, verification, and handoffs;
    reduced line count is not evidence that the refactor is behavior-preserving.
- Verify current design rationale lives under `docs/architecture/` and delivery history lives in
    one dated `docs/features/YYYY-MM-DD-<name>.md` record.

## Reference implementations

- `CLAUDE.md` — operational protocol (≤ 300 lines, §0 pre-flight, §1 behavioral core,
  §2 forbidden, §8 cross-ref table).
- `docs/ClaudeCodingGuide.md` — C# conventions (§0 decision tree, stable §0–§N, BAD/GOOD
    pairs in §9.10, §15 policy column).
- `docs/PublicDocumentationGuide.md` — public module documentation structure and authoring rules.
- `.github/skills/code-review/SKILL.md` — single-task skill (front-loaded routing,
  no convention duplication, links to the Coding Guide for every rule check).