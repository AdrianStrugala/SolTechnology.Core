using System;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterAllImplementations(this IServiceCollection services, Type genericInterface)
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
