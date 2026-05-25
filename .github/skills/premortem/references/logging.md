# Logging — `src/SolTechnology.Core.Logging/`

- `logger.Log*` contract change (template, level, scope) violates
  [docs/ClaudeCodingGuide.md](../../../../docs/ClaudeCodingGuide.md) §11.
- Structured field renamed → downstream queries / dashboards silently empty.
- PII leak via new field, exception message, or scope value.
- Performance regression (boxing, string concat) on hot path.
- Shared-framework reference change (`Microsoft.Extensions.Logging`) triggers
  [NU1510](../../../../src/Directory.Build.props) noise for pure-NuGet consumers.

