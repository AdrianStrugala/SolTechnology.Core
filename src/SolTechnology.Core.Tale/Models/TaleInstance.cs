using System.Text.Json;
using System.Text.Json.Serialization;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Persistence;

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

    /// <summary>
    /// When the state of this tale instance was last saved, in UTC. Lease renewals do not change it.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the Running lease of the run that last saved this tale expires, in UTC; <c>null</c> for
    /// every status other than <see cref="TaleStatus.Running"/>. The claim save and every lease renewal
    /// set it to the writer's clock plus the writer's <see cref="TaleOptions.RunningLeaseTimeout"/>.
    /// <c>ResumeStory</c> refuses a Running tale until this instant has passed. A missing value (a row
    /// written before the field existed, or by a repository that drops it) holds the tale until
    /// <see cref="LastUpdatedAt"/> plus the reader's <see cref="TaleOptions.RunningLeaseTimeout"/> and logs
    /// a warning. Repositories must store and return the instant unchanged on inserts and updates.
    /// </summary>
    public DateTimeOffset? LeaseExpiresAt { get; set; }

    /// <summary>The complete context for this tale, serialized as JSON.</summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Optimistic-concurrency version of the stored state this instance was read at; <c>0</c> when it
    /// has never been stored. <see cref="Persistence.ITaleRepository.Save"/> increments it.
    /// </summary>
    public long Version { get; set; }

    /// <summary>Creates an empty instance; used by serializers and repositories.</summary>
    [JsonConstructor]
    public TaleInstance() { }

    /// <summary>
    /// Create a deep clone of this tale instance (used by in-memory repository
    /// to emulate database immutability).
    /// </summary>
    public TaleInstance Clone()
    {
        var json = JsonSerializer.Serialize(this, TaleJsonOptions.Default);
        return JsonSerializer.Deserialize<TaleInstance>(json, TaleJsonOptions.Default)!;
    }
}
