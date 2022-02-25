using ApiClients.FootballDataApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;

namespace ApiClients
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiClients(this IServiceCollection services)
        {
            services.AddApiClient<IFootballDataApiClient, FootballDataApiClient>("football-data");  //has to match the name from configuration

            return services;
        }
    }
}
