namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// The ambient Chronicle node of the current async flow. Private <see cref="AsyncLocal{T}"/>
/// behind an internal type, like <c>CorrelationIdService</c>.
/// </summary>
internal static class AmbientChronicle
{
    private static readonly AsyncLocal<ChronicleNode?> Context = new();

    internal static ChronicleNode? Current
    {
        get => Context.Value;
        set => Context.Value = value;
    }

    internal static ChronicleRoot? OpenRoot
        => Context.Value?.Root is { IsClosed: false } root ? root : null;
}
