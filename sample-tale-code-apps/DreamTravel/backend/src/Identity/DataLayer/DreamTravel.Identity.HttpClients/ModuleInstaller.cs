using DreamTravel.Identity.HttpClients.Aiia;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;

namespace DreamTravel.Identity.HttpClients
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallHttpClients(this IServiceCollection services)
        {
            services.AddApiClient<IAiiaApi, AiiaApi>("Aiia");  //has to match the name from configuration

            return services;
        }
    }
}
