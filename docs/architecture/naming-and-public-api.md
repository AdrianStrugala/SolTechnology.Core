# Naming and Public API

Source identifiers capitalize acronyms: `API`, `HTTP`, `SQL`, `AUID`, and `CQRS`. Apply this
rule to namespaces, types, members, and local identifiers when the identifier is not constrained
by an existing compatibility contract.

Published package and assembly identities remain stable even when their casing differs from
source identifiers. For example, the package ID remains `SolTechnology.Core.Api` while its
namespace is `SolTechnology.Core.API`. Consumer compatibility takes precedence over cosmetic
uniformity.

Public registration and middleware extensions use the branded prefixes `AddSol*`, `UseSol*`,
and `MapSol*`. Examples include `AddSolCQRS`, `AddSolAuthentication`, `AddSolVersioning`,
`UseSolLogging`, and `MapSolHealthChecks`. Pre-1.0 names have no compatibility forwarders.

The convention makes domain abbreviations recognizable and reduces extension-method collisions
in IntelliSense. Public and protected symbols remain compatibility contracts: rename, move, or
delete them only after explicit maintainer confirmation and with the appropriate semver impact.

Current exceptions are documented by the code rather than normalized speculatively. Do not infer
that a package ID or legacy public symbol should be renamed solely to satisfy acronym casing.
