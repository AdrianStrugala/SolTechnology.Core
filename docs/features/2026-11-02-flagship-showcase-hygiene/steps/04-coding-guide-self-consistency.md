---
spec: 2026-11-02-flagship-showcase-hygiene
step: 04
status: to-do
---

# Step 04: Coding Guide self-consistency

## Summary
The binding guide contradicts itself and the code (`P1-6`):

- §12 requires the obsolete, non-packable `SolTechnology.Core.Guards` (`logging-cache#16`,
  `docs/ClaudeCodingGuide.md:510`).
- §7 names `ResponseEnvelopeFilter` (`:315`, `:357`), which does not exist; the filter is
  `ResultConversionFilter`. `AddAuthenticationAndBuildFilter` is already gone: api-auth 01
  (`d46525f1`, `C-28`) rewrote the line to `AddSolAuth(...)` from `SolTechnology.Core.Auth` (`:358`).
- §7 presents `CalculateBestPathController` as its template, but that file does not follow §7
  (fixed in step 03).
- §15 lists controller `try/catch` (`CalculateBestPathController.CalculateBestPathV1`, `:551`) as a
  live example, which step 03 removes. Its Newtonsoft row (`:554`) names the same controller,
  whose `JsonConvert` use step 03 also removes (resync 2026-11-10).
- §13 says `AggregateError` comes from `SolTechnology.Core.CQRS` (`:528`); it lives in the
  `SolTechnology.Core` package, namespace `SolTechnology.Core.Errors`
  (`src/SolTechnology.Core/Errors/AggregateError.cs`; resync 2026-11-10).

Out of scope since 2026-11-03 (user: "nie dropuj joke z kodu"): the "dupa" CORS policy is a kept
joke. Its §15 row is already removed and §7 already records it as a kept joke (§9.10); this step
does not edit either.

## Affected components
- `docs/ClaudeCodingGuide.md` — EDIT — §12 `:510` → BCL guards (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegative`); §7 `:315` and `:357` `ResponseEnvelopeFilter` → `ResultConversionFilter` (the §7 auth line already reads `AddSolAuth(...)`, delivered by api-auth 01); §7 `:334` the controller template declares `typeof(CalculateBestPathResult)` for 200 and `ProblemDetails` for failures (premortem SRM-9); §13 `:528` `AggregateError` → `SolTechnology.Core` (namespace `SolTechnology.Core.Errors`); §15 "Where seen" column marks the fixed controller `try/catch` example (`:551`) as historical without deleting the rule, and moves the Newtonsoft row's example (`:554`) to the code that still uses it after step 03 (`GoogleHTTPClient.*` in `DreamTravel.GeolocationDataClients`); the §7 kept-joke sentence and the §9.10 joke exception stay unchanged
- `CLAUDE.md` — CHECK — `§11` gotcha rows. Verified accurate on 2026-11-10 at `c0eb9482` (auid 03 rewrote the AUID row in `dd64b0d2`; the Tale rows match `TaleJsonOptions` and `AddSolTale`); edit only if steps 01–03 changed a fact a row states

## Changes
- Prerequisite: core 05 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)), tale 03 and tale 11 ([`2026-11-02-core-2-0-tale`](../../2026-11-02-core-2-0-tale.md)), auid 03 ([`2026-11-02-core-2-0-auid`](../../2026-11-02-core-2-0-auid.md)) (former core steps 08, 18, 43, 44), and step 03 of this feature are merged (all but step 03 done, verified 2026-11-10). This step moved here from the core feature at the split (2026-11-02) because it must follow step 03.
- Rules are not weakened; only stale references change.
- Premortem SRM-9 (step 00, 2026-11-10, scenario 9: the template advertises the `Result` envelope that `ResultConversionFilter` removes, so Swagger and clients built from it expect `{isSuccess, data}`): the template documents the wire type. One sentence under the template says that a successful `Result<T>` is sent as `T` and a failed one as ProblemDetails, linking `docs/Api.md`.
- Premortem SRM-8 (scenario 8): §9.10's examples (`//I'm just void`) and the §7 kept-joke sentence stay byte-identical.
- Guide §0, §1, and §6 event placement is edited by step 05 in the same PR as the `CitySearched` move (premortem SRM-15); this step leaves those lines alone, whichever of the two steps runs first.

## Acceptance criteria
- [ ] `grep -nE "SolTechnology\.Core\.Guards|AddAuthenticationAndBuildFilter|ResponseEnvelopeFilter" docs/ClaudeCodingGuide.md` prints nothing.
- [ ] The documentation-cleanup skill passes for `docs/ClaudeCodingGuide.md` and `CLAUDE.md`.
- [ ] `git diff <base> -- docs/ClaudeCodingGuide.md | grep -E '^[-+].*(dupa|joke)'` prints nothing (the kept-joke lines are untouched). `<base>` is the commit before this step: `master` has no branch protection and may receive the step directly, so `origin/master` is not a reliable base.
- [ ] `grep -n "AggregateError" docs/ClaudeCodingGuide.md` names `SolTechnology.Core`, not `SolTechnology.Core.CQRS`.
- [ ] SRM-9: `grep -n "typeof(Result<" docs/ClaudeCodingGuide.md` prints nothing.

## Open questions
- none

## Deviations
