using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Jobs;

namespace DreamTravel.Infrastructure
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallInfrastructure(this IServiceCollection services)
        {
            services.AddSolHangfire();

            return services;
        }
    }
}
