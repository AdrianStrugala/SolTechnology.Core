---
adr: 010-production-pattern-adoption-programme
step: 06 of 07
status: to-do
---
# Step 06: Testing — `UtcDateTimeSpecimen` + composable data attribute

## Summary
Add UTC DateTime specimen and extend `AutoNSubstituteDataAttribute` to accept composable customizations.

## Affected components
- `src/SolTechnology.Core.Testing/Customizations/UtcDateTimeSpecimen.cs` — new (T1)
- `src/SolTechnology.Core.Testing/AutoNSubstituteDataAttribute.cs` — edit (T2)

## Details
- **T1:** `ISpecimenBuilder` that generates `DateTime` with `DateTimeKind.Utc`.
- **T2:** Add `params Type[] customizations` to ctor. Layer `UtcDateTimeSpecimen` into default set. Zero-arg usage stays source-compatible.

## Acceptance criteria
- Generated `DateTime` has `Kind == Utc`
- `[AutoNSubstituteData(typeof(MyCustomization))]` layers the custom customization
- Existing tests compile without changes
- `dotnet build` green

## Open questions
- none

