using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Story.Builder;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story;

/// <summary>
/// Registry of <c>StoryHandler</c> types discovered at startup. The HTTP controller uses
/// this whitelist to resolve handlers by their short name without scanning every loaded
/// assembly.
/// </summary>
public sealed class StoryHandlerRegistry
{
    private readonly ConcurrentDictionary<string, Type> _handlers =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(Type handlerType) => _handlers[handlerType.Name] = handlerType;

    public bool TryResolve(string name, out Type handlerType) =>
        _handlers.TryGetValue(name, out handlerType!);

    public IReadOnlyDictionary<string, Type> AllHandlers => _handlers;
}

/// <summary>
/// DI registration for the Story framework.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Register the Story framework and return a builder for further configuration.
    /// </summary>
    /// <remarks>
    /// Behavior:
    /// <list type="bullet">
    ///   <item>Scans <paramref name="assemblies"/> (or the entry + calling assembly when
    ///   none are supplied) for <see cref="IChapter{TContext}"/> and
    ///   <see cref="StoryHandler{TInput,TContext,TOutput}"/> implementations and registers
    ///   them as <c>Transient</c>.</item>
    ///   <item>Registers <see cref="StoryHandlerRegistry"/> and <see cref="StoryOptions"/>
    ///   as singletons.</item>
    ///   <item>Registers an <see cref="InMemoryStoryRepository"/> as the default
    ///   <see cref="IStoryRepository"/> and the <see cref="StoryManager"/> as
    ///   <c>Scoped</c>. The repository can be replaced through the returned
    ///   <see cref="IStoryBuilder"/> (<c>UseSqliteStoryRepository</c>,
    ///   <c>UseStoryRepository&lt;T&gt;</c>). A repository is always registered — the
    ///   minimum is in-memory.</item>
    /// </list>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional <see cref="StoryOptions"/> mutator.</param>
    /// <param name="assemblies">Assemblies to scan for chapters and handlers.</param>
    public static IStoryBuilder RegisterStories(
        this IServiceCollection services,
        Action<StoryOptions>? configure = null,
        params Assembly[] assemblies)
    {
        // Idempotent: multiple modules may each call this with their own assembly.
        var existingOptions = services
            .FirstOrDefault(d => d.ServiceType == typeof(StoryOptions))?.ImplementationInstance as StoryOptions;
        var options = existingOptions ?? new StoryOptions();
        configure?.Invoke(options);
        if (existingOptions is null)
        {
            services.AddSingleton(options);
        }

        var existingRegistry = services
            .FirstOrDefault(d => d.ServiceType == typeof(StoryHandlerRegistry))?.ImplementationInstance as StoryHandlerRegistry;
        var registry = existingRegistry ?? new StoryHandlerRegistry();

        var targets = assemblies is { Length: > 0 }
            ? assemblies.Distinct().ToArray()
            : DefaultScanAssemblies();

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

                if (InheritsFromOpenGeneric(type, typeof(StoryHandler<,,>)))
                {
                    services.AddTransient(type);
                    registry.Register(type);
                }
            }
        }

        if (existingRegistry is null)
        {
            services.AddSingleton(registry);
            services.TryAddSingleton<IStoryRepository>(_ => new InMemoryStoryRepository());
            services.AddScoped<StoryManager>();
        }

        return new StoryBuilder(services, options);
    }

    private static Assembly[] DefaultScanAssemblies()
    {
        var list = new HashSet<Assembly>();
        var entry = Assembly.GetEntryAssembly();
        if (entry != null) list.Add(entry);
        var calling = Assembly.GetCallingAssembly();
        if (calling != null) list.Add(calling);
        return list.ToArray();
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);

    private static bool InheritsFromOpenGeneric(Type type, Type openGeneric)
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
