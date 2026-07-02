# Deprecated Libraries

| Library | Replaced By | Notes |
|---------|-------------|-------|
| `SolTechnology.Core.Scheduler` | `SolTechnology.Core.Hangfire` | In-memory cron scheduler → Hangfire-backed persistent recurring jobs. Use `AddSolRecurringJob<TJob>(cron)`. See [docs/Hangfire.md](docs/Hangfire.md). |
| `SolTechnology.Core.Guards` | `FluentValidation` | Custom guard clauses → industry-standard `AbstractValidator<T>` auto-discovered by CQRS pipeline. Validators live in the same file as the input DTO. |
| `SolTechnology.Core.ApiClient` | `SolTechnology.Core.HTTP` | Resilient typed HTTP clients (Polly v8 retry / circuit breaker, correlation propagation, metrics). See [docs/Clients.md](docs/Clients.md). |
| `SolTechnology.Core.Story` | `SolTechnology.Core.Tale` | Workflow framework renamed to Tale — type/using rename (`StoryHandler` → `TaleHandler`) under a new package id. See [docs/Tale.md](docs/Tale.md). |

