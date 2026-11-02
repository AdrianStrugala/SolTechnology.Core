---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 56
status: to-do
---

# Step 56: XML docs shipped — HTTP, HTTP.Testing, Api, API.Testing, Authentication

## Summary
This step continues step 55 for the HTTP and API packages. It enables
`GenerateDocumentationFile` per project and documents every public/protected member reported by
`CS1591`.

## Affected components
- `src/SolTechnology.Core.HTTP/SolTechnology.Core.HTTP.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.HTTP.Testing/SolTechnology.Core.HTTP.Testing.csproj` — EDIT — same
- `src/SolTechnology.Core.API/SolTechnology.Core.API.csproj` — EDIT — same
- `src/SolTechnology.Core.API.Testing/SolTechnology.Core.API.Testing.csproj` — EDIT — same
- `src/SolTechnology.Core.Authentication/SolTechnology.Core.Authentication.csproj` — EDIT — same
- `src/SolTechnology.Core.HTTP/**/*.cs`, `src/SolTechnology.Core.HTTP.Testing/**/*.cs`, `src/SolTechnology.Core.API/**/*.cs`, `src/SolTechnology.Core.API.Testing/**/*.cs`, `src/SolTechnology.Core.Authentication/**/*.cs` — EDIT — `<summary>` blocks

## Changes
- Same rules as step 55.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] `dotnet pack` of each of the 5 projects contains the `.xml` doc file.

## Open questions
- none

## Deviations
