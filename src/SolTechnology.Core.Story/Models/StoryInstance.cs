using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolTechnology.Core.Story.Models;

/// <summary>
/// Represents a persisted story instance with its current state.
/// Used by repositories to save and restore story execution state.
/// </summary>
public class StoryInstance
{
    /// <summary>Unique identifier for this story instance.</summary>
    public Auid StoryId { get; set; } = default!;

    /// <summary>
    /// Type name of the StoryHandler (short name by default; can be assembly-qualified if you configure it).
    /// Used to resolve the correct handler type when resuming.
    /// </summary>
    public string HandlerTypeName { get; set; } = default!;


    /// <summary>
    /// Optional idempotency key provided by the caller. Repositories may use this to deduplicate
    /// <c>StartStory</c> requests that repeat due to network retries.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Current chapter being executed (or waiting for input).</summary>
    public ChapterInfo? CurrentChapter { get; set; }

    /// <summary>Execution history — all chapters that have been executed so far, chronologically.</summary>
    public List<ChapterInfo> History { get; set; } = new();

    /// <summary>Current status of the story.</summary>
    public StoryStatus Status { get; set; } = StoryStatus.Created;

    /// <summary>When this story instance was first created. Never updated.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this story instance was last updated.</summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The complete context for this story, serialized as JSON.</summary>
    public string Context { get; set; } = string.Empty;

    [JsonConstructor]
    public StoryInstance() { }

    /// <summary>
    /// Create a deep clone of this story instance (used by in-memory repository
    /// to emulate database immutability).
    /// </summary>
    public StoryInstance Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<StoryInstance>(json)!;
    }
}
