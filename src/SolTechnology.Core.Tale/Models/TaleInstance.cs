using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolTechnology.Core.Tale.Models;

/// <summary>
/// Represents a persisted tale instance with its current state.
/// Used by repositories to save and restore tale execution state.
/// </summary>
public class TaleInstance
{
    /// <summary>Unique identifier for this tale instance.</summary>
    public Auid TaleId { get; set; } = default!;

    /// <summary>
    /// Type name of the TaleHandler (short name by default; can be assembly-qualified if you configure it).
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

    /// <summary>Current status of the tale.</summary>
    public TaleStatus Status { get; set; } = TaleStatus.Created;

    /// <summary>When this tale instance was first created. Never updated.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this tale instance was last updated.</summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The complete context for this tale, serialized as JSON.</summary>
    public string Context { get; set; } = string.Empty;

    [JsonConstructor]
    public TaleInstance() { }

    /// <summary>
    /// Create a deep clone of this tale instance (used by in-memory repository
    /// to emulate database immutability).
    /// </summary>
    public TaleInstance Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<TaleInstance>(json)!;
    }
}
