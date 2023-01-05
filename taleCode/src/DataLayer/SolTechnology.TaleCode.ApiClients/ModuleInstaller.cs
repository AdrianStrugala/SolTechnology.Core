using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;
using SolTechnology.TaleCode.ApiClients.ApiFootballApi;
using SolTechnology.TaleCode.ApiClients.FootballDataApi;

namespace SolTechnology.TaleCode.ApiClients
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallApiClients(this IServiceCollection services)
        {
            services.AddApiClient<IFootballDataApiClient, FootballDataApiClient>("football-data");  //has to match the name from configuration
            services.AddApiClient<IApiFootballApiClient, ApiFootballApiClient>("api-football");  //has to match the name from configuration

            return services;
        }
    }
}
