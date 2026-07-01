# SolTechnology.Core.SQL.Testing

Integration-testing fixtures for `SolTechnology.Core.SQL` consumers. **Reference from test projects
only** — never from production assemblies.

> Part of the testing framework defined in
> [ADR-008](adr/008-testing-framework-companions.md). For the overall testing strategy see
> [theQuality.md](theQuality.md); for the shared foundation helpers see [Testing.md](Testing.md).

## What's inside

| Type | Purpose |
|---|---|
| `SQLFixture` | A Testcontainers-backed database fixture for **MSSQL** (dacpac) and **PostgreSQL**. |
| Schema provisioning | Pluggable strategies — dacpac, raw scripts, or a delegate (e.g. EF migrations). |
| Respawn reset | Between-test data reset via [Respawn](https://github.com/jbogard/Respawn). |

ORM-agnostic by design: serves **Dapper** and **Entity Framework** alike.

## Usage

Spin up `SQLFixture` in your component-test host, choose a schema-provisioning strategy, and let
Respawn reset state between tests. See the companion foundation package
[`SolTechnology.Core.Testing`](Testing.md) for the shared Testcontainers lifetime/reuse model these
fixtures build on.
