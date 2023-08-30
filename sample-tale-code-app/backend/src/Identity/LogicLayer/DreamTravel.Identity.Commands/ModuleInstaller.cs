using DreamTravel.Identity.DatabaseData;
using DreamTravel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Identity.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection 
            InstallIdentityCommands(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(ICommandHandler<>));
            services.RegisterAllImplementations(typeof(IQueryHandler<,>));

            services.InstallDatabaseData();

            return services;
        }
    }
}
