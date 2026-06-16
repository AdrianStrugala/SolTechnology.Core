---
adr: 010-production-pattern-adoption-programme
step: 01 of 07
status: to-do
---
# Step 01: Logging — correlation docs + scope helpers + PII masking

## Summary
Document `ICorrelationIdService` as canonical, add `PushToScope` extensions, add `PiiMask` helpers + `[Masked]` attribute.

## Affected components
- `docs/Log.md` — add Correlation section (L1)
- `src/SolTechnology.Core.Logging/Extensions/LoggerScopeExtensions.cs` — new (L2)
- `src/SolTechnology.Core.Logging/Masking/PiiMask.cs` — new (L3)
- `src/SolTechnology.Core.Logging/Masking/MaskMode.cs` — new (L3)
- `src/SolTechnology.Core.Logging/Masking/MaskedAttribute.cs` — new (L3)
- CQRS pipeline behavior that reads `[LogScope]` — wire `[Masked]` support

## Details
- **L1:** Declare `ICorrelationIdService` as the single correlation primitive. Document resolution order (Activity → X-Correlation-Id → GUID). Note MessageBus gap (M2, step 05).
- **L2:** `PushToScope(this ILogger, string key, object? value)` and tuple overload. Uses `Dictionary<string,object?>` only.
- **L3:** `PiiMask.Full(value)` → `"***MASKED***"`. `PiiMask.Partial(value, keepChars)` → keeps first/last N. Guard-rail: never returns empty. `[Masked(MaskMode.Partial, keepChars: 3)]` attribute wired into `[LogScope]` enricher.

## Acceptance criteria
- `docs/Log.md` has Correlation section
- `LoggerScopeExtensions` compiles
- `PiiMask.Full(null)` returns `"***MASKED***"`; `PiiMask.Partial("1234567890", 3)` returns `"123***890"`
- `[LogScope, Masked]` masks value before pushing to scope
- Tests pass

## Open questions
- none

