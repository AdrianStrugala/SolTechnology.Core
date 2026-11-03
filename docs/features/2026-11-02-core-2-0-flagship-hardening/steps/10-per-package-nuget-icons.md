---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 10
status: done
---

# Step 10: Per-package NuGet icons

## Summary
Every package shipped the old `docs/mini-logo.png`. AvroConvert and SolTechnology.DateTimeZone
use the current SolTechnology brand: the seven-chevron ring with a product glyph inside, on a
white 128 px NuGet icon. Added on 2026-11-03 at the user's request: each Core package gets its own
icon in the same system; the base `SolTechnology.Core` package uses the plain SolTechnology mark,
and the testing kits use their parent's glyph with a lab-flask badge (concept sheet approved by
the user on 2026-11-03).

## Affected components
- `sol-technology.github.io/scripts/brand-core.py` — NEW (website repository) — glyphs, concept sheet, white NuGet icons
- `docs/icons/<PackageId>.png` — NEW — 20 icons, one per packable project (new folder under `docs/`, confirmed by the user on 2026-11-03)
- `src/Directory.Build.props` — EDIT — `PackageIcon` is `docs\icon.png`, packed from `docs/icons/$(PackageId).png`
- `src/SolTechnology.Core.Guards/*.csproj`, `src/SolTechnology.Core.Scheduler/*.csproj` — unchanged (not packed; keep `mini-logo.png`)

## Changes
- Glyphs: Core = SolTechnology mark, CQRS = forked arrows, Tale = open book, Logging = log lines, HTTP = globe, Api = braces, Authentication = key, Cache = lightning, AUID = `#`, SQL = database, Blob = cloud, MessageBus = envelope, Hangfire = flame, Testing = flask; `*.Testing` kits = parent glyph + flask badge (Redis.Testing uses the Cache glyph).
- The Authentication → Auth rename (api-auth step 01) moves `docs/icons/SolTechnology.Core.Authentication.png` with the project.

## Acceptance criteria
- [x] Every packed `SolTechnology.Core*.2.0.0.nupkg` contains `docs/icon.png` byte-identical to `docs/icons/<PackageId>.png`, and its nuspec declares `<icon>docs/icon.png</icon>` (20 of 20).
- [x] `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none

## Deviations
