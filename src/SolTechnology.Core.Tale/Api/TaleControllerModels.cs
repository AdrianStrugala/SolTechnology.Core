using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Api;

/// <summary>DTO for <see cref="TaleInstance"/> over HTTP.</summary>
public class TaleInstanceDto
{
    /// <summary>The tale identifier as a string.</summary>
    public string TaleId { get; set; } = default!;

    /// <summary>Short type name of the handler that runs the tale.</summary>
    public string HandlerTypeName { get; set; } = default!;

    /// <summary>Current status of the tale.</summary>
    public TaleStatus Status { get; set; }

    /// <summary>The chapter waiting for input, with its input schema; null unless the tale is paused.</summary>
    public ChapterInfo? CurrentChapter { get; set; }

    /// <summary>Chapters executed so far, in order.</summary>
    public List<ChapterInfo> History { get; set; } = new();

    /// <summary>When the tale was first stored (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the tale was last stored (UTC).</summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>Maps a stored <see cref="TaleInstance"/> to the HTTP shape, without its serialized context.</summary>
    public static TaleInstanceDto FromModel(TaleInstance instance) => new()
    {
        TaleId = instance.TaleId.ToString(),
        HandlerTypeName = instance.HandlerTypeName,
        Status = instance.Status,
        CurrentChapter = instance.CurrentChapter,
        History = instance.History,
        CreatedAt = instance.CreatedAt,
        LastUpdatedAt = instance.LastUpdatedAt
    };
}

/// <summary>Result envelope returned by <c>GET /api/tale/{id}/result</c>.</summary>
public class TaleResultDto
{
    /// <summary>The tale identifier as a string.</summary>
    public string TaleId { get; set; } = default!;

    /// <summary>Status of the tale; always <see cref="TaleStatus.Completed"/> in a successful response.</summary>
    public TaleStatus Status { get; set; }

    /// <summary>The tale's output, read from its stored context.</summary>
    public object? Output { get; set; }
}
