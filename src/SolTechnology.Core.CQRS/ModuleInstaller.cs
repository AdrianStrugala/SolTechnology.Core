using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using SolTechnology.Core.CQRS.Decorators.Logging;
using SolTechnology.Core.CQRS.Decorators.Validation;

namespace SolTechnology.Core.CQRS;

public static class ModuleInstaller
{
    public static IServiceCollection RegisterCommands(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        services.AddValidatorsFromAssembly(callingAssembly);

        services.RegisterAllImplementations(typeof(ICommandHandler<>), callingAssembly);
        try
        {
            services.Decorate(typeof(ICommandHandler<>), typeof(CommandHandlerValidationDecorator<>));
            services.Decorate(typeof(ICommandHandler<>), typeof(CommandHandlerLoggingDecorator<>));
        }
        catch (DecorationException)
        {
            //could happen if no service of the type is registered
        }

        services.RegisterAllImplementations(typeof(ICommandHandler<,>), callingAssembly);
        try
        {
            services.Decorate(typeof(ICommandHandler<,>), typeof(CommandHandlerValidationDecorator<,>));
            services.Decorate(typeof(ICommandHandler<,>), typeof(CommandWithResultHandlerLoggingDecorator<,>));
        }
        catch (DecorationException)
        {
            //could happen if no service of the type is registered
        }


        return services;
    }

    public static IServiceCollection RegisterQueries(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        services.AddValidatorsFromAssembly(callingAssembly);

        services.RegisterAllImplementations(typeof(IQueryHandler<,>), callingAssembly);
        services.Decorate(typeof(IQueryHandler<,>), typeof(QueryHandlerValidationDecorator<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(QueryHandlerLoggingDecorator<,>));

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