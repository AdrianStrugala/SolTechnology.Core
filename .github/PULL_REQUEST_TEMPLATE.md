## What and why

<!-- One or two sentences. Link the dated feature brief under docs/features/ if there is one. -->

## Semver impact

- [ ] None (docs, tests, samples, CI)
- [ ] PATCH (internal fix, no public API change)
- [ ] MINOR (additive public API)
- [ ] MAJOR (breaking; the commit has a `BREAKING CHANGE:` footer and the feature brief explains the migration)

## Checklist

- [ ] `dotnet build SolTechnology.Core.slnx` and `pwsh ./.github/runTests.ps1` pass locally.
- [ ] New behaviour has tests; every new `Result` failure path has a negative test.
- [ ] No change from the [`CLAUDE.md` §2](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/CLAUDE.md#2-forbidden-actions--ask-dont-act) list
      (public symbol rename or removal, version bump, `Directory.Build.props` analyser settings,
      new external dependency in `src/`) without the maintainer's agreement.
- [ ] Dependency floor: if a version in the "Library and testing-kit dependencies" group of
      `Directory.Packages.props` changed, `canonical-versions.md` and the unreleased section of
      `CHANGELOG.md` are updated in this PR ([CONTRIBUTING.md](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/CONTRIBUTING.md#dependency-floors)).
- [ ] Docs under `docs/` match the change.
