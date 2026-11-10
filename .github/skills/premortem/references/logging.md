# Logging — `src/SolTechnology.Core.Logging/`

- `logger.Log*` contract change (template, level, scope) violates
  [docs/ClaudeCodingGuide.md](../../../../docs/ClaudeCodingGuide.md) §11.
- Structured field renamed → downstream queries / dashboards silently empty.
- PII leak via new field, exception message, or scope value.
- Performance regression (boxing, string concat) on hot path.
- Shared-framework reference change (`Microsoft.Extensions.Logging`) triggers
  [NU1510](../../../../src/Directory.Build.props) noise for pure-NuGet consumers.
- Ambient per-operation state (`AsyncLocal`) that one record spans for a whole long-lived request
  (WebSocket, SignalR, SSE) or service lifetime → unbounded buffers and one late, Warning-level
  entry per connection; exclude upgradable and streaming requests from per-request roots.
- Emission level derived from the root alone → an `Ok` root with a failed child is filtered by a
  Warning minimum level although sampling keeps it; decide the level from every signal sampling
  uses.
- Instrumentation reading a diagnostic payload, a storage parameter, or a counter before its cheap
  gate → per-call cost even when nothing is recorded.
- `[LoggerMessage]` `{@Name}` placeholders reach providers under the parameter name (no `@`), so
  Serilog does not destructure them; do not promise structured payloads from the template.
- Moving a span from a module's own `ActivitySource` to a shared one changes its tags (typed vs
  string), its status (`Ok` vs `Unset`), and which option turns it off.
