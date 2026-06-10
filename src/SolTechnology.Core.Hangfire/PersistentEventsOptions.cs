namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Options for <c>AddPersistentEvents()</c>.
/// </summary>
public sealed class PersistentEventsOptions
{
    /// <summary>Hangfire queue name for event dispatch jobs.</summary>
    public string QueueName { get; set; } = "default";
}

