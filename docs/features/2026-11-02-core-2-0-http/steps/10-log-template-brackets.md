---
spec: 2026-11-02-core-2-0-http
step: 10
status: done
---

# Step 10: Log templates — `[{Placeholder}]` in HTTP (L-03)

## Summary
Guide §11 requires every placeholder to be wrapped in brackets. Templates in HTTP are not (`http-api#18`). Guide §15 marks this as "report only" because templates are a contract consumers query. 2.0 accepts the change (D2), and it is listed in the release notes as L-03.

Former core step 70 (HTTP part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — `:143` `TimeoutSeconds=[{Seconds}]s`
- `src/SolTechnology.Core.HTTP/HealthChecks/BaseUpstreamServiceHealthCheck.cs` — EDIT — `:110,119,126` `{HealthPath}` → `[{UpstreamURL}]`

## Changes
- Property names stay the same except `HealthPath` → `UpstreamURL`, which is listed in the release notes.
- A check for unbracketed placeholders: `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.HTTP --include=*.cs` prints nothing (tune the regex in the PR; `[LoggerMessage]` templates included).

## Acceptance criteria
- [x] The grep check above prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green (log-asserting tests updated).
- [x] The core brief's L-03 entry lists the `HealthPath` → `UpstreamURL` rename; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### Tuned template check and the tests that pin it

**Original plan:** `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.HTTP --include=*.cs` prints nothing (tune the regex in the PR).

**Actual implementation:** That single-line grep printed nothing even before the fix, because every unbracketed template sat in a call split over several lines. The check used instead reads each `Log*(…)` call across lines and flags any `{Placeholder}` not wrapped in `[]`: `perl -0777 -ne 'while(/\.Log\w*\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\)/g){my $c=$&; while($c=~/"([^"]*)"/g){my $l=$1; print "$ARGV: $l\n" if $l=~/(?<!\[)\{[A-Z]\w*\}|\{[A-Z]\w*\}(?!\])/}}' $(find src/SolTechnology.Core.HTTP -name "*.cs" -not -path "*/obj/*")`. On the previous commit it lists exactly the four templates of this step; now it prints nothing. HTTP has no `[LoggerMessage]` templates. No existing test asserted a log template, so `CheckHealth_Failure_LogsUpstreamURLInBrackets` (three failure paths) and `AddSolHTTPClient_TimeoutSecondsWithPolly_LogsBracketedWarning` were added with a small capturing logger; all four cases failed before the change. The core brief's L-03 HTTP bullet now says the property is renamed and log queries must switch to `UpstreamURL`. The `ModuleInstaller.cs` edit changes only the template text (`di.md` checklist unaffected).
