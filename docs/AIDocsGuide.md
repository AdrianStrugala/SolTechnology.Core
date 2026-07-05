# AI Docs Guide — authoring `CLAUDE.md`, `ClaudeCodingGuide.md`, `*.agent.md`, `SKILL.md`

A third class of docs lives in this repo: files read **exclusively by AI agents**
(`CLAUDE.md` at root, `docs/ClaudeCodingGuide.md`, every `.github/agents/*.agent.md`,
every `.github/skills/*/SKILL.md` — and this file). They are not user-facing; they are
not narrative; they are the agent's operational and convention memory. Optimise them
for four things, in this order:

1. **Routing speed** — the agent must find the rule for the task in the first ~N tokens.
2. **Compliance verification** — both agent and reviewer must be able to point at "you
   broke §X" without ambiguity.
3. **Token efficiency** — every sentence competes with code for context window space.
4. **Self-update loop** — agents must be able to append rules without breaking sections
   that other docs / ADRs / skills cite.

Everything else (prose, motivation, history) is waste.

## Three-layer hierarchy (one role per file)

| File | Role | Size budget |
|---|---|---|
| `CLAUDE.md` (root) | Operational protocol: how the agent behaves (pre-flight, behavioral core, tool usage, forbidden actions, dependency management, self-improvement routing). | ≤ 300 lines |
| `docs/ClaudeCodingGuide.md` | Conventions: what the agent writes (project structure, CQRS, naming, logging, anti-patterns, doc shape). | indexable; sections are stable cite-targets |
| `.github/skills/*/SKILL.md` | One task, end-to-end (code-review, premortem, planning). Loaded on demand. | as short as the task allows |

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
   State the current rule. War stories go to ADRs.
5. **No marketing / praise.** "Beautifully designed Tale Code framework…" → delete.
   The agent does not need motivation; it needs the rule.
6. **No `> Note:` / `> Tip:` / `> IMPORTANT:` blockquotes.** Either the info matters
   (regular sentence) or it doesn't (delete). Same rule as Coding Guide §18.6.

### B. Structure

7. **Stable section numbering.** §0, §1, …, §N. Numbers are cite-targets — `CLAUDE.md`,
   ADRs and skills reference "§9.10", "§18", "§4". **NEVER renumber existing sections.**
   New rules append at the end; if a section grows beyond cohesion, split it but keep
   the old number with a "moved to §M" pointer for one release.
8. **Front-load.** Rules with the highest cost of violation (layer boundaries, secrets
   in config, missing primary ctor, forbidden actions) go first. Edge cases at the end.
9. **Decision tree at the entry.** First section answers "what am I writing?" in 5–7
   points. The agent routes before the first edit.
10. **Tables over prose for matrices.** Layer → references, exception → status, topic →
    source-of-truth, anti-pattern → fix. Same rule as Coding Guide §18.5 — agents parse
    tables faster than paragraphs.
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
14. **Anti-patterns with locations.** "`CalculateBestPathController.CalculateBestPathV1`
    has `try/catch + JsonConvert`" — the agent knows where to fix it on the next pass.
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
    call `get_errors`." — the agent knows it is part of the protocol, not a suggestion.
20. **Self-improvement clause with explicit triggers.** Canonical statement:
    `CLAUDE.md` §9. The Coding Guide's §20 defines only the local how-to-append.
21. **Token-budget hint.** Single section ≤ 150 lines. Above that the LLM starts
    losing earlier fragments under resampling. If a section grows — split it into a
    separate file (as this one was) or move detail into a skill.

### E. Cross-file discipline

22. **Cross-link, never copy-paste.** Logging convention lives in Coding Guide §11.
    `CLAUDE.md` says "follow §11 for any `logger.Log*`". The `code-review` skill says
    "check §11". One source of truth.
23. **Skills are situational, not pre-loaded.** Not every rule lives in the guide.
    Skills load on demand — that saves tokens in sessions that don't need them.
    Read-before-invoke rule: `CLAUDE.md` §0.3.

### F. What to avoid

24. **No emoji decoration** beyond `✅` / `❌` in BAD/GOOD pairs and `- [ ]` in
    checklists. LLMs handle `**bold**` and headings well; piktograms are noise.
25. **No "you can" / "usually" / "in most cases".** Either a rule or an exception.
    Exceptions are explicit: "Exception: legacy test files listed in `CLAUDE.md §X`."
26. **No `TODO` / "we should later".** Either it's an issue in the tracker or it
    doesn't exist. AI-only docs are not a todo board.

## When refactoring an AI-only doc

- Strip every blockquote (`> Note:`, `> IMPORTANT:`, `> 🚨`).
- Replace soft-language ("usually", "consider", "it's good to") with imperative.
- Pull every convention rule out of `CLAUDE.md` into the Coding Guide; replace with a
  cross-link in `CLAUDE.md` §8's table.
- Collapse any rule stated in more than one place into one canonical section + links.
- Verify section numbers in the Coding Guide and skills still match every
  cross-reference — a dangling "§9.11" is worse than no reference.

## Reference implementations

- `CLAUDE.md` — operational protocol (≤ 300 lines, §0 pre-flight, §1 behavioral core,
  §2 forbidden, §8 cross-ref table).
- `docs/ClaudeCodingGuide.md` — conventions (§0 decision tree, stable §0–§N, BAD/GOOD
  pairs in §9.10, §15 policy column, §18).
- `.github/skills/code-review/SKILL.md` — single-task skill (front-loaded routing,
  no convention duplication, links to the Coding Guide for every rule check).