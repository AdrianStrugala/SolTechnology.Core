---
adr: 010-production-pattern-adoption-programme
step: 07 of 10
status: to-do
---

# Step 07: Author the Testing-companions child ADR (T1, T2)

## Summary
Author the child ADR (provisional ADR-016) that adds an AutoFixture UTC `DateTime` specimen and an
NSubstitute data-attribute factory to `SolTechnology.Core.Testing`. Small, self-contained, no
inter-dependencies. Seeds its own plan and premortem.

## Affected components
- `docs/adr/<next>-testing-companions-utc-and-data-attribute.md` — the child ADR.
- `docs/adr/<next>-testing-companions-utc-and-data-attribute/` — its plan folder.

## Details
- **T1 — `UtcDateTimeSpecimen`.** An AutoFixture specimen that generates UTC `DateTime`s, for
  consumers testing EF/Postgres (which reject non-UTC `DateTimeKind`).
- **T2 — `AutoNSubstituteDataAttribute(params Type[] customizations)`.** Composes AutoFixture
  customizations per test, layering `UtcDateTimeSpecimen` and others. **NSubstitute, not Moq** — the
  source's `AutoMoq` usage is the anti-pattern the canonical stack rejects
  (`ClaudeCodingGuide.md` §8: NUnit + FluentAssertions + NSubstitute + AutoFixture).
- Keep the helpers in the existing `SolTechnology.Core.Testing` package — no new project.

## Acceptance criteria
- Child ADR authored with blue/red + premortem-as-final-step; semver **MINOR** (additive).
- T2 explicitly uses NSubstitute (not Moq) and composes `UtcDateTimeSpecimen`.
- No new test project is introduced (helpers live in `SolTechnology.Core.Testing`).
- Index row added in `docs/adr/README.md`.

## Open questions
- none — T1/T2 are unblocked.

