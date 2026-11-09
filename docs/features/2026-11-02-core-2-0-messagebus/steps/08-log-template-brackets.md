---
spec: 2026-11-02-core-2-0-messagebus
step: 08
status: done
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
- [x] The grep check above prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` is green (Docker) (log-asserting tests updated).

## Open questions
- none

## Deviations

### Template check and pinning tests

**Original plan:** the `grep -rnE` check from Changes; update the log-asserting tests.

**Actual implementation:** every MessageBus template sits on the line after its `Log*(` call, so the planned
single-line regex matched none of the 11 old templates and proves nothing. The check that ran is a multi-line Perl
scan of each `Log*( ... );` call for a placeholder without `[` before or `]` after it: 11 hits on `7701c4e1`, none
after the change. The locations also moved in step 07: nine receiver templates now live in `MessageBusReceiver.cs`
and `MessageSettler.cs`. No test asserted these templates, so new tests pin them:
`MessageSettlerTests.Settle_EveryOutcome_LogsBracketedTemplate` (eight settlement templates through a substituted
`ServiceBusReceiver`), `MessageBusReceiverTests.PublishReceiveAndStop_LogTemplates_BracketEveryPlaceholder`
(start, publish, handled and failed-stop templates on the emulator), and
`MessageBusReceiverTests.MissingQueue_LogTemplates_BracketEveryPlaceholder` (publish failure and processor error on a
queue the emulator does not have). Both emulator tests also fail on any unbracketed MessageBus template. Eight of the
ten new cases failed before the change; the two that passed cover templates already bracketed in step 03. The
receiver test host now logs from `Trace`, so the debug templates are captured.
