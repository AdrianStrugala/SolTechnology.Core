# Contributing to SolTechnology.Core

Thanks for helping. Bug reports, documentation fixes, and pull requests are all welcome.

## Build and test

You need the .NET 10 SDK (10.0.100 or later, see `global.json`) and Docker (the component tests
start PostgreSQL, Redis, Azurite, and the Service Bus emulator through Testcontainers).

```bash
dotnet workload restore SolTechnology.Core.slnx
dotnet build SolTechnology.Core.slnx
pwsh ./.github/runTests.ps1
```

`runTests.ps1` runs every project under `tests/` with `--no-build`, so build first. The DreamTravel
sample has its own tests: `cd sample-tale-code-apps/DreamTravel && dotnet test`.

## Conventions

- Code follows the [Coding Guide](docs/ClaudeCodingGuide.md); the current design lives in
  [`docs/architecture/`](docs/architecture/README.md).
- Non-trivial changes start with a dated feature brief, see the
  [delivery workflow](docs/architecture/delivery-workflow.md).
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/) with a module scope,
  for example `fix(cache): ...`. A change to a published package adds a `Semver: PATCH | MINOR | MAJOR`
  footer; the [commit-message skill](.github/skills/commit-message/SKILL.md) has the details.
- Package versions live only in `Directory.Packages.props` (Central Package Management); never put
  `Version=` on a `PackageReference`.

## Dependency floors

The item group "Library and testing-kit dependencies" in `Directory.Packages.props` sets the minimum
versions every published package asks of its consumers. A PR that changes a version in that group
must, in the same PR:

1. update [`canonical-versions.md`](.github/skills/package-management/references/canonical-versions.md);
2. add a line to the unreleased section of [`CHANGELOG.md`](CHANGELOG.md) naming the new floor.

For a Dependabot PR, push both to its branch before merging. Dependabot never proposes major
updates; a major bump goes through the
[package-management skill](.github/skills/package-management/SKILL.md) by hand.

## Pull requests

Fill in the [PR template](.github/PULL_REQUEST_TEMPLATE.md). CI builds the solution and runs the
tests on every PR. Security issues go through [SECURITY.md](SECURITY.md), never a public issue.
