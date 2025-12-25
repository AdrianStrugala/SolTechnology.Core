using System.Text.Json;

namespace SolTechnology.Core.Story.Models;

/// <summary>
/// Represents a persisted story instance with its current state.
/// Used by repositories to save and restore story execution state.
/// </summary>
public class StoryInstance
{
    /// <summary>
    /// Unique identifier for this story instance.
    /// </summary>
    public string StoryId { get; init; } = default!;

    /// <summary>
    /// Assembly-qualified type name of the StoryHandler.
    /// Used to resolve the correct handler type when resuming.
    /// </summary>
    public string HandlerTypeName { get; init; } = default!;

    /// <summary>
    /// Current chapter being executed (or waiting for input).
    /// Null if the story hasn't started or has completed.
    /// </summary>
    public ChapterInfo? CurrentChapter { get; set; }

    /// <summary>
    /// Execution history - all chapters that have been executed so far.
    /// Ordered chronologically.
    /// </summary>
    public List<ChapterInfo> History { get; set; } = new();

    /// <summary>
    /// Current status of the story.
    /// </summary>
    public StoryStatus Status { get; set; } = StoryStatus.Created;

    /// <summary>
    /// When this story instance was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this story instance was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The complete narration (context) for this story.
    /// Stored as JSON string to support any narration type.
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Constructor for creating a new story instance.
    /// </summary>
    public StoryInstance(string storyId, string handlerTypeName, string context)
    {
        StoryId = storyId;
        HandlerTypeName = handlerTypeName;
        Context = context;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        Status = StoryStatus.Created;
    }

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public StoryInstance()
    {
    }

    /// <summary>
    /// Create a deep clone of this story instance.
    /// Used by InMemoryRepository to simulate database immutability.
    /// </summary>
    public StoryInstance Clone()
    {
        // Serialize to JSON and deserialize to create a deep copy
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<StoryInstance>(json)!;
    }
}
