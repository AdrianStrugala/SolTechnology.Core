---
name: deliver
description: Entry point of the delivery workflow — drives one request from the state on disk to the final report, stopping only for the planner's intake questions and the user's approval of the plan. Use when the user asks, in any wording or language ("implement", "add", "build", "create", "new feature"), for a code change that sizes as a Feature in the delivery workflow, without naming an agent, a skill, or a single phase; or says "deliver" for a change of any size. Not for a question, and not for a Docs-only or Small change unless the user says "deliver". Resumes an unfinished delivery.
---

# Deliver

Entry point and driver of the
[delivery workflow](../../../docs/architecture/delivery-workflow.md#phases). That page is the
method and its rationale: sizes, phase order, owners, approval, step acceptance, git, and stop
conditions. Read it first. This skill adds only how to drive it and what to report.

## Critical rules

- **The workflow page decides.** NEVER skip, reorder, or merge its phases, add a stop it does not
  name, or accept a phase on anything but the evidence it names.
- **State lives on disk.** Every phase starts by reading the feature brief, the step statuses, and
  git. Nothing is carried in the conversation.
- **Fresh context for the judges, one context for the implementer.** Planning, plan review,
  premortem, and code review each run in their own fresh context (`CLAUDE.md` §4 tool table).
  One implementer carries every step of the feature: dispatch it once and continue the same
  context for the next step and for fix rounds (Claude Code: `SendMessage` to the same subagent;
  Copilot: the same session). NEVER start a fresh implementer per step. No way to start a fresh
  context → finish the current phase, name the next one, and stop; the user starts a new session
  with "deliver" and the delivery resumes.
- **The driver stays out of the work.** It writes no production code and no tests, edits no plan,
  and judges nothing it dispatched. It sets `approved:` after the user's yes, sets `code-review:`
  from the review's verdict, and commits.

## Procedure

1. **Locate.** Find the feature brief the request names, or the only brief that is not
   `completed` / `abandoned` and matches it. No brief → check the baseline, then size the request.
   A brief → resume at the phase its state selects.
2. **Plan and Plan review.** Dispatch the planner with the request verbatim. When it returns
   intake questions, relay them to the user verbatim, wait, and dispatch it again with the answers
   verbatim. With `plan-review: pending`, dispatch the reviewer for its critique only (its steps
   1–3).
3. **Approval.** Present the five items and wait. Dispatch the reviewer — the planner when plan
   review is waived — with the user's answers verbatim; repeat until the user approves.
4. **Premortem.** With `premortem: pending`, dispatch `implement-plan` in a fresh context to
   resolve the gate.
5. **Build**, lowest-numbered row first. Dispatch the implementer with `implement-plan` and the
   step; when it yields with the row `done`, commit, then continue the same implementer with the
   next step. Do not run `./.github/verify.ps1` between steps.
6. **Code review.** When every row is `done`, dispatch `code-review` in a fresh context with the
   brief and the baseline commit. `FAIL` → pass the findings table verbatim to the implementer,
   commit its fix, and dispatch a fresh review; at most 3 rounds. `PASS` → set
   `code-review: done (YYYY-MM-DD)`.
7. **Close.** Dispatch `implement-plan` for the retrospective. Commit.
8. **Report.**

Docs-only: edit, run `./.github/checkDocs.ps1`, commit, report. Small: dispatch one implementer
with the contract from the conversation and `CLAUDE.md` as its protocol, run
`./.github/verify.ps1` yourself, dispatch `code-review` when the change touches package code
([Code review](../../../docs/architecture/delivery-workflow.md#code-review)), then commit.

## Dispatch contract

A dispatch prompt contains only: the role file to read first, the input paths (brief, step), the
base commit, the gate output or findings of a failed round, and "return the report your file
defines". NEVER add the conversation, a summary of earlier phases, or an opinion of the work — a
reviewer told what to expect confirms it.

## Report

In the conversation, in the user's language, in this order:

1. **Outcome** — what exists now, in at most 5 sentences.
2. **Read in this order** — at most 7 file links following the call path from the entry point,
   one line each. When the change adds or alters a flow across two or more layers, dispatch the
   [`diagram`](../../agents/diagram.agent.md) agent and link the diagram first.
3. **Decisions and deviations** — from the brief.
4. **Evidence** — each gate, the command, the result, from the last run.
5. **Code review** — rounds run; findings fixed (count); findings left, each with `file:line` and the reason.
6. **Not verified** — everything no gate covered: tests that need Docker, manual checks.
7. **Semver impact** and every `CLAUDE.md` §2 action performed.
8. **Your next actions** — the branch name, the commit count, push and pull request.

## Constraints

- Do not mark a step, a gate, or a feature done; the owning skill sets its own state.
- Do not start a second step in one dispatch.
- Do not treat the report as the durable record; the feature brief is.
- Write repository files in English and mirror the user's language in conversation.
