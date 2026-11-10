## SolTechnology.Core.MessageBus

Typed Azure Service Bus publishing and receiving. You register a message type against a queue or a topic, publish it
through `IMessagePublisher`, and handle it in an `IMessageHandler<T>`; the module owns the client, the processors, and
the settlement of every received message.

### Features

- **Typed endpoints** — one line per message type: `WithQueuePublisher<T>`, `WithQueueReceiver<T, THandler>`,
  `WithTopicPublisher<T>`, `WithTopicReceiver<T, THandler>`.
- **One publisher** — `IMessagePublisher.Publish(message)` sends to every queue and topic registered for the type.
- **Scoped handlers** — each message is handled in its own DI scope, so handlers can use scoped services such as a
  `DbContext`.
- **Explicit settlement** — success completes the message, a failure is abandoned for retry and dead-lettered on the
  last attempt, and a message that cannot be read is dead-lettered at once.
- **Correlation** — the publisher's correlation id travels with the message and is restored in the handler's log scope.
- **Fails at start** — an empty connection string or a non-positive `MaxDeliveryCount` stops the host.
- **Optional provisioning** — `CreateResources` creates missing queues, topics, and subscriptions at start, also when
  several instances start at once.
- **Health check** — `AddSolServiceBusHealthCheck()` proves broker connectivity with a non-destructive peek.

### Registration

Register the module with its configuration, then chain one call per endpoint:

```csharp
var messageBusConfiguration = builder.Configuration.GetSection("MessageBus").Get<MessageBusConfiguration>()!;

builder.Services
    .AddSolMessageBus(messageBusConfiguration)
    .WithQueuePublisher<OrderPlaced>("orders")
    .WithQueueReceiver<OrderPlaced, OrderPlacedHandler>("orders")
    .WithTopicPublisher<PlayerMatchesSynchronized>("player-matches")
    .WithTopicReceiver<PlayerMatchesSynchronized, CalculatePlayerStatistics>("player-matches", "statistics");
```

### Configuration

| `MessageBusConfiguration` | Default | Purpose |
|---|---|---|
| `ConnectionString` | — (required) | Service Bus connection string. An empty or whitespace value stops the host at start with `OptionsValidationException`. |
| `MaxDeliveryCount` | `10` | Delivery on which a failing message is dead-lettered instead of abandoned. Set it to the queue's or subscription's `MaxDeliveryCount`; it must be greater than zero. |
| `CreateResources` | `false` | Create missing queues, topics, and subscriptions at start; created queues and subscriptions get `MaxDeliveryCount`. Needs the *Manage* claim; prefer infrastructure as code. |
| `TransportType` | `AmqpWebSockets` | AMQP over port 443, which passes most proxies and firewalls; `AmqpTcp` uses port 5671. |
| `MaxConcurrentCalls` | `1` | Messages each receiver handles in parallel. |
| `PrefetchCount` | `0` | Messages each receiver fetches ahead. |
| `RetryOptions` | `null` | `ServiceBusRetryOptions` for the module's client; `null` keeps the SDK defaults. |
| `Queues` | empty | Queue name per message type (`MessageType` is the class name without namespace), used when `WithQueuePublisher` or `WithQueueReceiver` gets no name. |

Bind it from the `MessageBus` section, and keep the connection string in an environment variable
(`MessageBus__ConnectionString`) or a secret store rather than in `appsettings.json`:

```json
{
  "MessageBus": {
    "MaxDeliveryCount": 10,
    "Queues": [
      { "MessageType": "OrderPlaced", "QueueName": "orders" }
    ]
  }
}
```

### Usage

#### Messages

A message is a class that implements `IMessage` and assigns its `Id` once; the id becomes the Service Bus `MessageId`:

```csharp
public record OrderPlaced(int OrderId) : IMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
```

#### Publishing

Inject `IMessagePublisher` and publish:

```csharp
public class PlaceOrderService(IMessagePublisher messagePublisher)
{
    public Task PlaceOrder(int orderId, CancellationToken cancellationToken) =>
        messagePublisher.Publish(new OrderPlaced(orderId), cancellationToken);
}
```

The body is the message serialized as JSON with `Newtonsoft.Json`; `Subject` and the `Type` application property carry
the class name, and `CorrelationId` carries the current correlation id.

#### Receiving

Implement `IMessageHandler<T>` and register it with `WithQueueReceiver` or `WithTopicReceiver`:

```csharp
public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IMessageHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order [{OrderId}] placed", message.OrderId);
        return Task.CompletedTask;
    }
}
```

The receiver settles every message itself:

| Outcome | Settlement |
|---|---|
| `Handle` returns | Completed |
| `Handle` returns, but completing fails (for example, the lock was lost) | Warning `Failed to complete message [{MessageId}]; lock will expire naturally`; the message is delivered again |
| `Handle` throws, delivery count below `MaxDeliveryCount` | Abandoned and delivered again |
| `Handle` throws on delivery `MaxDeliveryCount` | Dead-lettered, reason `HandlerFailedMaxDelivery`, description = the exception's message |
| Empty body | Dead-lettered, reason `EmptyBody` |
| Body is not JSON of the message type | Dead-lettered, reason `DeserializationFailed` |
| Body deserializes to `null` | Dead-lettered, reason `NullPayload` |
| Host stops while `Handle` runs | `cancellationToken` is cancelled; if `Handle` still returns, the message is completed, and if it throws, the message is delivered again |

#### Chronicle

`AddSolMessageBus` registers [Chronicle](Log.md#chronicle): every received message is its own root
`Message {MessageType}` with attributes `MessageId`, `DeliveryCount` and `Entity`, inside the
receiver's log scope, so handler, SQL and HTTP steps nest under it.

| Settlement | Root status | Attributes |
|---|---|---|
| Completed | `Ok` | `Settlement=Completed` |
| Handler succeeded, completion failed | `Ok` | `Settlement=CompleteFailed` |
| Handler threw, retries left | `Failed` | `Settlement=Abandoned` |
| Handler threw on the last delivery | `Failed` | `Settlement=DeadLettered`, `DeadLetterReason=HandlerFailedMaxDelivery` |
| Unreadable body | `Failed` (`DeadLettered`) | `Settlement=DeadLettered`, `DeadLetterReason=EmptyBody`, `DeserializationFailed` or `NullPayload` |
| Processor shutdown | `Cancelled` | — |

The body and the application properties are never recorded, and a dead-lettered message records
the reason code only, never the description.

#### Running several instances

Every instance that registers a receiver takes part in receiving:

| Receiver | Behaviour across instances |
|---|---|
| `WithQueueReceiver` | Competing consumers: each message is handled by one instance. |
| `WithTopicReceiver` | All instances share the configured subscription, so they also compete: each message is handled by one instance. |
| Every instance must see every message | Give each instance its own subscription name, for example `$"cache-invalidation-{Environment.MachineName}"`. The module does not provide this; create and clean up those subscriptions yourself. |

- **At least once.** A message whose lock expires or whose instance crashes is delivered again, possibly to another
  instance, so handlers must be idempotent; the
  [idempotency store of `SolTechnology.Core.Cache`](Cache.md#idempotency-store) can record what was already done.
- **No ordering across instances.** Messages are handled in parallel by several instances, and by one instance when
  `MaxConcurrentCalls` is above `1`.
- **Provisioning is safe to race.** With `CreateResources` on, an instance that finds an entity created by another
  instance in the meantime continues to start.

#### Health check

Add the connectivity check after `AddSolMessageBus`, which registers the client it uses:

```csharp
builder.Services.AddHealthChecks()
    .AddSolServiceBusHealthCheck();
```

It peeks the first queue in `MessageBusConfiguration.Queues`, which needs only the *Listen* claim and never locks or
consumes a message. The default timeout is 10 s (`timeout` parameter).

| Outcome | Status |
|---|---|
| Peek succeeds | `Healthy` |
| Broker rejects the probe (unauthorized, or the queue does not exist) | `Degraded` |
| No queue configured to probe | `Degraded` |
| Connection, DNS, or timeout failure | `Unhealthy` (configurable via `failureStatus`) |
| Caller cancels | Cancellation is rethrown |

### Testing

[`SolTechnology.Core.ServiceBus.Testing`](ServiceBus.Testing.md) runs the Azure Service Bus emulator in a container;
publish through a started host and assert on what your handler did:

```csharp
[Test]
public async Task Publish_OrderPlaced_HandlerReceivesIt()
{
    // Arrange
    var fixture = new ServiceBusFixture(configFilePath: "servicebus-emulator-config.json");
    await fixture.Initialize();
    var received = new TaskCompletionSource<OrderPlaced>();
    var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
    builder.Services.AddSingleton(received);
    builder.Services
        .AddSolMessageBus(new MessageBusConfiguration
        {
            ConnectionString = fixture.ConnectionString,
            TransportType = ServiceBusTransportType.AmqpTcp
        })
        .WithQueuePublisher<OrderPlaced>("orders")
        .WithQueueReceiver<OrderPlaced, RecordingHandler>("orders");
    using var host = builder.Build();
    await host.StartAsync();

    // Act
    await host.Services.GetRequiredService<IMessagePublisher>().Publish(new OrderPlaced(42));

    // Assert
    var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(60));
    message.OrderId.Should().Be(42);
    await host.StopAsync();
    await fixture.DisposeAsync();
}

public class RecordingHandler(TaskCompletionSource<OrderPlaced> received) : IMessageHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced message, CancellationToken cancellationToken)
    {
        received.TrySetResult(message);
        return Task.CompletedTask;
    }
}
```

The emulator has no administration API, so declare the test's queues in the emulator config file and leave
`CreateResources` off.

### Conventions

- **Do** assign `IMessage.Id` once per message; a new id on every read defeats duplicate detection.
- **Do** set `MaxDeliveryCount` to the value configured on the queue or subscription.
- **Do** make handlers idempotent; delivery is at least once.
- **Do** throw from `Handle` to retry; return to complete.
- **Don't** rename a message class configured in `Queues` without updating its `MessageType`; the lookup uses the class
  name without namespace.
- **Don't** remove or rename message properties while older publishers or messages in flight still use them; both
  sides read the JSON with `Newtonsoft.Json`.
- **Don't** turn `CreateResources` on in production; provision entities with infrastructure as code.

### What ships in DI

| Service | Implementation | Lifetime |
|---|---|---|
| `IMessagePublisher` | Sends to every registered entity of the message type | Singleton |
| `IHostedService` | Starts one processor per receiver and settles messages | Singleton |
| `IMessageHandler<T>` | Your handler, one per `With*Receiver` call | Scoped |
| `ServiceBusClient` (keyed) | The module's own client; an unkeyed `ServiceBusClient` you register is not used | Singleton |
| `IOptions<MessageBusConfiguration>` | Validated at host start | Singleton |
| `ICorrelationIdService` | From `SolTechnology.Core.Logging` | Singleton |
