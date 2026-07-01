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
    /// Registers the in-house mediator, pipeline behaviors, and the command/query/event handlers
    /// (and optional FluentValidation validators) discovered from the assemblies configured on
    /// <see cref="CQRSOptions"/> via <see cref="CQRSOptions.RegisterCommandsFromAssembly"/>,
    /// <see cref="CQRSOptions.RegisterQueriesFromAssembly"/>, and
    /// <see cref="CQRSOptions.RegisterEventsFromAssembly"/>. Idempotent — safe to call multiple times.
    /// </summary>
    public static IServiceCollection AddSolCQRS(this IServiceCollection services, Action<CQRSOptions>? configure = null)
    {
        var options = new CQRSOptions();
        configure?.Invoke(options);

        // Register mediator (scoped — shares request lifetime)
        services.TryAddScoped<IMediator, CQRSMediator>();

        // Register event dispatch seam defaults (plugin can replace the publisher)
        services.TryAddSingleton<IEventPublisher, InMemoryEventPublisher>();
        services.TryAddScoped<IEventDispatcher, EventDispatcher>();

        // Register pipeline behaviors
        if (options.UseLogging)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>)));
        }

        if (options.UseFluentValidation)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(FluentValidationPipelineBehavior<,>)));
        }

        // Scan the explicitly-registered assemblies, one handler family per registration method
        foreach (var assembly in options.CommandAssemblies)
        {
            RegisterHandlers(services, assembly, typeof(ICommandHandler<>));
            RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
        }

        foreach (var assembly in options.QueryAssemblies)
        {
            RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));
        }

        foreach (var assembly in options.EventAssemblies)
        {
            RegisterHandlers(services, assembly, typeof(IEventHandler<>));
        }

        if (options.UseFluentValidation)
        {
            // Validators guard command/query inputs — scan every distinct registered assembly once
            var validatorAssemblies = options.CommandAssemblies
                .Concat(options.QueryAssemblies)
                .Concat(options.EventAssemblies)
                .Distinct();

            foreach (var assembly in validatorAssemblies)
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
