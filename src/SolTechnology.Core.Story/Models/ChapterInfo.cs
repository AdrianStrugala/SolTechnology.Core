using System.Text.Json;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.Story.Models;

/// <summary>
/// Tracks execution information for a single chapter.
/// Used for history, debugging, and pause/resume functionality.
/// </summary>
public class ChapterInfo
{
    /// <summary>
    /// Unique identifier for the chapter (usually the type name).
    /// </summary>
    public required string ChapterId { get; set; }

    /// <summary>
    /// Type of chapter: "Automated" or "Interactive".
    /// </summary>
    public string ChapterType { get; set; } = "Automated";

    /// <summary>
    /// When this chapter started executing.
    /// </summary>
    public required DateTime StartedAt { get; set; }

    /// <summary>
    /// When this chapter finished executing.
    /// Null if still in progress or waiting for input.
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Current status of this chapter.
    /// </summary>
    public StoryStatus Status { get; set; } = StoryStatus.Running;

    /// <summary>
    /// For interactive chapters: schema of required input fields.
    /// Empty for automated chapters.
    /// </summary>
    public List<DataField> RequiredData { get; set; } = new();

    /// <summary>
    /// For interactive chapters: the user input that was provided.
    /// Null if input hasn't been provided yet.
    /// </summary>
    public JsonElement? ProvidedData { get; set; }

    /// <summary>
    /// If this chapter failed, contains the error information.
    /// Null if chapter succeeded.
    /// </summary>
    public Error? Error { get; set; }
}
