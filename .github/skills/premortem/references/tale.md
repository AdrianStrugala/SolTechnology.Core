# Tale — `src/SolTechnology.Core.Tale/`

- Persisted context schema delta without a migration path → tales fail to resume after deploy.
- Chapter name/identity changed → in-flight tales resume on the wrong chapter.
- Idempotency broken: replay of a chapter causes side effects twice.
- Interactive tale waiting on user input deadlocks when the input channel changes.
- Serialization contract drift between worker and API host.
