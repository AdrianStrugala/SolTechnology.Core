---
spec: 2026-11-02-core-2-0-auid
step: 03
status: to-do
---

# Step 03: AUID — `SolTechnology.Core.AUID` namespace; no process-wide `TimeProvider`

## Summary
`Auid`, `AuidTypeConverter`, and `AuidJsonConverter` live in the global namespace, which pollutes
every consumer (`logging-cache#12`, `Auid.cs:14,441`, `AuidJsonConverter.cs:8`).
`Auid.TimeProvider` is a mutable process-wide static that any library can change and that makes
parallel tests interfere (`logging-cache#10`, `:40`, Guide §9.6). This step implements `C-08` and
`C-09`.

Former core step 44; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — `namespace SolTechnology.Core.AUID;`; delete `TimeProvider`; NEW `New(string? code, TimeProvider timeProvider)` and `New<T>(TimeProvider timeProvider)`; `AuidTypeConverter` sealed
- `src/SolTechnology.Core.AUID/AuidJsonConverter.cs` — EDIT — namespace; sealed
- `src/SolTechnology.Core.Tale/**`, `src/SolTechnology.Core.*/**` — EDIT — `using SolTechnology.Core.AUID;` where `Auid` is used (`grep -rln "\bAuid\b" src`)
- `sample-tale-code-apps/DreamTravel/**` — EDIT — same usings
- `tests/SolTechnology.Core.AUID.Tests/AuidNewTests.cs` — EDIT — the overflow test uses `FakeTimeProvider` through the overload
- `CLAUDE.md` — EDIT — `§11` AUID gotcha row mentions the namespace

## Changes
- `Auid.New(code)` keeps using `TimeProvider.System`.
- Persisted-contract check (premortem trigger): `Auid` values inside persisted Tale contexts serialize through `AuidJsonConverter` as strings, so the namespace move does not change stored JSON. Verify that no persisted field stores an assembly-qualified type name containing `Auid` (`grep -rn "AssemblyQualifiedName\|FullName" src/SolTechnology.Core.Tale`); if one does, stop and record it for the migration guide.
- `docs/AUID.md` usings are updated in step 05.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [ ] `dotnet test tests/SolTechnology.Core.AUID.Tests` and `dotnet test tests/SolTechnology.Core.Tale.Tests` are green.
- [ ] `grep -n "static TimeProvider" src/SolTechnology.Core.AUID/Auid.cs` prints nothing.
- [ ] A Tale instance JSON captured at `4097d47f` that contains an `Auid` still deserializes (test in `tests/SolTechnology.Core.Tale.Tests`).
- [ ] Premortem mitigations for the persisted contract are applied.

## Open questions
- none (`C-08`, `C-09` confirmed by the user on 2026-11-02)

## Deviations
