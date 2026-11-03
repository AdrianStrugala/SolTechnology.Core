using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SolTechnology.Core.Tale.Persistence;

/// <summary>
/// JSON serializer options the Tale framework uses for persisted state. Custom
/// <see cref="ITaleRepository"/> implementations must serialize <c>TaleInstance</c> parts
/// (<c>History</c>, <c>CurrentChapter</c>) with <see cref="Default"/> so that
/// <see cref="TalePausedError"/> and <see cref="TaleCancelledError"/> keep their types.
/// </summary>
public static class TaleJsonOptions
{
    /// <summary>
    /// Read-only options: case-insensitive names, fields included, cycles ignored, and the
    /// <c>talePaused</c> / <c>taleCancelled</c> discriminators registered on <see cref="Error"/>.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            // Tale Context may carry EF/domain entities with bidirectional navs.
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddTaleErrorTypes } }
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    private static void AddTaleErrorTypes(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(Error) || typeInfo.PolymorphismOptions is null)
        {
            return;
        }

        typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(TalePausedError), "talePaused"));
        typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(TaleCancelledError), "taleCancelled"));
    }
}
