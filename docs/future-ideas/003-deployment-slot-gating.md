# FI-003: Deployment-slot gating

> **Status:** ⏸️ Parked
> **Parked date:** 2026-06-25
> **Source:** [Production pattern adoption wave 2, step 09](../features/2026-06-24-production-pattern-adoption-wave-2.md#completion-summary)
> **Would-be target:** `SolTechnology.Core.Scheduler`
> **Semver / Effort:** MINOR · S

---

## Context — what it is

An `IDeploymentSlotProvider` abstraction + `DeploymentSlotGuard` helper so background jobs
(`BackgroundService`, Hangfire recurring jobs) skip work when running on a non-live blue/green
deployment slot.

Default implementation reads the App Service `WEBSITE_SLOT_NAME` environment variable and compares
to the configured "live" slot name. Custom providers can be registered for non-Azure environments.

## Why parked

- No current production app needs slot-aware background job suppression.
- Adds DI complexity without an immediate consumer.
- Hangfire's built-in queue mechanism + the distributed lock from `Core.Cache` (Feature-002 step 04)
  cover the "don't run on multiple instances" need without slot awareness.

## What would unpark it

- A deployment to a blue/green App Service (or similar) where background jobs on the staging slot
  cause real side-effects (e.g. sending emails, processing payments).
- A consumer app that needs "only the live slot runs Hangfire recurring jobs" semantics.

