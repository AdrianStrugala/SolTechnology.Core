# Story — `src/SolTechnology.Core.Story/`

- Persisted state schema delta without a migration path → workflows fail to resume after deploy.
- Step name/identity changed → in-flight workflows resume on the wrong step.
- Idempotency broken: replay of a step causes side effects twice.
- Interactive workflow waiting on user input deadlocks when the input channel changes.
- Serialization contract drift between worker and API host.

