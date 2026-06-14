namespace SolTechnology.Core.CQRS;

/// <summary>
/// Replaceable seam for event publishing. The default dispatches fire-and-forget in-process.
/// Plugins (e.g. <c>AddPersistentEvents()</c>) swap the implementation for durable dispatch.
/// </summary>
public interface IEventPublisher
{
    /// <summary>Publish a strongly-typed event.</summary>
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;

    /// <summary>
    /// Publish an event whose concrete type is only known at runtime
    /// (e.g. deserialized from a queue).
    /// </summary>
    void Publish(IEvent @event);
}


