---
adr: 010-production-pattern-adoption-programme
step: 06 of 10
status: to-do
---

# Step 06: Author the MessageBus child ADR (M1, M2, M3, M4)

## Summary
Author the child ADR (provisional ADR-015) that adds a broker-agnostic `IMessageProcessor<TEvent>`
seam, scope-per-message + correlation propagation, dead-letter-with-reason, and a shared processor
pipeline to `SolTechnology.Core.MessageBus`. This is the largest design call. Depends on ADR-011
(L1 correlation), open question Q3 (RabbitMQ scope), and Q4 (single `Result`). Seeds its own plan and
premortem.

## Affected components
- `docs/adr/<next>-messagebus-broker-agnostic.md` — the child ADR.
- `docs/adr/<next>-messagebus-broker-agnostic/` — its plan folder.

## Details
- **M1 — broker-agnostic seam.** `IMessageProcessor<TEvent>` + a `MessageBrokerType` switch
  (`Disabled`/`RabbitMq`/`ServiceBus`); consumers bind to the seam, not the transport. The `Disabled`
  switch no-ops messaging in isolated/test environments.
- **M2 — scope + correlation.** Scope-per-message is already present in
  `MessageBusReceiver` (`CreateAsyncScope`); the gap is reading `message.CorrelationId` (or
  generating one) into `ICorrelationIdService` (from ADR-011) and opening a log scope for the
  duration. Make this the documented default.
- **M3 — dead-letter-with-reason.** Already present in `MessageBusReceiver` (dead-letters with a
  reason); formalize it as the documented poison-message default.
- **M4 — shared pipeline.** Factor the deserialize → scope → correlation → process → ack/deadletter
  pipeline so a future transport differs only in transport-specific ack/consume.
- **Dependency impact (`CLAUDE.md` §1):** `RabbitMQ.Client` is added only if Q3 puts the transport in
  scope; otherwise the seam ships with ServiceBus only. Report in the child ADR.

## Acceptance criteria
- Child ADR authored with blue/red + premortem-as-final-step; semver **MINOR** (additive seam) or
  **MAJOR** if the receiver's public surface is reshaped — classify against the diff.
- Correlation propagation into `MessageBusReceiver` is part of the plan (consumes ADR-011).
- RabbitMQ in/out-of-scope decision (Q3) and any `RabbitMQ.Client` impact recorded.
- Index row added in `docs/adr/README.md`.

## Open questions
- Q3 (RabbitMQ scope) and Q4 (single `Result`) — resolved in step 01; ADR-011 must be authored first.

