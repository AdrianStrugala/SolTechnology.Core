---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 01
status: done
---

# Step 01: Clear NU1903 — Testcontainers and DreamTravel parent bumps, Pkcs override in SQL

## Summary
`master` fails restore with `NU1903` from three sources (`C2`):

- `Testcontainers@4.12.0` pulls SSH.NET 2025.1.0 (GHSA-q939-rpr3-3284, GHSA-mggc-4xg6-vcxf).
  `Testcontainers@4.14.0`+ depends on `SSH.NET@2026.0.0`; `4.16.0` exists for the whole family.
- `Microsoft.Data.SqlClient@7.0.1` pulls `System.Security.Cryptography.Pkcs@9.0.13`, hit by
  GHSA-846f-f764-76mx (CVE-2026-84008, High; vulnerable `[9.0.0, 9.0.20]`, `[10.0.0, 10.0.12]`,
  `[8.0.0, 8.0.1]`). No SqlClient release fixes it (7.1.1 still declares Pkcs `9.0.18`).
  `SolTechnology.Core.SQL` and `SQL.Testing` build with `TreatWarningsAsErrors`, so it is an error.
- DreamTravel (warnings only, but the brief's `NU190[1-4]` grep covers the whole slnx):
  `MessagePack@2.5.302` via `Aspire.Hosting.* 13.4.6` → `StreamJsonRpc 2.25.29`
  (GHSA-qhrr-8q5h-9q3h, fixed in 2.5.303), and the stale `SQLitePCLRaw` suppress.

Per `CLAUDE.md §6`, parents are bumped at their seam. The Pkcs fix is a leaf override inside two
shipped packages; the user confirmed it on 2026-11-02 (`CLAUDE.md §2/§6`, see the brief's
plan-review answers, RQ1).

## Affected components
- `src/SolTechnology.Core.Testing/SolTechnology.Core.Testing.csproj` — EDIT — `Testcontainers` 4.12.0 → 4.16.0
- `src/SolTechnology.Core.SQL.Testing/SolTechnology.Core.SQL.Testing.csproj` — EDIT — `Testcontainers`, `Testcontainers.PostgreSql` → 4.16.0; NEW `PackageReference System.Security.Cryptography.Pkcs 10.0.13` with a one-line comment
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — EDIT — NEW `PackageReference System.Security.Cryptography.Pkcs 10.0.13` with a one-line comment
- `src/SolTechnology.Core.Redis.Testing/SolTechnology.Core.Redis.Testing.csproj` — EDIT — `Testcontainers.Redis` → 4.16.0
- `src/SolTechnology.Core.Blob.Testing/SolTechnology.Core.Blob.Testing.csproj` — EDIT — `Testcontainers.Azurite` → 4.16.0
- `src/SolTechnology.Core.ServiceBus.Testing/SolTechnology.Core.ServiceBus.Testing.csproj` — EDIT — `Testcontainers.ServiceBus` → 4.16.0
- `src/SolTechnology.Core.Testing/Containers/ContainerLifecycleHelper.cs` — EDIT — only if the 4.16 builder API changed
- `sample-tale-code-apps/DreamTravel/src/DreamTravel.Aspire/DreamTravel.Aspire.csproj` — EDIT — `Aspire.AppHost.Sdk`, `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer` 13.4.6 → 13.6.4
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.SQLite/DreamTravel.SQLite.csproj` — EDIT — `SQLitePCLRaw.bundle_green 2.1.11` → `SQLitePCLRaw.bundle_e_sqlite3 2.1.14`; delete the `NuGetAuditSuppress` block (`:6-12`)
- `sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` — EDIT — delete the `NuGetAuditSuppress` block (`:24-27`)
- `.github/skills/package-management/references/canonical-versions.md` — EDIT — `Testcontainers*` rows (`:76-88`) → 4.16.0; NEW `System.Security.Cryptography.Pkcs` row (override, why, removal trigger); `Aspire.Hosting.*` rows (`:161-162`) → 13.6.4; `SQLitePCLRaw` row (`:79-80`) → `bundle_e_sqlite3 2.1.14`

## Changes
- Run the [`dependency-audit`](../../../../.github/skills/dependency-audit/SKILL.md) skill. Use `dotnet list SolTechnology.Core.slnx package --vulnerable --include-transitive` to confirm the parent seams before and after.
- Testcontainers: bump every `Testcontainers*` `PackageReference` to `4.16.0` (floor `4.14.0`).
- Pkcs (leaf override, user-confirmed 2026-11-02):
  - `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="10.0.13" />` in `SolTechnology.Core.SQL` and `SolTechnology.Core.SQL.Testing`.
  - `10.0.13` is the first patched `10.0.x` (advisory range `[10.0.0, 10.0.12]`); it exists on nuget.org (verified 2026-11-02).
  - Comment above each line: `<!-- GHSA-846f-f764-76mx via Microsoft.Data.SqlClient; remove when SqlClient depends on a patched Pkcs. -->`.
  - The follow-up "remove the Pkcs override when SqlClient bumps" is recorded in the brief's `Follow-ups`.
- DreamTravel parents:
  - `Aspire.Hosting.* 13.6.4` depends on `MessagePack 2.5.307` directly, which clears GHSA-qhrr-8q5h-9q3h without touching `StreamJsonRpc`.
  - `Aspire.Hosting.SqlServer 13.6.4` still declares Pkcs `10.0.12`. `DreamTravel.Aspire` references `SolTechnology.Core.SQL`, so the SQL override must lift it to `10.0.13`. Confirm with `dotnet list sample-tale-code-apps/DreamTravel/src/DreamTravel.Aspire package --include-transitive | grep -i pkcs`. If it does not resolve to `10.0.13`, stop and ask; no further leaf override without confirmation.
  - `SQLitePCLRaw.bundle_green` has no release after 2.1.11. `SQLitePCLRaw.bundle_e_sqlite3 2.1.14` pulls `lib.e_sqlite3 2.1.14`, outside GHSA-2m69-gcr7-jv3q (`<= 2.1.11`). If the bundle swap breaks SQLite start-up, record a deviation and keep the suppress.
- Commits:
  - `fix(testing): bump Testcontainers to 4.16.0 to clear NU1903 in SSH.NET`.
  - `fix(sql): pin System.Security.Cryptography.Pkcs 10.0.13 over SqlClient's vulnerable transitive`.
  - `chore(dreamtravel): bump Aspire to 13.6.4 and SQLitePCLRaw bundle; drop stale audit suppress`.
  - Advisory IDs go in each body.

## Acceptance criteria
- [x] `dotnet restore SolTechnology.Core.slnx` succeeds from a clean clone (`git clean -xdf` first).
- [x] `dotnet build SolTechnology.Core.slnx 2>&1 | grep -E "NU190[1-4]|NU1605"` prints nothing.
- [x] `dotnet list SolTechnology.Core.slnx package --vulnerable --include-transitive` lists no entry.
- [x] `grep -rn "NuGetAuditSuppress" --include=*.csproj .` prints nothing (or a deviation records why the SQLite suppress stays).
- [x] Container check: `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green (Docker). No test project under `tests/` is container-backed at `4097d47f`.
- [x] `canonical-versions.md` rows match the csproj versions.

## Open questions
- none (Pkcs leaf override confirmed by the user on 2026-11-02, RQ1)

## Deviations
