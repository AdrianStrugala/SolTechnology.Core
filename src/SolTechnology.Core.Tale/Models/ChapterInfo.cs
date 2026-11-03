using System.Text.Json;
using SolTechnology.Core;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.Tale.Models;

/// <summary>
/// Tracks execution information for a single chapter.
/// Used for history, debugging, and pause/resume functionality.
/// </summary>
public class ChapterInfo
{
    /// <summary>
    /// Display identifier of the chapter (<see cref="IChapter{TContext}.ChapterId"/>, the type name by
    /// default). Not used to match history; see <see cref="StepIndex"/>.
    /// </summary>
    public required string ChapterId { get; set; }

    /// <summary>
    /// Position of the chapter's step in the tale's <c>Tell()</c> plan. Together with
    /// <see cref="ChapterTypeName"/> it identifies the chapter in the history. <c>null</c> for history
    /// written by SolTechnology.Core.Tale 1.x, which cannot be resumed.
    /// </summary>
    public int? StepIndex { get; set; }

    /// <summary>
    /// Full type name of the chapter without assembly information (namespace, name and generic
    /// arguments), so the history key survives a redeploy.
    /// </summary>
    public string? ChapterTypeName { get; set; }

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
    public TaleStatus Status { get; set; } = TaleStatus.Running;

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
