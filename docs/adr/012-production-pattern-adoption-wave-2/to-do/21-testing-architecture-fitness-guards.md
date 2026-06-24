---
adr: 012-production-pattern-adoption-wave-2
step: 21 of 24
status: to-do
---

# Step 21: D3 — Architecture "fitness" guard tests + recipe (`Core.Testing` + repo self-tests)

## Summary
Adopt two self-policing "executable convention" tests for the SolTechnology.Core repo and document
the pattern as a shippable recipe in `Core.Testing`: (1) a **build-hygiene guard** asserting every
`Directory.Build.props` enables `TreatWarningsAsErrors` with only an explicit, commented NoWarn
allow-list, and (2) a **test-host containment guard** asserting `WebApplicationFactory` / `APIFixture`
is instantiated only in approved base classes.

## Affected components
- `tests/` — a repo self-test fixture hosting both guard tests (add to an existing test project or a
  small new guard-tests project; if new, add it to `SolTechnology.Core.slnx` `/Tests/`). Prior art:
  `sample-tale-code-apps/aiia-storage/src/Aiia.Storage.SolutionTests/SolutionTest.cs` already walks
  `.csproj` files asserting `TreatWarningsAsErrors` — model the build-hygiene guard on it.
- `docs/Testing.md` — document the "fitness guard" recipe so consumers can adopt it.
- (No `src/` production code — this step is tests + docs only.)

## Details
- **Build-hygiene guard:**
  - Walk every `Directory.Build.props` (root, `src/`, `tests/`, sample apps) and assert
    `TreatWarningsAsErrors=true` is set at the build-foundation level (`src/Directory.Build.props`
    already does — `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
  - Assert the only suppressed warnings are an explicit allow-list **with a reason comment**. The
    current allow-list is `WarningsNotAsErrors = NU1900;NU1510` in `src/Directory.Build.props`, each
    documented with a comment — encode exactly that allow-list.
  - **Known laggards the guard must account for:** `src/SolTechnology.Core.SQL`,
    `src/SolTechnology.Core.Scheduler`, and `src/SolTechnology.Core.MessageBus` set
    `TreatWarningsAsErrors=false` at the **project** level today (verified). The guard must either
    (a) ship an explicit, commented allow-list entry for each of these three projects, or (b) be
    paired with fixing them. Recommend (a) — document each laggard with its reason so the guard fails
    on any *new* unlisted suppression. Decide and record in the step's implementation.
  - Designed to **fail rather than be edited around** — a new `TreatWarningsAsErrors=false` or a new
    `NoWarn`/`WarningsNotAsErrors` entry without an allow-list update breaks the build.
- **Test-host containment guard:**
  - Regex over the test tree asserting `new WebApplicationFactory` / `APIFixture` (the repo's
    `SolTechnology.Core.API.Testing` fixture) is instantiated **only** in approved base classes,
    preventing ad-hoc test-host proliferation. Encode the approved base-class allow-list.
- **Recipe doc:** capture both guards as a reusable pattern in `docs/Testing.md` (what they enforce,
  how to add the allow-list, why they are designed to fail loudly).

## Acceptance criteria
- The build-hygiene guard passes against the current repo with an explicit allow-list covering the
  three known `TreatWarningsAsErrors=false` projects and the `NU1900;NU1510` suppression — and fails
  if a new unlisted suppression is introduced.
- The test-host containment guard passes against the current test tree and fails if
  `WebApplicationFactory`/`APIFixture` is instantiated outside an approved base class.
- `docs/Testing.md` documents the fitness-guard recipe.
- No production (`src/`) code is added — tests + docs only.

## Open questions
- New guard-tests project vs adding to an existing `tests/*` project. Recommend a small dedicated
  `tests/SolTechnology.Core.Architecture.Tests` (clear intent, no coupling); flag for the reviewer.
- Allow-list-the-three-laggards vs fix-them. Recommend allow-list now (smaller blast radius), with a
  follow-up to fix; flag for the reviewer.

