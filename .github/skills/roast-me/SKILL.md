---
name: roast-me
description: Interview the user one question at a time before planning or coding. Use when the request is vague, before invoking the implementation-planning agent, or when the user says "roast me" / "stress-test this". Maintains a running ledger of resolved / open / asking branches across turns.
---

# Roast Me

Before planning or coding, interview the user **one question per turn** until the full intent of
the task is clear. The skill defeats the failure mode *"I assumed I knew what was wanted"* by
making every assumption explicit and confirmed.

## When to use

- The request is vague, under-specified, or contradicts something in the repo.
- Before invoking the [implementation-planning](../../agents/implementation-planning.agent.md)
  agent on anything non-trivial.
- Before a large code change to surface hidden assumptions.
- The user says "roast me" / "stress-test this" / "grill me".

## When NOT to use

- The spec is already clear and unambiguous.
- The user has explicitly said "implement now" / "just do it".
- Clarification cost would exceed implementation cost (one-line fix, typo, doc update).

## Procedure

### 1. Explore first

Scan the codebase for context (existing patterns, naming, dependencies) BEFORE forming any
questions. Continue to explore between questions as new answers reveal new branches to investigate.

### 2. Ask one question per turn

ONE question per reply. Multi-question turns drop signal — answers blur together and the user
cannot scan-confirm. Each question MUST:

- Offer a **recommended default** so the user can confirm with one word ("yes" / "default").
- Use a **tight shape** (see §Question shapes below).
- Reference specific files / types in backticks where applicable.

### 3. Maintain the ledger

EVERY reply starts with the ledger. No exceptions:

```
✅ Resolved
- <decision> → <chosen answer>
- ...

🔍 Open branches (not yet asked)
- <short bullet>
- ...

❓ Currently asking
- <the one question, with a recommended default>
```

Without the ledger, earlier answers drift out of context on long threads. The ledger is the
short-term memory of the skill.

### 4. Do not act between questions

The only actions allowed between questions:

- `read_file` to gather context.
- `grep_search` / `semantic_search` to verify a claim.

NEVER: edit files, run terminal commands, start a plan, write a draft.

### 5. Stop condition

When you can restate the task in 3–5 unambiguous bullets with no open branches, STOP asking.
Produce:

```markdown
## Task restatement
- <bullet>
- <bullet>
- <bullet>

Ready to proceed?
```

Wait for the user to confirm "yes" / "ready" / "go" before handing off.

### 6. Soft cap

If 10 questions accrue without convergence, pause. Ask: "Continue interviewing or switch to
direct implementation with stated assumptions?" Let the user pick.

### 7. Escape hatch

If the user says "just do it" / "stop asking" / "enough questions" / "implement now", switch
immediately to the §5 summary step with whatever is resolved so far. Mark every open branch
as `assumed: <default>` in the restatement.

## Question shapes

Favour tight shapes over open prose:

| Shape | Example | When to use |
|---|---|---|
| Binary | "Opt-in or opt-out by default?" | Two clear sides exist. |
| Constrained multi-select | "Storage: in-memory, SQLite, or external?" | Finite, enumerable options. |
| Default-confirm | "Default to MIT licence — yes?" | A reasonable default is obvious; user confirms. |
| Open | "What is the primary success metric?" | The space is genuinely unbounded. Use sparingly. |

## After the stop condition

Once the user confirms "Ready to proceed?", produce a summary handoff:

1. **Key decisions** + the rationale for each.
2. **Constraints and trade-offs** discovered during the interview.
3. **Skills or agents to invoke next** (e.g. "hand off to `implementation-planning`",
   "go straight to `code-review`").
4. **Recommended next step** in one sentence.

This summary becomes the seed for the next phase. Pass it verbatim to the next skill / agent.

## Failure modes

- ❌ Asking two questions in one turn ("…and also, should X?"). Split them.
- ❌ Starting to plan or edit while questions remain open.
- ❌ Dropping the ledger. Without it, the conversation rewinds.
- ❌ Re-asking something already in `✅ Resolved`.
- ❌ Keeping `✅ Resolved` bullets long — one line each, decision + answer, no rationale.
- ❌ Asking the user something the codebase or a specialist skill can already answer. Find the
  answer first; confirm only if it materially affects the plan.
- ❌ Improvising a freehand mini-interview when this skill is unavailable. STOP and tell the
  user `roast-me` is required (CLAUDE.md §2). Do not substitute a hand-rolled questionnaire.

## Constraints

- DO NOT write code, edit files, run terminal commands, or produce a plan between questions.
- DO NOT batch multiple questions into one turn.
- DO NOT skip the ledger. It is the contract of this skill.
- ALWAYS present a recommended default with each question.
- ALWAYS stop the interview once the task is restated in 3–5 bullets — do not gold-plate.

