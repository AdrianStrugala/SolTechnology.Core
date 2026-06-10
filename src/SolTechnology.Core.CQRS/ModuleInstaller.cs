﻿using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.CQRS.Internal;
using SolTechnology.Core.CQRS.PipelineBehaviors;

namespace SolTechnology.Core.CQRS;

/// <summary>
/// Registration entrypoint for the CQRS module.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Registers the in-house mediator, pipeline behaviors, command/query/event handlers,
    /// and (optionally) FluentValidation validators from the specified assemblies.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    public static IServiceCollection AddCQRS(this IServiceCollection services, Action<CQRSOptions>? configure = null, params Assembly[] assemblies)
    {
        var options = new CQRSOptions();
        configure?.Invoke(options);

        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        // Register mediator (scoped — shares request lifetime)
        services.TryAddScoped<IMediator, CQRSMediator>();

        // Register pipeline behaviors
        if (options.UseLogging)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>)));
        }

        if (options.UseFluentValidation)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(FluentValidationPipelineBehavior<,>)));
        }

        // Scan assemblies for handlers and validators
        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly, typeof(ICommandHandler<>));
            RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
            RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));
            RegisterHandlers(services, assembly, typeof(IEventHandler<>));

            if (options.UseFluentValidation)
            {
                services.AddValidatorsFromAssembly(assembly);
            }
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type genericInterface)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);

            foreach (var serviceType in interfaces)
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(serviceType, type));
            }
        }
    }
}
