using DreamTravel.Identity.DatabaseData;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection
            InstallIdentityCommands(this IServiceCollection services)
        {
            services.AddCommands();

            services.InstallDatabaseData();

            return services;
        }
    }
}
