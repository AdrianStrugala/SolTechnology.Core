# FI-002: Priority background jobs — Hangfire flavour

> **Status:** ⏸️ Parked
> **Parked date:** 2026-06-24
> **Source:** [Production pattern adoption — wave 2](../features/2026-06-24-production-pattern-adoption-wave-2.md) (candidate C3)
> **Would-be target:** `SolTechnology.Core.Hangfire` (docs / helper)
> **Semver / Effort:** MINOR · M

---

## Context — what it is

The harvested production app runs a hand-rolled background worker: **N dedicated tasks per priority
tier** (high / medium / low), each pulling from its own queue, building a per-message scope
(tenant + correlation), and marking messages processed / failed. The tiers are **cooperative** (no
true preemption), which the source code honestly documents.

## Decision context

We **do not** want to port that shape. The house preference is **Hangfire-backed durable background
jobs** ([background-processing architecture](../architecture/background-processing.md)), not a bespoke in-process worker
pool with its own scope/correlation plumbing.

## Why parked

- **Hangfire already covers the durable-background-job need** — a priority layer is an *enhancement*,
  not a gap.
- No current consumer is asking for tiered priority.
- Porting the hand-rolled pool would duplicate scope/correlation machinery that
  `Core.Hangfire` filters already provide.

## Sketch (when picked up) — the Hangfire-native design

Instead of an in-process task-per-tier pool, build priority natively on
`SolTechnology.Core.Hangfire`:

- **Dedicated Hangfire queues per tier** — e.g. `high` / `default` / `low`.
- **Route jobs** with the `[Queue("…")]` attribute (or an enqueue-time queue selector).
- **Per-queue worker counts / ordering** on the Hangfire server —
  `BackgroundJobServerOptions.Queues` listed **in priority order** so higher tiers are polled first.
- **Reuse** the existing scope and correlation handling from `Core.Hangfire` filters — no
  bespoke plumbing.

This keeps a single durable-jobs mechanism and adds priority as configuration, not as a parallel
subsystem.

## Guard-rails

- Don't reintroduce a second background-execution model alongside Hangfire.
- Priority ordering must be explicit (queue order), not implicit in worker counts.

## Graduation triggers

Create a dated feature (or make a small `Core.Hangfire` docs correction) when:

- A consumer needs genuine tiering (latency-sensitive jobs starved by bulk work).
- We observe head-of-line blocking on a single default queue in a real deployment.

## Related

- [Background-processing architecture](../architecture/background-processing.md).
- [Hangfire module docs](../Hangfire.md) — worker counts, queues, retry defaults.
- [Production pattern adoption wave 2](../features/2026-06-24-production-pattern-adoption-wave-2.md) — original assessment.

