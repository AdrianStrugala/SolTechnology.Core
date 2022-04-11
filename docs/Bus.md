### Overview

The SolTechnology.Core.MessageBus library provides minimal functionality needed for Azure Service Bus connection. It handles needed services registration and configuration.

### Registration

For installing the library, reference **SolTechnology.Core.MessageBus** nuget package and invoke **AddMessageBus()** service collection extension method:

```csharp
services.AddMessageBus();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "MessageBus": {
      "ConnectionString": "your-service-bus-connection-string"
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var messageBusConfiguration = new MessageBusConfiguration
{
    ConnectionString = "your-service-bus-connection-string"
};
services.AddMessageBus(messageBusConfiguration);
```


### Usage

#### II. Message publishing
1) Register message publisher

```csharp
            services.AddMessageBus()
                    .WithPublisher<PlayerMatchesSynchronizedEvent>("synchronizeplayermatches");
```

Where T is a published message type and argument is a topic name.


2) Inject IMessagePublisher

```csharp
  public SynchronizePlayerMatchesHandler(IMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher;
        }
```

3) Invoke Publish() method

```csharp
var message = new PlayerMatchesSynchronizedEvent(command.PlayerId);
await _messagePublisher.Publish(message);
```

#### I. Message receiving
1) Register message receiver

```csharp
builder.Services.AddMessageBus()
                .WithReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>("synchronizeplayermatches", "calculatestatistics");
```

Where generic arguments are MessageType, MessageHandlerType class and arguments: topicName and subscriptionName.


2) Make your Message Handler implement IMessageHandler<T> (where T is a MessageType)

```csharp
public class CalculatePlayerStatistics : IMessageHandler<PlayerMatchesSynchronizedEvent>
```

3) Implement Handle(T) method

```csharp
        public async Task Handle(PlayerMatchesSynchronizedEvent message, CancellationToken cancellationToken)
        {
            var command = new CalculatePlayerStatisticsCommand(message.PlayerId);
            await _handler.Handle(command);
        }
```


4) What is more
- The Message Handler is registered as IHostedService
- Topics and subsriptions are created from code on registration using default values
- Message has to implement IMessage inteface