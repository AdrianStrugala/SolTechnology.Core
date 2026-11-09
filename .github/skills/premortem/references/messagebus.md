# MessageBus — `src/SolTechnology.Core.MessageBus/`

- Message contract drift between producer and consumer versions.
- Poison message handling change → dead-letter loop or silent drop.
- Lock duration / max delivery count change → duplicate processing. Check that complete, abandon, and
  dead-letter never use the processor's cancellation token (the SDK cancels it when shutdown starts, so a
  handler that returns during a rolling deploy cannot complete and another instance handles the message
  again), that a completion failure never enters the handler-failure path (abandon or dead-letter of a
  handled message), and that entities created with `CreateResources` get
  `MessageBusConfiguration.MaxDeliveryCount`, not the broker default of 10. Pinned by
  `Stop_HandlerFinishesAfterShutdownStarts_MessageCompletedNotRedelivered` (emulator) and
  `EnsureInitialized_CreateResources_CreatesQueueAndSubscriptionWithConfiguredMaxDeliveryCount`
  (MessageBus code review, 2026-11-09).
- Broker initialisation that is not all-or-nothing → a retry after a halfway failure adds a second sender
  (every message published twice) and fails receiver registration. Check that senders and processors are
  kept only when every endpoint was provisioned and created (MessageBus code review, 2026-11-09).
- Topic / subscription naming change → silent missed messages.
- Serialiser switch (`Newtonsoft.Json` ↔ `System.Text.Json`) without a migration plan → in-flight
  messages fail to deserialise after deploy.

