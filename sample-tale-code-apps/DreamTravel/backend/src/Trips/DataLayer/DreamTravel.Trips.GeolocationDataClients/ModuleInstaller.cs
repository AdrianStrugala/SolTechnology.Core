using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.GeolocationData.MichelinApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;

namespace DreamTravel.GeolocationData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationDataClients(this IServiceCollection services)
        {
            services.AddApiClient<IGoogleApiClient, GoogleApiClient, GoogleApiOptions>("Google");
            services.AddApiClient<IMichelinApiClient, MichelinApiClient, MichelinApiOptions>("Michelin");

            return services;
        }
    }
}
