using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Tale.Builder;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale;

/// <summary>
/// DI registration for the Tale framework.
/// </summary>
public static class ModuleInstaller
{
    extension(IServiceCollection services)
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
        /// <param name="configure">Optional <see cref="TaleOptions"/> mutator.</param>
        /// <param name="assemblies">Assemblies to scan for chapters and handlers.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ITaleBuilder AddSolTale(Action<TaleOptions>? configure = null, params Assembly[] assemblies)
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

            foreach (var type in targets.SelectMany(ExportedTypes).Where(t => t.IsClass && !t.IsAbstract))
            {
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

            if (existingRegistry is null)
            {
                services.AddSingleton(registry);
                services.TryAddSingleton<ITaleRepository>(_ => new InMemoryTaleRepository());
                services.AddScoped<TaleManager>();
            }

            return new TaleBuilder(services, options, registry);
        }
    }

    private static Type[] ExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }
    }

    private static Assembly[] DefaultScanAssemblies(Assembly calling)
    {
        var list = new HashSet<Assembly> { calling };
        var entry = Assembly.GetEntryAssembly();
        if (entry != null)
        {
            list.Add(entry);
        }

        return list.ToArray();
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);

    internal static bool InheritsFromOpenGeneric(Type type, Type openGeneric)
    {
        for (var t = type.BaseType; t != null && t != typeof(object); t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
            {
                return true;
            }
        }

        return false;
    }
}
