# Package Release

Supported packages inherit version `1.0.0`; `SolTechnology.Core.Logging` overrides it with
`1.2.0`. Scheduler and Guards remain at `0.5.0` and are not packable.

Package metadata, MIT license, README, icon, XML documentation, symbols, deterministic builds, and
SourceLink are centralized in `src/Directory.Build.props`; only the reference-only Guards and
Scheduler projects opt out of the XML documentation file. A build guard fails when a packable
project under `src/` is omitted from `SolTechnology.Core.slnx`.

The package workflow builds, tests, and packs on pull requests, `master`, version tags, and
manual runs. Publishing to NuGet occurs only for a `v*` tag or `workflow_dispatch`; ordinary
`master` pushes do not publish. Packing enumerates projects from the solution instead of using a
hand-maintained package list, and publishing uses `--skip-duplicate`.

The tag publish also waits for the `dreamtravel-tests` job, which runs the DreamTravel unit and
component tests on every trigger. DreamTravel is the reference app the Coding Guide cites, so a
showcase regression blocks a release like a library regression does. The job has no `if:` and no
`continue-on-error`: a job that `publish` needs and that is skipped skips the publish too, and a
Linux-only failure is fixed in tests or fixtures, never by weakening the job.

Deprecated package unlisting uses a separate manual workflow. It requires the exact confirmation
value `UNLIST` and performs unlisting rather than claiming unsupported NuGet CLI deprecation.

Every action in the workflows is pinned to a full commit SHA with its release as a comment, so a
moved tag cannot change what runs next to the NuGet key. Dependabot proposes `nuget` and
`github-actions` updates weekly. It ignores NuGet majors, which go through the package-management
skill by hand, and no group mixes published dependency floors with test-only or DreamTravel
packages, so every floor raise is its own visible PR that must carry its `canonical-versions.md`
and `CHANGELOG.md` lines ([CONTRIBUTING.md](../../CONTRIBUTING.md#dependency-floors)). Actions
update as one group because `download-artifact` runs only on tags, where a split bump would break
only the release. Groups and pins are listed in [`docs/CICD.md`](../CICD.md).

The release architecture favors deliberate publishing, structural package discovery, centralized
metadata, a pinned supply chain, and an explicit retirement operation.