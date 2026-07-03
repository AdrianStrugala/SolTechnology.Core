namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Options for <c>AddSolPersistentEvents()</c>.
/// </summary>
public sealed class PersistentEventsOptions
{
    /// <summary>Hangfire queue name for event dispatch jobs.</summary>
    public string QueueName { get; set; } = "default";
}

