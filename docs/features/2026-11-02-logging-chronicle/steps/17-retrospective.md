---
spec: 2026-11-02-logging-chronicle
step: 17
status: to-do
---

# Step 17: Retrospective

## Summary
Close the Chronicle feature. Verify the delivered behavior against the brief's acceptance
criteria, move the delivered design and rationale into current architecture pages, complete the
durable feature record, and delete this working folder. Run only after every step 01–16 is `done`
and after the logging, infrastructure-module, and foundation retrospectives listed under the
brief's cross-feature dependencies have created the architecture pages this step extends.

## Affected components
- `docs/architecture/logging.md` — EDIT (exists since the `2026-11-02-core-2-0-logging` retrospective) — remove the `ITimingService` registration bullet and the "Timings and masking" timing paragraph, update the request-envelope table (2144–2146 only with Chronicle disabled; 2147 removed; 2155) and the `OperationsName` paragraph (CQRS no longer owns an `ActivitySource`); NEW "Chronicle" section: ambient `AsyncLocal` tree and struct handle (B2), placement in Logging (B1), integration seams and the SqlClient observer (B3), flight-recorder provider (B4), request report as the finish entry (B5), payload shape (B6), OTel mapping (B7), tail sampling (B8), `Begin` vs `BeginRoot` (B9, B11), bounds and markers, EventIds 2154/2155
- `docs/architecture/cqrs.md` — EDIT — pipeline step replaces the CQRS `ActivitySource` (the "Activities start on the CQRS-owned `ActivitySource`" sentence); event-handler steps, `Cancelled` on a cancelled dispatch (P-26); fire-and-forget event roots
- `docs/architecture/tale-framework.md` — EDIT — run and chapter steps; Tale → Logging package edge
- `docs/architecture/http-client.md` — EDIT — handler chain with `ChronicleHTTPHandler` (outermost) and `ChronicleHTTPAttemptHandler` (after resilience)
- `docs/architecture/background-processing.md` — EDIT — "Filters": `UseSolFilters` installs four pieces, `ChronicleJobFilter` with `Order = 0` and why; Hangfire job roots, their outcome mapping from `Result`, cancellation, and the overlap guard
- `docs/architecture/infrastructure-modules.md` — EDIT (exists) — SQL capture through SqlClient diagnostics, the SQL package is unchanged, connection-open retries are not steps; MessageBus message roots and the settlement → status mapping
- `docs/architecture/README.md` — EDIT — only if a page above is missing from the topic list
- `docs/features/2026-11-02-logging-chronicle.md` — EDIT — completion
- `docs/features/2026-11-02-logging-chronicle/` — DELETE — the whole working folder

## Changes
1. Review the delivered feature and run final validation:
   - `dotnet build SolTechnology.Core.slnx -c Release`
   - `pwsh ./.github/runTests.ps1`
   - `cd sample-tale-code-apps/DreamTravel && dotnet build && dotnet test tests/Component`
   - every checkbox under the brief's `## Acceptance criteria`, including the clean-room grep.
2. Update the affected `docs/architecture/*.md` pages above to current behavior and rationale.
   Move the B1–B11 rationale that shipped; leave rejected alternatives in the brief.
3. Complete the durable feature record: `## Completion summary` (what shipped, PRs, final
   `CH-nn`/`LC-nn`/`CP-nn` state), `## Deviations` (collected from every step's `Deviations`),
   and `## Follow-ups` (for example a JSON payload state, other database providers, per-name slow
   thresholds, a Chronicle sequence diagram through the `diagram` agent).
4. Set the brief's frontmatter to `status: completed` (or `abandoned` with the reason) and
   `completed: YYYY-MM-DD`. This needs the user's confirmation (`CLAUDE.md §2`).
5. Verify that no durable link points into the working folder: replace the brief's link to
   `summary.md` under `## Implementation plan` with plain text, then run
   `grep -rn "2026-11-02-logging-chronicle/" docs README.md src tests sample-tale-code-apps .github` and expect no output.
6. Delete `docs/features/2026-11-02-logging-chronicle/` (needs the user's confirmation,
   `CLAUDE.md §2`).

## Acceptance criteria
- [ ] Every acceptance criterion in the brief is checked.
- [ ] `grep -n "Chronicle" docs/architecture/logging.md docs/architecture/cqrs.md docs/architecture/tale-framework.md docs/architecture/http-client.md docs/architecture/background-processing.md docs/architecture/infrastructure-modules.md` lists each file.
- [ ] `grep -rln "ITimingService" docs --include=*.md | grep -v "docs/features/"` prints only `docs/Log.md`.
- [ ] The brief has `status: completed` (or `abandoned`), a `completed:` date, and non-empty `Completion summary`, `Deviations`, and `Follow-ups` (`none` is a valid entry).
- [ ] `grep -rn "2026-11-02-logging-chronicle/" docs README.md src tests sample-tale-code-apps .github` prints nothing.
- [ ] `test ! -d docs/features/2026-11-02-logging-chronicle` succeeds.

## Open questions
- none

## Deviations
