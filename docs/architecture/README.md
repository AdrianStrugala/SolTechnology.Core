# Architecture

This folder is the source of truth for how SolTechnology.Core works now and why its current
architecture was chosen.

Architecture documents are living documentation:

- update them in the same change that alters the described behavior;
- describe only the current design, constraints, and rationale;
- replace obsolete content instead of preserving historical versions;
- link to code, contracts, and module documentation instead of copying them;
- use Git history when an earlier version is needed.

Delivery history does not belong here. A dated feature record under
[`../features/`](../features/) captures what was planned and delivered at a point in time and may
no longer describe the current system.

## Topics

- [AI-assisted development](ai-assisted-development.md)
- [API versioning](api-versioning.md)
- [Authentication](authentication.md)
- [Background processing](background-processing.md)
- [CQRS](cqrs.md)
- [Delivery workflow](delivery-workflow.md)
- [HTTP clients](http-client.md)
- [Naming and public API](naming-and-public-api.md)
- [Package release](package-release.md)
- [Tale framework](tale-framework.md)
- [Testing](testing.md)
