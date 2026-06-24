---
adr: 012-production-pattern-adoption-wave-2
step: 00 of 24 (premortem gate)
status: reviewed
---

# Step 00: Run premortem (implementation gate)

> **Gate — runs first.** Authored last (the plan must be complete to premortem it) but numbered `00`
> so the "lowest `⬜ to-do` first" rule executes it before step `01`
> ([ADR-006 §5](../../006-implementation-plan-workflow.md)). No `01..23` step starts until this gate
> returns *Go* / *Go with mitigations*.

## Summary
Before **any** production code from this plan is written, run the
[`premortem`](../../../../.github/skills/premortem/SKILL.md) skill over ADR-012 as a whole and over
each high-risk step. Implementation is **blocked** until the premortem returns *Go* or *Go with
mitigations*. This is the mandatory gate per the implementation-planning agent contract.

## Affected components
- None (process gate — no files under `src/`, `tests/`, or pipeline configs are changed by this
  step). The premortem output is recorded in the plan's working notes / PR description, not as new
  `src/` code.

## Details
Run the premortem against ADR-012 with particular attention to these high-plausibility,
high-impact scenarios (each cites concrete evidence):

- **New-package public NuGet surface (steps 04–06, 13).** This wave adds **three** new packages with
  new **public** surface — a wrong shape is a future MAJOR:
  - `IDistributedLockService` (`Core.DistributedLock`, steps 04–05),
  - `BaseUpstreamServiceHealthCheck<TReport>` + the pure `HealthReport`→JSON formatter
    (`Core.HealthChecks`, step 06),
  - `IIdempotencyStore` / `StoredResponse` (public in `Core.Api`, step 11) **plus** the thin glue
    package `SolTechnology.Core.Api.Idempotency.Redis` and its `AddRedisIdempotencyStore()`
    registration seam (step 13).
  Premortem each contract before it ships. Pair with
  [`blue-red-team`](../../../../.github/skills/blue-red-team/SKILL.md) on the new-package decision
  sub-sections (the idempotency glue package is the reviewer-approved option (ii): `Core.Api` stays
  Redis-free; the glue package references both `Core.Api` and `Core.Cache`).
- **DistributedLock degrade-to-`null` guard-rail (steps 04–05, 10).** If a backend failure throws
  instead of returning `null`, the leader-elected poller (`Core.Scheduler`) crashes its host loop.
  Verify the no-throw-on-failure contract end-to-end.
- **HealthCheck cancellation taxonomy + ASP.NET-free foundation (step 06).** If caller-cancellation is
  mapped to `Unhealthy` instead of rethrown, orchestrators see false-negative health and cycle pods —
  verify rethrow. Also verify the foundation stays ASP.NET-free (it references
  `Microsoft.Extensions.Diagnostics.HealthChecks`, **not** the ASP.NET
  `Microsoft.AspNetCore.Diagnostics.HealthChecks`): the JSON writer is a pure `HealthReport`→JSON
  formatter, and the `HttpContext`-shaped `HealthCheckOptions.ResponseWriter` adapter lives in the
  host / `Core.Api`, so `Core.SQL` and `Core.Cache` never gain an ASP.NET `FrameworkReference`
  (steps 07–08).
- **Idempotency 5xx rule + remove-on-exception (steps 11–13).** If a `5xx` is ever stored, or the key
  is not removed on a handler exception, a transient failure becomes permanently non-retryable —
  the worst failure for a payment-style endpoint. Verify both guard-rails and the atomic-add
  concurrency path, **especially in the new `Core.Api.Idempotency.Redis` glue package (step 13)**
  where two instances can race the same key.
- **Correlation queue rule (step 17).** If the client id leaks onto queue messages, a caller-facing
  token enters a context where it has no meaning. Verify "platform flows, client does not".
- **B4 default on the exception path (step 02).** Confirm unmapped-5xx ⇒ recoverable, mapped-4xx ⇒
  not, is the shipped default and is documented as the contract. Note `FromError` writes
  `error.Recoverable` directly (no options param); `FromException` derives the status-based default.
- **`ValidateOnStart` everywhere (ADR-010 G3).** Every new `AddOptions<T>` in this wave
  (DistributedLock, HealthChecks, Idempotency, LeaderElection, DeploymentSlot) must
  `.ValidateOnStart()` — premortem a missing call (fails at first request instead of boot).
- **Build-hygiene guard blast radius (step 21).** The guard will flag existing
  `TreatWarningsAsErrors=false` projects (`Core.SQL`, `Core.Scheduler`, `Core.MessageBus`) — confirm
  the allow-list strategy so the guard does not break the build on first run.
- **New dependency CVE gate (step 05).** Re-run `validate_cves` on the exact pinned
  `DistributedLock.*` versions (clean at 2026-06-24) before merge; resolve any finding via
  [`dependency-audit`](../../../../.github/skills/dependency-audit/SKILL.md). The new
  `Core.Api.Idempotency.Redis` glue package (step 13) introduces **no new third-party NuGet** — it
  only project-references `Core.Api` + `Core.Cache` — so it adds no CVE surface of its own.

## Acceptance criteria
- The premortem has been run over ADR-012 and recorded.
- Every guard-rail above is confirmed as covered by a step's acceptance criteria (or a mitigation is
  added).
- The premortem verdict is **Go** or **Go with mitigations**; any *No-Go* scenario has a recorded
  mitigation folded back into the relevant step file before code begins.
- Only after this gate passes may the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md)
  skill begin moving step files `01..23` from `to-do/` (or `reviewed/`) to `done/`.

## Open questions
- none — this is the fixed opening gate.

