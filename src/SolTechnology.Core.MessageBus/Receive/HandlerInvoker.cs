using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.MessageBus.Receive;

/// <summary>
/// Calls the <see cref="IMessageHandler{TMessage}"/> of a message type through a compiled generic delegate,
/// so a handler's exception reaches the receiver unwrapped.
/// </summary>
internal sealed class HandlerInvoker
{
    private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _invokersByType = new();

    public Task Invoke(IServiceProvider serviceProvider, Type messageType, object message, CancellationToken cancellationToken) =>
        _invokersByType.GetOrAdd(messageType, BuildInvoker)(serviceProvider, message, cancellationToken);

    private static Func<IServiceProvider, object, CancellationToken, Task> BuildInvoker(Type messageType) =>
        typeof(HandlerInvoker)
            .GetMethod(nameof(InvokeHandler), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(messageType)
            .CreateDelegate<Func<IServiceProvider, object, CancellationToken, Task>>();

    private static Task InvokeHandler<TMessage>(IServiceProvider serviceProvider, object message, CancellationToken cancellationToken)
        where TMessage : IMessage =>
        serviceProvider.GetRequiredService<IMessageHandler<TMessage>>().Handle((TMessage)message, cancellationToken);
}
