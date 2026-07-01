using System.Reflection;

namespace SolTechnology.Core.CQRS;

/// <summary>
/// Configuration options for the CQRS module. Handler discovery is explicit: call
/// <see cref="RegisterCommandsFromAssembly"/>, <see cref="RegisterQueriesFromAssembly"/>,
/// and <see cref="RegisterEventsFromAssembly"/> to scan the assemblies containing your handlers.
/// </summary>
public sealed class CQRSOptions
{
    private readonly HashSet<Assembly> _commandAssemblies = [];
    private readonly HashSet<Assembly> _queryAssemblies = [];
    private readonly HashSet<Assembly> _eventAssemblies = [];

    /// <summary>
    /// When true (default), registers <c>FluentValidationPipelineBehavior</c> and scans the
    /// registered command/query/event assemblies for <c>IValidator&lt;T&gt;</c> implementations.
    /// </summary>
    public bool UseFluentValidation { get; set; } = true;

    /// <summary>
    /// When true (default), registers <c>LoggingPipelineBehavior</c> for automatic
    /// operation tracking.
    /// </summary>
    public bool UseLogging { get; set; } = true;

    internal IReadOnlyCollection<Assembly> CommandAssemblies => _commandAssemblies;

    internal IReadOnlyCollection<Assembly> QueryAssemblies => _queryAssemblies;

    internal IReadOnlyCollection<Assembly> EventAssemblies => _eventAssemblies;

    /// <summary>
    /// Discovers and registers command handlers (<c>ICommandHandler&lt;&gt;</c> and
    /// <c>ICommandHandler&lt;,&gt;</c>) from the given <paramref name="assemblies"/>.
    /// </summary>
    public CQRSOptions RegisterCommandsFromAssembly(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            _commandAssemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>
    /// Discovers and registers query handlers (<c>IQueryHandler&lt;,&gt;</c>) from the given
    /// <paramref name="assemblies"/>.
    /// </summary>
    public CQRSOptions RegisterQueriesFromAssembly(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            _queryAssemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>
    /// Discovers and registers event handlers (<c>IEventHandler&lt;&gt;</c>) from the given
    /// <paramref name="assemblies"/>.
    /// </summary>
    public CQRSOptions RegisterEventsFromAssembly(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            _eventAssemblies.Add(assembly);
        }

        return this;
    }
}

