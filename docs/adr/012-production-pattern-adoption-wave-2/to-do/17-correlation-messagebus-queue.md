---
adr: 012-production-pattern-adoption-wave-2
step: 17 of 24
status: to-do
---

# Step 17: B1.4 — Queue correlation propagation (`Core.MessageBus`)

## Summary
Propagate correlation onto bus/queue messages: the **platform** correlation id flows as a message
property and is restored on the receiver; the **client** correlation id deliberately does **not**
flow (handlers run out of client context). Builds on ADR-010's existing publisher/receiver
correlation wiring and the two-level model from step 14.

## Affected components
- `src/SolTechnology.Core.MessageBus/` publisher — stamp the platform correlation id as a message
  application property when publishing (extend the ADR-010 `MessagePublisher` correlation stamping
  to the two-level model: platform only).
- `src/SolTechnology.Core.MessageBus/` receiver — read the platform id, set it on
  `ICorrelationIdService`, and push it to the log scope (ADR-010 `MessageBusReceiver` already does
  the single-id version; align to platform id and ensure the client id is **not** read).
- `docs/Bus.md` — document "platform id flows, client id does not" for queue messages.
- `tests/SolTechnology.Core.MessageBus.Tests/` — publish-then-receive carries platform id; client id
  is absent on the receiver side.

## Details
- **The rule (acceptance-critical, guard-rail):** queue handlers are "out of client context", so the
  client correlation id must NOT be written onto the message or restored on the receiver. Only the
  platform id flows — it ties the async work back to the originating request in the platform's own
  trace without leaking a caller-facing token into a context where it has no meaning.
- Reuse the existing ADR-010 mechanism (publisher stamps `CorrelationId`; receiver reads + sets +
  pushes scope). This step changes *which* id flows (platform) and asserts the client id is excluded
  under the two-level model — it is a small, surgical change, not a new pipeline.
- **`Core.MessageBus` `TreatWarningsAsErrors=false`** today — keep additions warning-clean.
- Note ADR-010 step 05's preserved deviation: the in-process pipeline was intentionally **not**
  extracted (`HandleMessageAsync` is under budget). Do not re-open that — keep this change inside the
  existing publish/receive methods.

## Acceptance criteria
- Published messages carry the platform correlation id as an application property.
- The receiver restores the platform id onto `ICorrelationIdService` and the log scope.
- The client correlation id is **never** written to the message nor restored on the receiver
  (asserted by test).
- `docs/Bus.md` documents the flow rule.

## Open questions
- none — the "platform flows, client does not" rule is fixed by the harvest and ADR-012 guard-rails.

