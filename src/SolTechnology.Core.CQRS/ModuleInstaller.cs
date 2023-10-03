using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS.Decorators.Logging;
using SolTechnology.Core.CQRS.Decorators.Validation;

namespace SolTechnology.Core.CQRS;

public static class ModuleInstaller
{
    public static IServiceCollection RegisterCommands(this IServiceCollection services)
    {
        services.RegisterAllImplementations(typeof(ICommandHandler<>), Assembly.GetCallingAssembly());
        services.Decorate(typeof(ICommandHandler<>), typeof(CommandHandlerValidationDecorator<>));
        services.Decorate(typeof(ICommandHandler<>), typeof(CommandHandlerLoggingDecorator<>));

        services.RegisterAllImplementations(typeof(ICommandHandler<,>), Assembly.GetCallingAssembly());
        services.Decorate(typeof(ICommandHandler<,>), typeof(CommandHandlerValidationDecorator<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(CommandWithResultHandlerLoggingDecorator<,>));

        return services;
    }

    public static IServiceCollection RegisterQueries(this IServiceCollection services)
    {
        services.RegisterAllImplementations(typeof(IQueryHandler<,>), Assembly.GetCallingAssembly());
        services.Decorate(typeof(IQueryHandler<,>), typeof(QueryHandlerValidationDecorator<,>));

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