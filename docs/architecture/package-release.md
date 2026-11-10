# Package Release

Every supported package inherits version `2.0.0` from `src/Directory.Build.props`; no package
overrides it. 2.0.0 is not published yet; the first `v2.0.0` tag publishes it. Scheduler and
Guards are deprecated, reference-only projects: they stay at `0.5.0`, are `[Obsolete]` and not
packable, and sit outside `SolTechnology.Core.slnx`, so `SolTechnology.Core.Guards.Tests` does not
run in CI.

## Lockstep versions

All `SolTechnology.Core.*` packages share one version and are upgraded together. A mixed 1.x/2.x
graph compiles but fails at runtime (`MissingMethodException`, `TypeLoadException`), so every
package ships `src/SolTechnology.Core.Lockstep.targets` as `buildTransitive/<PackageId>.targets`.
It warns with `SOLCORE001` when the resolved `SolTechnology.Core.*` packages (Guards and Scheduler
excluded) span more than one major version. Every copy defines the same target, so MSBuild keeps
one definition and runs it once. 1.x hotfixes, if ever needed, branch from the `v1.0.0` tag.

## Dependencies

Package versions are managed centrally in the root `Directory.Packages.props`
(`ManagePackageVersionsCentrally`); no `PackageReference` carries a `Version`. Each version is
exact, and it is the minimum version a published package declares, so a floor raise is a visible
change ([package-management skill](../../.github/skills/package-management/SKILL.md) and its
`canonical-versions.md`).

`SolTechnology.Core` declares no dependency at all, and no shared `ItemGroup` adds
`Microsoft.Extensions.*` references to every project: each package references only what it
compiles against, or gets it from the ASP.NET Core shared framework. A consumer that relied on
1.x bringing `Microsoft.Extensions.*` through the base package adds the reference itself.

## Metadata

Package metadata, MIT license, README, icon, XML documentation, symbols, deterministic builds, and
SourceLink are centralized in `src/Directory.Build.props`. `GenerateDocumentationFile` is switched
on there for every `src` project; only Guards and Scheduler opt out. Each package with a
`PackageReadmeSource` packs that `docs/*.md` file as its README and `docs/icons/<PackageId>.png` as
its icon, so every package has its own icon. Symbols ship as `.snupkg` next to each `.nupkg`.

## Workflow

The package workflow builds, tests, and packs on pull requests, `master` pushes, `v*` tags, and
manual runs. Only a `v*` tag publishes: manual runs and `master` pushes never do. On a tag, the
build fails before packing when the tag is not `v` + `<Version>`, because the version comes from
the props file, not from the tag, and nuget.org versions can never be reused. Tests run through
`.github/runTests.ps1`, which runs every test project in the slnx and fails when any one of them
fails, after running the rest. Packing enumerates projects from the solution instead of using a
hand-maintained package list, a build guard fails when a packable project under `src/` is missing
from `SolTechnology.Core.slnx`, and publishing uses `--skip-duplicate`.

The tag publish also waits for the `dreamtravel-tests` job, which runs the DreamTravel unit and
component tests on every trigger. DreamTravel is the reference app the Coding Guide cites, so a
showcase regression blocks a release like a library regression does. The job has no `if:` and no
`continue-on-error`: a job that `publish` needs and that is skipped skips the publish too, and a
Linux-only failure is fixed in tests or fixtures, never by weakening the job.

Publishing uses the `NUGET_API_KEY` repository secret; there is no protected GitHub environment
and no required reviewer, so the tag guard is the automated check that a human reviewer would
otherwise make. Deprecated package unlisting uses a separate manual workflow. It requires the exact
confirmation value `UNLIST` and performs unlisting rather than claiming unsupported NuGet CLI
deprecation. Details are in [`docs/CICD.md`](../CICD.md).

## Supply chain

Every action in the workflows is pinned to a full commit SHA with its release as a comment, so a
moved tag cannot change what runs next to the NuGet key. Dependabot proposes `nuget` and
`github-actions` updates weekly. It ignores NuGet majors, which go through the package-management
skill by hand, and no group mixes published dependency floors with test-only or DreamTravel
packages, so every floor raise is its own visible PR that must carry its `canonical-versions.md`
and `CHANGELOG.md` lines ([CONTRIBUTING.md](../../CONTRIBUTING.md#dependency-floors)). Actions
update as one group because `download-artifact` runs only on tags, where a split bump would break
only the release. Groups and pins are listed in [`docs/CICD.md`](../CICD.md).

The release architecture favors deliberate, tag-only publishing, lockstep versions, structural
package discovery, centralized versions and metadata, a pinned supply chain, and an explicit
retirement operation.
