using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.CQRS
{
    public static class ModuleInstaller
    {
        public static IServiceCollection RegisterCommands(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(ICommandHandler<>), Assembly.GetCallingAssembly());
            services.RegisterAllImplementations(typeof(ICommandHandler<,>), Assembly.GetCallingAssembly());

            return services;
        }

        public static IServiceCollection RegisterQueries(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(IQueryHandler<,>), Assembly.GetCallingAssembly());

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
}
