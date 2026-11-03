using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Per-run switches that only <see cref="TaleManager"/> sets: whether the run is persisted and which
/// stored instance it resumes. A direct <c>Handle</c> call uses <see cref="Direct"/>.
/// </summary>
internal sealed class TaleRunOptions
{
    public static TaleRunOptions Direct { get; } = new();

    public bool Persist { get; init; }

    public TaleInstance? Resume { get; init; }

    public string? IdempotencyKey { get; init; }
}
