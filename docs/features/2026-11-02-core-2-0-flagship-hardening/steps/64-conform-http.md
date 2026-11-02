---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 64
status: to-do
---

# Step 64: Guide conformance — HTTP

## Summary
Several HTTP files exceed the size caps (`http-api#24`): `RequestBuilder.cs` (477),
`HTTP/ModuleInstaller.cs` (241), `HttpPolicyFactory.cs` (225, with `Configure` at about 160
lines), and `ServiceCallErrorMapper.cs` (167). Hand-written constructor capture,
`static class` + `this`, essay comments, and unbraced `if`s also need fixing
(`http-api#23,25`). This is a behaviour-preserving refactor. The Api, Authentication, and
HTTP.Testing half follows in step 65 (split from the former single HTTP/Api conformance step
during plan review, RQ11).

## Affected components
- `src/SolTechnology.Core.HTTP/RequestBuilder.cs` — EDIT — partials `RequestBuilder.Content.cs`, `RequestBuilder.Send.cs`, `RequestBuilder.TrySend.cs`, `RequestBuilder.BodyCapture.cs` (NEW)
- `src/SolTechnology.Core.HTTP/HttpPolicyFactory.cs` — EDIT — `RetryStrategyOptionsBuilder.cs`, `CircuitBreakerOptionsBuilder.cs` (NEW, internal)
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — extract internal helpers below 150 lines
- `src/SolTechnology.Core.HTTP/ServiceCallErrorMapper.cs` — EDIT — below 150 lines (extract the deserialization-failure mapping into an internal helper file)
- `src/SolTechnology.Core.HTTP/**/*Extensions.cs` — EDIT — `extension` blocks

## Changes
- No public or protected signature change. ModuleInstaller edits are mechanical (the premortem `di.md` checklist still applies).

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.HTTP -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green; DreamTravel component tests are green.
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
