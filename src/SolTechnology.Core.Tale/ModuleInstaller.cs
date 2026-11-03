using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Tale.Builder;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

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

    public bool TryResolve(string name, out Type handlerType) =>
        _exposed.TryGetValue(name, out handlerType!);

    public IReadOnlyDictionary<string, Type> AllHandlers => _exposed;
}

/// <summary>
/// DI registration for the Tale framework.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Register the Tale framework and return a builder for further configuration.
    /// </summary>
    /// <remarks>
    /// Behavior:
    /// <list type="bullet">
    ///   <item>Scans <paramref name="assemblies"/> (or the entry + calling assembly when
    ///   none are supplied) for <see cref="IChapter{TContext}"/> and
    ///   <see cref="TaleHandler{TInput,TContext,TOutput}"/> implementations and registers
    ///   them as <c>Transient</c>.</item>
    ///   <item>Registers <see cref="TaleHandlerRegistry"/> and <see cref="TaleOptions"/>
    ///   as singletons.</item>
    ///   <item>Registers an <see cref="InMemoryTaleRepository"/> as the default
    ///   <see cref="ITaleRepository"/> and the <see cref="TaleManager"/> as
    ///   <c>Scoped</c>. The repository can be replaced through the returned
    ///   <see cref="ITaleBuilder"/> (<c>UseTaleRepository&lt;T&gt;</c>). A repository is always registered — the
    ///   minimum is in-memory.</item>
    /// </list>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional <see cref="TaleOptions"/> mutator.</param>
    /// <param name="assemblies">Assemblies to scan for chapters and handlers.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ITaleBuilder AddSolTale(
        this IServiceCollection services,
        Action<TaleOptions>? configure = null,
        params Assembly[] assemblies)
    {
        // Idempotent: multiple modules may each call this with their own assembly.
        var existingOptions = services
            .FirstOrDefault(d => d.ServiceType == typeof(TaleOptions))?.ImplementationInstance as TaleOptions;
        var options = existingOptions ?? new TaleOptions();
        configure?.Invoke(options);
        if (existingOptions is null)
        {
            services.AddSingleton(options);
        }

        var existingRegistry = services
            .FirstOrDefault(d => d.ServiceType == typeof(TaleHandlerRegistry))?.ImplementationInstance as TaleHandlerRegistry;
        var registry = existingRegistry ?? new TaleHandlerRegistry();

        var targets = assemblies is { Length: > 0 }
            ? assemblies.Distinct().ToArray()
            : DefaultScanAssemblies(Assembly.GetCallingAssembly());

        foreach (var assembly in targets)
        {
            Type[] exportedTypes;
            try
            {
                exportedTypes = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                exportedTypes = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }

            foreach (var type in exportedTypes)
            {
                if (!type.IsClass || type.IsAbstract) continue;

                if (ImplementsOpenGeneric(type, typeof(IChapter<>)))
                {
                    services.AddTransient(type);
                }

                if (InheritsFromOpenGeneric(type, typeof(TaleHandler<,,>)))
                {
                    services.AddTransient(type);
                    registry.Register(type);
                }
            }
        }

        if (existingRegistry is null)
        {
            services.AddSingleton(registry);
            services.TryAddSingleton<ITaleRepository>(_ => new InMemoryTaleRepository());
            services.AddScoped<TaleManager>();
        }

        return new TaleBuilder(services, options, registry);
    }

    private static Assembly[] DefaultScanAssemblies(Assembly calling)
    {
        var list = new HashSet<Assembly> { calling };
        var entry = Assembly.GetEntryAssembly();
        if (entry != null) list.Add(entry);
        return list.ToArray();
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);

    internal static bool InheritsFromOpenGeneric(Type type, Type openGeneric)
    {
        var t = type.BaseType;
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric) return true;
            t = t.BaseType;
        }
        return false;
    }
}
