---
adr: 010-production-pattern-adoption-programme
step: 05 of 10
status: to-do
---

# Step 05: Author the cross-cutting coding-guide child ADR (G1, G2, G3, G5, G6, G7)

## Summary
Author the child ADR (provisional ADR-014) that encodes the cross-cutting production conventions into
`docs/ClaudeCodingGuide.md` and adds the one missing `Result` combinator. G4 (Guards deprecation) is
already done and excluded. Depends on open question Q4 (single `Result`) and Q5 (`ValidateOnStart`
behaviour). Seeds its own plan and premortem.

## Affected components
- `docs/adr/<next>-cross-cutting-conventions.md` — the child ADR.
- `docs/adr/<next>-cross-cutting-conventions/` — its plan folder.

## Details
- **G1 — `TimeProvider`.** Ban raw `DateTime.UtcNow`/`DateTimeOffset.Now` in library code; inject
  `TimeProvider`. Evidence: production usages are confined to `AUID` (`Auid.cs`) and `Story`
  (`StoryEngine`, `StoryManager`, `StoryInstance`); Cache/SQL/Logging/MessageBus/Hangfire are already
  clean. Test-infrastructure usage (`Testing/Containers`) is exempt.
- **G2 — `JsonSerializerOptions`.** Encode "must be `static`/singleton" and apply in serialization
  helpers (ties to the Cache ADR-012 guard-rail).
- **G3 — `ValidateOnStart` everywhere.** Extend `.ValidateDataAnnotations().ValidateOnStart()` to
  `AddCache`/`AddSQL`/`AddMessageBus` (Logging + HTTP already do). **Behaviour change** — bad config
  fails the host, not the first request — premortem-check in the child ADR (open question Q5).
- **G5 — one `Result` + `MapError`.** Add `MapError` to `ResultExtensions`
  (`src/SolTechnology.Core.CQRS/`) — the only missing combinator (`Map`/`Bind`/`Tap`/`Match`/`Ensure`
  already ship). Document that `SolTechnology.Core.CQRS.Result` is the single canonical type.
- **G6 — `[ExcludeFromCodeCoverage]`** on `ModuleInstaller`s / config records / infra adapters with a
  `Justification`.
- **G7 — primary-constructor consistency** for new code (`ClaudeCodingGuide.md` §9.5 already prefers
  them).
- **Guard-rail (source defect):** make the logging template/argument-mismatch a review-checklist item
  in §11.

## Acceptance criteria
- Child ADR authored with blue/red + premortem-as-final-step; semver **PATCH–MINOR** (`MapError`
  addition is MINOR for CQRS).
- `MapError` lands under `SolTechnology.Core.CQRS`; single-`Result` decision documented.
- G3 fail-fast behaviour change is premortem-checked.
- Index row added in `docs/adr/README.md`.

## Open questions
- Q4 (single `Result`) and Q5 (`ValidateOnStart`) — must be resolved in step 01 before authoring.

