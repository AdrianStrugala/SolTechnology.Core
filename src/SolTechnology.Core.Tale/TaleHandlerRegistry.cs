using System.Collections.Concurrent;
using SolTechnology.Core.Tale.Builder;

namespace SolTechnology.Core.Tale;

/// <summary>
/// Registry of <c>TaleHandler</c> types for the HTTP controller. It knows every handler that
/// <c>AddSolTale</c> scanned, but resolves only those opted in with
/// <see cref="ITaleBuilder.ExposeOverHttp{THandler}"/>.
/// </summary>
public sealed class TaleHandlerRegistry
{
    private readonly ConcurrentDictionary<string, Type> _scanned = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Type> _exposed = new(StringComparer.OrdinalIgnoreCase);

    internal void Register(Type handlerType) => _scanned[handlerType.Name] = handlerType;

    internal void Expose(Type handlerType)
    {
        Register(handlerType);
        _exposed[handlerType.Name] = handlerType;
    }

    internal bool TryResolveScanned(string name, out Type handlerType) =>
        _scanned.TryGetValue(name, out handlerType!);

    /// <summary>Finds an exposed handler by its short type name (case-insensitive).</summary>
    /// <param name="name">Short type name of the handler, as used in the HTTP route.</param>
    /// <param name="handlerType">The handler type when found.</param>
    /// <returns><c>true</c> when the handler is exposed over HTTP.</returns>
    public bool TryResolve(string name, out Type handlerType) =>
        _exposed.TryGetValue(name, out handlerType!);

    /// <summary>Every handler exposed over HTTP, keyed by short type name.</summary>
    public IReadOnlyDictionary<string, Type> AllHandlers => _exposed;
}
