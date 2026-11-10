# Build & NuGet — `Directory.Build.props`, `src/Directory.Build.props`

- `TreatWarningsAsErrors=true` → a new analyzer warning fails consumer builds. Confirm with
  [src/Directory.Build.props](../../../../src/Directory.Build.props).
- Demoted warnings (`NU1900`, `NU1510`) hide a real regression.
- Transitive `Microsoft.Extensions.*` 10.0.1 bumped → downstream apps still on `net9.0` get
  NU1605 / runtime mismatch.
- `snupkg` symbol package missing → debugging broken for consumers.
- Author / RepositoryUrl / license metadata regressed.
- Some `SolTechnology.Core.*` packages bumped to a new major, others not → a mixed graph compiles
  and fails at runtime (`MissingMethodException`, `TypeLoadException`); keep the lockstep version
  and the `SOLCORE001` `buildTransitive` check.
- Shared `ItemGroup` or package removed from `src/Directory.Build.props` → projects outside the
  slnx (Guards, Scheduler, Guards.Tests) stop compiling unnoticed; build them by path.
- Version change in `Directory.Packages.props` → the published dependency floor moves for every
  consumer; a leaf override for a CVE with no parent fix needs a removal follow-up.
- Package version taken from the props file, not the tag → any `v*` tag publishes whatever
  `<Version>` says; keep the tag guard.

# .NET 10 target

- New language / runtime feature used in a public signature → consumers on older TFM cannot
  reference the package.
- Nullable annotations changed on a public surface → consumer warnings turn into errors.

