using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS.PipelineBehaviors;
using SolTechnology.Core.CQRS.SuperChain;

namespace SolTechnology.Core.CQRS;

public static class ModuleInstaller
{
    public static IServiceCollection RegisterCommands(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();

        services.RegisterAllImplementations(typeof(ICommandHandler<>), callingAssembly);
        services.RegisterAllImplementations(typeof(ICommandHandler<,>), callingAssembly);

        services.AddValidatorsFromAssembly(callingAssembly);

        services.AddMediatR(
            config =>
            {
                config.RegisterServicesFromAssembly(callingAssembly);
                config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
                config.AddOpenBehavior(typeof(FluentValidationPipelineBehavior<,>));
            });

        return services;
    }

    public static IServiceCollection RegisterQueries(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        services.RegisterAllImplementations(typeof(IQueryHandler<,>), callingAssembly);

        services.AddValidatorsFromAssembly(callingAssembly);

        services.AddMediatR(
            config =>
            {
                config.RegisterServicesFromAssembly(callingAssembly);
                config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
                config.AddOpenBehavior(typeof(FluentValidationPipelineBehavior<,>));
            });

        return services;
    }
    
    /// <summary>
    /// Scans the specified assembly (or assemblies) for non-abstract classes that implement IChainStep&lt;TContext&gt;
    /// and registers them as transient.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the registrations to.</param>
    /// <returns>The modified IServiceCollection.</returns>
    public static IServiceCollection RegisterChain(this IServiceCollection services)
    {
        Assembly[] assemblies = [Assembly.GetCallingAssembly()];

        var chainStepInterfaceType = typeof(IChainStep<>);

        var typesFromAssemblies = assemblies.SelectMany(a => a.GetExportedTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == chainStepInterfaceType));

        foreach (var type in typesFromAssemblies)
        {
            services.AddTransient(type);
        }

        return services;
    }


    private static IServiceCollection RegisterAllImplementations(this IServiceCollection services, Type genericInterface, Assembly assembly)
    {
        if (!genericInterface.IsInterface || !genericInterface.IsGenericType)
        {
            throw new ArgumentException("Invalid generic interface type", nameof(genericInterface));
        }

        var types = assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterfaces()
                    .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterface),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var type in types)
        {
            services.AddTransient(type.Service!, type.Implementation);
        }

        return services;
    }
}