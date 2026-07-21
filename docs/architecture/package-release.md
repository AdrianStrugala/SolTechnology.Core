# Package Release

Supported packages inherit version `1.0.0`; `SolTechnology.Core.Logging` overrides it with
`1.2.0`. Scheduler and Guards remain at `0.5.0` and are not packable.

Package metadata, MIT license, README, icon, symbols, deterministic builds, and SourceLink are
centralized in `src/Directory.Build.props`. A build guard fails when a packable project under
`src/` is omitted from `SolTechnology.Core.slnx`.

The package workflow builds, tests, and packs on pull requests, `master`, version tags, and
manual runs. Publishing to NuGet occurs only for a `v*` tag or `workflow_dispatch`; ordinary
`master` pushes do not publish. Packing enumerates projects from the solution instead of using a
hand-maintained package list, and publishing uses `--skip-duplicate`.

Deprecated package unlisting uses a separate manual workflow. It requires the exact confirmation
value `UNLIST` and performs unlisting rather than claiming unsupported NuGet CLI deprecation.

The release architecture favors deliberate publishing, structural package discovery, centralized
metadata, and an explicit retirement operation.