---
spec: 2026-11-02-core-2-0-http
step: 09
status: done
---

# Step 09: Guide conformance — HTTP

## Summary
Several HTTP files exceed the size caps (`http-api#24`): `RequestBuilder.cs` (477),
`HTTP/ModuleInstaller.cs` (241), `HttpPolicyFactory.cs` (225, with `Configure` at about 160
lines), and `ServiceCallErrorMapper.cs` (167). Hand-written constructor capture,
`static class` + `this`, essay comments, and unbraced `if`s also need fixing
(`http-api#23,25`). This is a behaviour-preserving refactor. The Api and Authentication half
follows in api-auth step 09 and the HTTP.Testing half in testing-kits step 06 (split from the
former single HTTP/Api conformance step during plan review, RQ11, and again per library on
2026-11-02).

Former core step 64; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.HTTP/RequestBuilder.cs` — EDIT — partials `RequestBuilder.Content.cs`, `RequestBuilder.Send.cs`, `RequestBuilder.TrySend.cs`, `RequestBuilder.BodyCapture.cs` (NEW)
- `src/SolTechnology.Core.HTTP/HttpPolicyFactory.cs` — EDIT — `RetryStrategyOptionsBuilder.cs`, `CircuitBreakerOptionsBuilder.cs` (NEW, internal)
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — EDIT — extract internal helpers below 150 lines
- `src/SolTechnology.Core.HTTP/ServiceCallErrorMapper.cs` — EDIT — below 150 lines (extract the deserialization-failure mapping into an internal helper file)
- `src/SolTechnology.Core.HTTP/**/*Extensions.cs` — EDIT — `extension` blocks

## Changes
- No public or protected signature change. ModuleInstaller edits are mechanical (the premortem `di.md` checklist still applies).

## Acceptance criteria
- [x] `find src/SolTechnology.Core.HTTP -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green; DreamTravel component tests are green.
- [x] Public API diff is empty.
- [x] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations

### What was extracted, and the public API diff

**Original plan:** Partials `RequestBuilder.Content/Send/TrySend/BodyCapture.cs`; internal `RetryStrategyOptionsBuilder` and `CircuitBreakerOptionsBuilder`; internal helpers for `ModuleInstaller.cs`; the deserialization-failure mapping of `ServiceCallErrorMapper` into an internal helper; `extension` blocks in `**/*Extensions.cs`; public API diff empty.

**Actual implementation:** `RequestBuilder` became five partial files (largest 126 lines); its private `ThrowOnFailureAsync`/`TryReadCappedBodyAsync` lost the suffix (Guide §10) and `TrySend` delegates the success read to a private `ReadSuccess`. `HttpPolicyFactory` (82 lines) keeps the outer timeout, the per-attempt timeout and the shared `IsTransientFailure`, uses its primary-constructor parameters directly instead of hand-written `_logger`/`_metrics` fields, and builds the two strategies through the new internal static builders. `ModuleInstaller` delegates the three options chains to internal `HttpClientOptionsRegistration` and the in-memory policy projection to internal `PolicyConfigurationProjector`, and declares `AddSolHTTPClient` in an `extension(IServiceCollection)` block like `AddSolCQRS`. `FromDeserializationFailure` is public and three lines long, so the ProblemDetails body parsing (`title`/`message`/`detail`, `recoverable`) moved instead, into internal `ProblemDetailsBody`. `HttpClientExtensions` and `UpstreamHttpHealthCheckExtensions` use `extension` blocks. Multi-line `//` comments in the touched files became one line. No test file changed; `find … | awk '$1>150'` prints nothing. A reflection dump of every public/protected member before and after the step differs only by the compiler-generated `<G>$…`/`<M>$…` extension marker types and `<Extension>$` skeleton methods of the three `extension` blocks; every callable signature, parameter name and default is unchanged. The `di.md` checklist holds for `ModuleInstaller.cs`: registrations, lifetimes, order and validation are unchanged.
