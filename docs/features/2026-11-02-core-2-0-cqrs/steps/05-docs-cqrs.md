---
spec: 2026-11-02-core-2-0-cqrs
step: 05
status: to-do
---

# Step 05: Module docs — `docs/CQRS.md`; failed-`Result` logging note in `docs/Log.md`

## Summary
`docs/CQRS.md` still uses `AddCQRS` (`:9,166`). It claims each event handler runs on its own
background task, but the code runs them in sequence in one scope (`:92`). The failed-`Result`
logging from step 03 also needs the consumer PII note (RM-15), which lives in `docs/Log.md`. Every
C# snippet must compile against the 2.0 API.

Former core step 60 (CQRS part and the RM-15 `docs/Log.md` note); split per library on 2026-11-02.

## Affected components
- `docs/CQRS.md` — EDIT — `AddSolCQRS`; event dispatch semantics; `:164,170,171` URLs are left for release step 02 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md))
- `docs/Log.md` — EDIT — RM-15 note only
- `SolTechnology.Core.slnx` — EDIT — add `docs/CQRS.md` to `/Docs/` if missing

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.CQRS`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigation (core step 00, 2026-11-02), documentation part:
  - RM-15 (scenario 15): `docs/Log.md` states that a failed `Result` is logged at Warning with the error type and `Error.Message`, so `Error.Message` must not contain PII.

## Acceptance criteria
- [ ] `grep -nE "\bAddCQRS\b|RestrictControllerToRegisteredHandlers|CancellationToken\.None" docs/CQRS.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] RM-15: `grep -n "Error.Message" docs/Log.md` matches the new text.

## Open questions
- none

## Deviations
