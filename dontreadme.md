# Deprecated Libraries

| Library | Replaced By | Notes |
|---------|-------------|-------|
| `SolTechnology.Core.Scheduler` | `SolTechnology.Core.Hangfire` | In-memory cron scheduler → Hangfire-backed persistent recurring jobs. Use `AddRecurringJob<TJob>(cron)`. See [docs/Hangfire.md](docs/Hangfire.md). |
| `SolTechnology.Core.Guards` | `FluentValidation` | Custom guard clauses → industry-standard `AbstractValidator<T>` auto-discovered by CQRS pipeline. Validators live in the same file as the input DTO. |

