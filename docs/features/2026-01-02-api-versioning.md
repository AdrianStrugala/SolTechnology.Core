---
status: completed
created: 2026-01-02
completed: 2026-01-02
---

# API Versioning

> Historical delivery record. It may not describe the current system.

## Goal

Introduce API versioning without embedding versions in resource URLs.

## Context

The application needed parallel API versions, a default for existing clients, and discoverable
OpenAPI documents without route duplication. DreamTravel had inconsistent routes: older endpoints
used unversioned URLs while newer endpoints embedded `/v2/`. Swagger exposed only part of the API,
controllers managed route constants independently, and there was no centralized deprecation
mechanism.

The goals were to unify version selection, expose every version in Swagger, centralize setup in
`SolTechnology.Core.Api`, support future versions, and report supported and deprecated versions to
clients.

## Original decision

Use `Asp.Versioning.Mvc` with header-based version selection:

- request header: `X-API-VERSION`;
- controllers declare `[ApiVersion]` and actions may use `[MapToApiVersion]`;
- routes identify resources without embedding a version segment;
- the application configures a default version for requests without the header;
- API Explorer generates one Swagger document per discovered version;
- responses report supported and deprecated versions.

The first DreamTravel plan used version `2.0` as its application default while marking `1.0` as
deprecated. That application policy was not made the package default.

## Alternatives considered

| Strategy | Benefits | Why it was not selected |
|---|---|---|
| URL path, `api/v1/resource` | Explicit, familiar, browser-friendly | Duplicated resource URLs and conflicted with the route setup used during implementation. |
| Query string, `?api-version=1.0` | Stable base URL | Mixed version identity with ordinary query parameters. |
| Header, `X-API-VERSION: 1.0` | Stable resource URLs, good library and Swagger integration | Selected despite requiring clients to set metadata explicitly. |
| Media type | Fits content negotiation | Too complex and uncommon for the repository's needs. |

## Amendment during delivery

The initial attempt used URL-segment versioning. Controllers combining hard-coded `/v1/` or
`/v2/` paths with `UrlSegmentApiVersionReader` were not registered as expected and requests
returned `404`. The plan was amended on the decision date to use `HeaderApiVersionReader`.

The amendment required v2 clients to move from paths such as `api/v2/CalculateBestPath` to the
stable resource path plus `X-API-VERSION: 2.0`. Tests and the DreamTravel UI needed the same header.

## Scope

- Select header versioning through `X-API-VERSION`.
- Assume a configured default when the header is absent.
- Report supported and deprecated versions.
- Generate version-specific Swagger documents.

## Implementation plan

Configure the versioning library, annotate controllers, group API Explorer documents, and update
clients and tests. Centralize package setup behind an extension method, generate version-specific
Swagger documents, order them newest-first in the UI, and label deprecated documents.

## Acceptance criteria

- Versioned controllers share stable resource routes.
- Clients select a version through `X-API-VERSION`.
- Swagger exposes each discovered version.
- Requests without a header use the configured application default.
- Responses report supported and deprecated versions.
- Existing v1 behavior remains available during its migration period.

## Expected consequences

### Positive

- One URL identifies each logical resource.
- Adding a version is controller metadata plus a new action or controller implementation.
- Swagger presents separate, discoverable documents.
- Clients receive machine-readable supported/deprecated-version headers.

### Negative

- Explicit selection requires a custom header and is less convenient in a browser address bar.
- Clients using versioned URL paths must change both path and headers.
- Assuming a default can hide clients that have not consciously selected a contract.

## Completion summary

Header-based versioning, centralized registration, per-version API Explorer documents, and the
Swagger selector shipped. DreamTravel clients and component tests were updated for the amended
header contract. Header versioning remains the current model; current behavior and rationale live
in [`../architecture/api-versioning.md`](../architecture/api-versioning.md).

## Deviations

- Dependency and extension-method versions in the original plan were superseded.
- The library default is `1.0`; DreamTravel independently configures `2.0`.

## Follow-ups

- Measure version usage before removing a deprecated contract.
- Add another selection mechanism only when a concrete consumer requires it.
- Keep sunset dates and migration promises application-owned rather than package defaults.
