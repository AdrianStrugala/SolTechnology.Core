---
name: dependency-audit
description: Resolve a NU1901–NU1904 CVE warning, an NU1605 downgrade, or a transitive vulnerability in SolTechnology.Core. Drives the parent-lookup → fix-at-source → override-only-as-last-resort flow from CLAUDE.md §5. Companion to `package-management` (which only handles "add a package").
---

# Dependency Audit

Drive the CVE / downgrade resolution flow from
[`CLAUDE.md §5`](../../../CLAUDE.md). Where `package-management` answers *"which version do I
pin?"*, this skill answers *"how do I fix a vulnerability or downgrade warning at source?"*.

## When to use

- `dotnet restore` / `dotnet build` emits **NU1901**, NU1902, NU1903 or NU1904.
- `dotnet build` emits **NU1605** (package downgrade detected across projects).
- A transitive dependency is flagged on GitHub / Snyk / Dependabot.
- A consumer reports a CVE pulled in via `SolTechnology.Core.<Module>`.
- Pipeline `publishPackages.yml` fails with a NuGet audit error.

## When NOT to use

- Adding a brand-new package — use [`package-management`](../package-management/SKILL.md).
- **NU1900** alone (audit data unreachable). Per `CLAUDE.md §5`, NU1900 is **not** a CVE;
  it means the audit feed is offline. Fix your feed; do not touch `Directory.Build.props`.
- Bumping a package for a non-security reason — use `package-management`.

## Critical rules — from `CLAUDE.md §5`

- **Fix at source, never mask.** A direct `PackageReference` override hides a vulnerable parent
  for one project at a time; the next consumer pulls the vulnerable parent again.
- **Bump the parent at the seam.** Find the *one* project that directly references the vulnerable
  parent. Transitive children inherit through `ProjectReference`.
- **Override is a last resort.** Only when the parent has no patched version and removing it is
  not viable. Document the reason in an inline comment next to the override.
- **No `NU190x` masking.** Adding the warning to `<NoWarn>` or `<WarningsNotAsErrors>` for a
  real CVE is a `CLAUDE.md §1` forbidden action.

## Procedure

### 1. Identify the failing code(s)

```bash
dotnet build SolTechnology.Core.slnx 2>&1 | grep -E 'NU190[1-5]'
```

Expected codes:

| Code | Severity | Meaning |
|---|---|---|
| NU1901 | Low | Known low-severity vulnerability |
| NU1902 | Moderate | Known moderate-severity vulnerability |
| NU1903 | High | Known high-severity vulnerability |
| NU1904 | Critical | Known critical-severity vulnerability |
| NU1605 | Downgrade | Project pulls a lower version than a transitive dep demands |

### 2. Find the parent — `dotnet list package --include-transitive`

For each failing project:

```bash
dotnet list <project.csproj> package --include-transitive --vulnerable
```

Read the output tree. The vulnerable leaf is named; the parent is the row directly above it
that *directly* references the leaf. The parent is the seam.

For NU1605, the `--include-transitive` flag shows which transitive demand wins; the failing
project must pin at least that version.

### 3. Cross-check the CVE

If a CVE is reported, use the Copilot `validate_cves` tool or the GitHub Advisory link printed
by NuGet:

- Confirm the CVE applies to the **resolved** version, not a guess.
- Note the minimum patched version. That is your bump target.

### 4. Fix at source — preferred path

Pick the first option that applies:

1. **Bump the parent** to a patched version. Verify the new version exists on
   [nuget.org](https://www.nuget.org/) and matches
   [`package-management/references/canonical-versions.md`](../package-management/references/canonical-versions.md)
   when listed there. Update the canonical table if the bumped version is new or higher.

   ```xml
   <!-- Before -->
   <PackageReference Include="Hangfire.Core" Version="1.8.16" />
   <!-- After — bump fixes vulnerable Newtonsoft.Json transitively -->
   <PackageReference Include="Hangfire.Core" Version="1.8.22" />
   ```

2. **Remove the parent** if it is unused. `dotnet remove package <Parent>` then verify the build.

3. **Migrate the SDK family.** Some parents are dead (e.g. `Microsoft.Azure.ServiceBus` →
   `Azure.Messaging.ServiceBus`). Migration is its own ADR and triggers
   [`implementation-planning`](../../agents/implementation-planning.agent.md). Do not migrate
   inside this skill.

### 5. Override — last resort only

If the parent has no patched version and removal / migration is not viable in the current PR:

```xml
<!-- DreamTravel.Infrastructure.csproj -->
<!-- Hangfire.Core 1.8.x has CVE-XXXX in transitive Newtonsoft.Json <13.0.4.
     Override pinned here at the seam. Remove once Hangfire 1.9 ships. -->
<PackageReference Include="Hangfire.Core" Version="1.8.16" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

Rules for the override:

- Lives in the **seam project** (the one that directly references the vulnerable parent),
  not at every leaf.
- Comment cites the CVE and the removal trigger.
- File a follow-up issue or ADR if the override has to live longer than one release cycle.

### 6. Verify

```bash
dotnet build SolTechnology.Core.slnx 2>&1 | grep -E 'NU190[1-5]'
# expected: empty
```

If the failing project is in a sample app:

```bash
cd sample-tale-code-apps/<App>
dotnet build 2>&1 | grep -E 'NU190[1-5]'
```

### 7. Document the fix

Update the change's commit body via [`commit-message`](../commit-message/SKILL.md):

```
fix(<scope>): bump Hangfire.Core to 1.8.22 to clear NU1904 in Newtonsoft.Json

Hangfire 1.8.16 pulled Newtonsoft.Json 12.0.3 transitively, flagged by
NU1904 / GHSA-... . Bumping the parent at the DreamTravel.Infrastructure seam
clears the warning for every downstream project via ProjectReference.

Semver: PATCH
```

If a CVE shaped a public API change (e.g. forced a serialiser swap), file an ADR.

## Pre-yield checklist

- [ ] `dotnet build SolTechnology.Core.slnx | grep NU190` is empty.
- [ ] The fix is at the parent seam, not at every leaf.
- [ ] No new entry in `<NoWarn>` or `<WarningsNotAsErrors>` for an `NU190x` code.
- [ ] `package-management/references/canonical-versions.md` reflects the new version when the
      bumped package is listed there.
- [ ] Commit body cites the CVE / advisory and the removal trigger if an override was used.
- [ ] Sample-app build also green if the fix touches a project they depend on transitively.

## Constraints

- DO NOT mask `NU190x` with `<NoWarn>`, `<WarningsNotAsErrors>`, or pragma suppression. That is
  `CLAUDE.md §1` forbidden.
- DO NOT touch `src/Directory.Build.props` to silence audit warnings. The only legitimate audit
  exception there is **NU1900** (audit feed offline) which is unrelated to CVEs.
- DO NOT migrate an SDK family inside this skill (e.g. `Microsoft.Azure.ServiceBus` →
  `Azure.Messaging.ServiceBus`). That is an ADR-driven change — hand off to
  [`implementation-planning`](../../agents/implementation-planning.agent.md).
- DO NOT add an override without an inline comment citing the CVE and removal trigger.
- DO NOT bump a package without checking
  [`package-management/references/canonical-versions.md`](../package-management/references/canonical-versions.md).
  A CVE fix is no excuse for drift.
- DO NOT improvise a freehand audit flow when this skill is unavailable. STOP and tell the user
  `dependency-audit` is required (CLAUDE.md §2). The masking-by-override anti-pattern is exactly
  what this skill exists to prevent.

