---
spec: 2026-11-02-core-2-0-cqrs
step: 05
status: done
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
- [x] `grep -nE "\bAddCQRS\b|RestrictControllerToRegisteredHandlers|CancellationToken\.None" docs/CQRS.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] RM-15: `grep -n "Error.Message" docs/Log.md` matches the new text.

## Open questions
- none

## Deviations

### Snippet verification recorded here, not in a PR description

**Original plan:** the PR author states in the PR description that the snippets compile.

**Actual implementation:** the change was committed directly to `master`, so the result is recorded
here. All 11 C# blocks of `docs/CQRS.md` were extracted into a throwaway `Microsoft.NET.Sdk.Web`
project (not committed) that referenced `SolTechnology.Core.CQRS` and `SolTechnology.Core.Testing`,
with global usings for the CQRS, Core, Errors, Logging, Masking, FluentValidation, NUnit and
FluentAssertions namespaces and stubs for the consumer types (`User`, `IUserRepository`,
`IOrderRepository`, `IEmailSender`, `InMemoryUserRepository`). `dotnet build` reported 0 warnings
and 0 errors; running the testing snippet's body passed, and sending an unregistered query printed
the documented missing-handler message. No snippet needed `// illustrative`. The RM-15 note in
`docs/Log.md` has no C# snippet.

### Page restructured to the public documentation shape

**Original plan:** replace `AddCQRS` and correct the event dispatch semantics.

**Actual implementation:** following `PublicDocumentationGuide.md`, the page was rewritten into the
canonical sections (Features, Registration, Configuration, Usage, Testing, Conventions, What ships
in DI, Working with AI Agent) and documents the step 02 and 03 behaviour (missing-handler error,
behavior order, the failed-`Result` log entry). Handlers in the snippets use primary constructors
instead of undeclared `_repository` fields. The `#### Result pattern` heading keeps the
`CQRS.md#result-pattern` anchor that `README.md` links to. The Working with AI Agent URLs are
unchanged for release step 02; only `AddCQRS` became `AddSolCQRS` there. `docs/Hangfire.md` still
uses `AddCQRS`; hangfire step 06 owns that page.
