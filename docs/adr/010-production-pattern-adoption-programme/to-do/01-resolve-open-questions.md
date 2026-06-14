---
adr: 010-production-pattern-adoption-programme
step: 01 of 10
status: to-do
---

# Step 01: Resolve programme open questions

## Summary
Get maintainer decisions on the five open questions recorded in ADR-010 before any child ADR is
authored. This is the first step because four of the six workstreams are blocked on an answer that
cannot be guessed (Redis client, EF companion, RabbitMQ scope, single-`Result` confirmation, and the
`ValidateOnStart` behaviour change). It is a decision-gathering PR — no code, no child ADR.

## Affected components
- `docs/adr/010-production-pattern-adoption-programme.md` — append the resolved answers (Amendment
  note; the ADR is still `Proposed`, so this is editing a draft, not a published ADR).

## Details
- **Q1 — Redis client (blocks ADR-012):** `Microsoft.Extensions.Caching.StackExchangeRedis`
  (`IDistributedCache` implementation, matches "over `IDistributedCache`") vs raw
  `StackExchange.Redis`. Repo already pins `StackExchange.Redis` 2.8.16 in the test companion.
- **Q2 — EF companion (blocks ADR-018):** introduce `SolTechnology.Core.SQL.EntityFramework`
  (new `src/` package, gated by `CLAUDE.md` §1) or keep `EntityBase` guidance documentation-only.
- **Q3 — RabbitMQ transport (blocks ADR-015):** ship a working `RabbitMQ.Client` transport now, or
  land the broker-agnostic seam + `MessageBrokerType` switch with ServiceBus only and defer RabbitMQ.
- **Q4 — single `Result` (blocks ADR-014):** confirm `SolTechnology.Core.CQRS.Result` is canonical
  (no second type exists in the repo) and the only gap is adding `MapError`.
- **Q5 — `ValidateOnStart` everywhere (shapes ADR-014):** confirm turning bad config into a
  host-start failure for `AddCache` / `AddSQL` / `AddMessageBus` is acceptable.

## Acceptance criteria
- Each of Q1–Q5 has a recorded decision appended to ADR-010.
- Each blocked child ADR (012, 013, 014, 015, 018) is marked *go* or *deferred* based on its answer.

## Open questions
- none — this step IS the open-questions resolution.

