---
spec: 2026-11-02-core-2-0-messagebus
step: 08
status: to-do
---

# Step 08: Log templates — `[{Placeholder}]` in MessageBus (L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Templates in the MessageBus receiver and publisher are not (`infra#20`). Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0 accepts the change (D2), and it is listed in the release notes as L-03.

Former core step 70 (MessageBus part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` (`:38-40,96-98,111-113,145-147,163-165`; locations moved by step 07), `Publish/MessagePublisher.cs` (`:40,46`) — EDIT — `[{MessageType}]`, `[{Entity}]`

## Changes
- Property names stay the same.
- A check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.MessageBus --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [ ] The grep check above prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker) (log-asserting tests updated).

## Open questions
- none

## Deviations
