using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS.PipelineBehaviors;

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
                config.AddOpenBehavior(typeof(FluentValidationPipelineBehavior<,>));
                config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
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
                config.AddOpenBehavior(typeof(FluentValidationPipelineBehavior<,>));
                config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            });

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
            services.AddTransient(type.Service, type.Implementation);
        }

        return services;
    }
}