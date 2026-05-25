# MessageBus — `src/SolTechnology.Core.MessageBus/`

- Message contract drift between producer and consumer versions.
- Poison message handling change → dead-letter loop or silent drop.
- Lock duration / max delivery count change → duplicate processing.
- Topic / subscription naming change → silent missed messages.
- Serialiser switch (`Newtonsoft.Json` ↔ `System.Text.Json`) without a migration plan → in-flight
  messages fail to deserialise after deploy.

