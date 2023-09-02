using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.CQRS
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(ICommandHandler<>));
            services.RegisterAllImplementations(typeof(ICommandHandler<,>));

            return services;
        }

        public static IServiceCollection AddQueries(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(IQueryHandler<,>));

            return services;
        }

        private static IServiceCollection RegisterAllImplementations(this IServiceCollection services, Type genericInterface)
        {
            if (!genericInterface.IsInterface || !genericInterface.IsGenericType)
            {
                throw new ArgumentException("Invalid generic interface type", nameof(genericInterface));
            }

            var types = Assembly.GetCallingAssembly()
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
}
