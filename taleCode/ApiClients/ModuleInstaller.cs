using ApiClients.FootballDataApi;
using Microsoft.Extensions.DependencyInjection;

namespace ApiClients
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiClients(this IServiceCollection services)
        {
            services.AddScoped<IFootballDataApiClient, FootballDataApiClient>();

            return services;
        }
    }
}
