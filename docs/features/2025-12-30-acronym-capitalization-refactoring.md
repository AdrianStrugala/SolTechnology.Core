---
status: completed
created: 2025-12-30
completed: 2025-12-30
---

# Acronym Capitalization Refactoring

> Historical delivery record. It may not describe the current system.

## Goal

Normalize acronym casing across source identifiers and align public package naming where
compatibility allowed it.

## Context

The codebase mixed `Api`/`API`, `Sql`/`SQL`, and `ApiClient`/`HTTP` terminology. The work also
introduced repository-wide naming inspections.

Microsoft's naming guidance distinguishes two-letter acronyms from longer ones, but the repository
selected one local rule: known acronyms use uppercase letters regardless of length. The intended
benefits were immediate visual recognition and one convention that did not require contributors to
count characters.

Examples identified during planning included:

| Previous form | Intended form |
|---|---|
| `ApiClient` | `APIClient` or a domain-specific HTTP name |
| `SqlConnection` | `SQLConnection` |
| `XmlDocument` | `XMLDocument` |
| `SolTechnology.Core.ApiClient` | `SolTechnology.Core.HTTP` |

## Original decision

The work had two phases:

1. Rename the `ApiClient` project and concepts to `HTTP`, because the module was an HTTP-client
  abstraction rather than a generic API client.
2. Normalize known acronyms such as API, SQL, XML, HTML, and UI in classes, interfaces,
  namespaces, tests, sample code, and documentation.

Published package identities already functioning as compatibility contracts were outside a blind
repository-wide rename. Existing names such as CQRS and AUID already followed the selected rule.

## Alternatives considered

### Follow Microsoft casing exactly

Rejected for repository code because the two-letter versus longer-acronym distinction was judged
less readable than one uppercase rule.

### Preserve every existing identifier

Rejected because it would leave the convention unenforceable and retain competing names for the
same concepts. Compatibility-sensitive package identities were still treated separately.

## Scope

- Rename source identifiers and namespaces to uppercase known acronyms.
- Replace the `ApiClient` module terminology with `HTTP`.
- Update IDE inspections, documentation, tests, and CI references.

## Implementation plan

The work was delivered in analysis, refactoring, IDE configuration, verification, documentation,
and CI phases:

- rename HTTP, API, and SQL projects, namespaces, types, references, and sample integrations;
- update `HTTPClients` configuration and DreamTravel client names;
- update the solution, project references, tests, and pipeline paths;
- configure IDE inspections for the repository's acronym convention;
- build the solution and run the complete test suite after the mechanical changes.

## Acceptance criteria

- Acronym casing is consistent in touched source identifiers.
- Projects build and tests pass after renames.
- Public compatibility constraints are recorded instead of silently broken.

## Completion summary

The source naming convention and the `ApiClient` to `HTTP` module rename shipped across core
projects, tests, DreamTravel, configuration, documentation, and build infrastructure. Historical
verification recorded a successful build and 334 passing tests across the then-current suite.

Package identities that function as compatibility contracts were not uniformly recased. Current
rules live in
[`../architecture/naming-and-public-api.md`](../architecture/naming-and-public-api.md).

## Consequences

### Positive

- Known acronyms became visually consistent across touched identifiers.
- `HTTP` described the client module more precisely than `ApiClient`.
- A single repository rule replaced acronym-length-dependent casing.

### Negative

- The broad rename created merge-conflict and broken-reference risk.
- Internal consumers had to update namespaces, type names, configuration, and project references
  together.
- Compatibility constraints prevented complete uniformity across all published names.

## Deviations

- Package ID `SolTechnology.Core.Api` remains stable while its namespace uses `API`.
- Current code still contains isolated naming exceptions; this record is not a claim of complete
  repository-wide normalization.

## Follow-ups

- Apply acronym casing when touching existing identifiers, subject to public compatibility.
- Treat any public or protected rename as an explicit compatibility decision rather than a style
  cleanup.
