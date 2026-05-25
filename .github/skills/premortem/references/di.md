# DI / ModuleInstaller — every module

- Registration removed or renamed → null reference at runtime, build green.
- Lifetime change (singleton ↔ scoped) → captive dependency or state bleed.
- Decorator order change → behaviour drift with no test coverage.
- `IOptions<T>` bound without validation → bad config in prod, no startup failure.
- New `ModuleInstaller` not called by consumer `Startup` / `Program.cs` → silent feature off.

