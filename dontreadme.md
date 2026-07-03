# Deprecated Libraries

| Library | Replaced By | Notes |
|---------|-------------|-------|
| `SolTechnology.Core.Scheduler` | `SolTechnology.Core.Hangfire` | In-memory cron scheduler → Hangfire-backed persistent recurring jobs. Use `AddSolRecurringJob<TJob>(cron)`. See [docs/Hangfire.md](docs/Hangfire.md). |
| `SolTechnology.Core.Guards` | `FluentValidation` | Custom guard clauses → industry-standard `AbstractValidator<T>` auto-discovered by CQRS pipeline. Validators live in the same file as the input DTO. |
| `SolTechnology.Core.ApiClient` | `SolTechnology.Core.HTTP` | Resilient typed HTTP clients (Polly v8 retry / circuit breaker, correlation propagation, metrics). See [docs/Clients.md](docs/Clients.md). |
| `SolTechnology.Core.Story` | `SolTechnology.Core.Tale` | Workflow framework renamed to Tale — type/using rename (`StoryHandler` → `TaleHandler`) under a new package id. See [docs/Tale.md](docs/Tale.md). |
| `SolTechnology.Core.BlobStorage` | `SolTechnology.Core.Blob` | Renamed to `.Blob` under a new package id — type/namespace rename (`BlobStorageConfiguration` → `BlobConfiguration`, `AddSolBlobStorage` → `AddSolBlob`, namespace `SolTechnology.Core.Blob`). Companion `SolTechnology.Core.Blob.Testing`. See [docs/Blob.md](docs/Blob.md). |
| `SolTechnology.Core.Faker` | `SolTechnology.Core.HTTP.Testing` | HTTP test helpers moved to the dedicated `.HTTP.Testing` package, co-located with the HTTP client module. |
| `SolTechnology.Core.Flow` | `SolTechnology.Core.Tale` | Workflow framework superseded by Tale — see `SolTechnology.Core.Story` → `SolTechnology.Core.Tale` migration above. |

