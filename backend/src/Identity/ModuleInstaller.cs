using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Identity
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallIdentity(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(ICommandHandler<>));
            services.RegisterAllImplementations(typeof(IQueryHandler<,>));

            services.InstallDatabaseData();

            return services;
        }
    }
}
