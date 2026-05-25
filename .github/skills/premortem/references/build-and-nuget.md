# Build & NuGet — `Directory.Build.props`, `src/Directory.Build.props`

- `TreatWarningsAsErrors=true` → a new analyzer warning fails consumer builds. Confirm with
  [src/Directory.Build.props](../../../../src/Directory.Build.props).
- Demoted warnings (`NU1900`, `NU1510`) hide a real regression.
- Transitive `Microsoft.Extensions.*` 10.0.1 bumped → downstream apps still on `net9.0` get
  NU1605 / runtime mismatch.
- `snupkg` symbol package missing → debugging broken for consumers.
- Author / RepositoryUrl / license metadata regressed.

# .NET 10 target

- New language / runtime feature used in a public signature → consumers on older TFM cannot
  reference the package.
- Nullable annotations changed on a public surface → consumer warnings turn into errors.

