# CQRS — `src/SolTechnology.Core.CQRS/`

- Handler not registered in `ModuleInstaller` → MediatR throws at first request, not at startup.
- Result pattern bypassed (throws instead of returning `Result.Failure(...)`) — see
  [docs/ClaudeCodingGuide.md](../../../../docs/ClaudeCodingGuide.md) §3.
- Chain handler ordering changed → silent behaviour drift (no compile-time guard).
- `IRequest<Result<T>>` signature change → consumers' handlers no longer match registration.
- Cancellation token dropped on the boundary.

