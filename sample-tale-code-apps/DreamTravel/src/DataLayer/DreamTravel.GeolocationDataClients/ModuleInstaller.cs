using DreamTravel.GeolocationDataClients.GeoDb;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.GeolocationDataClients.MichelinApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;

namespace DreamTravel.GeolocationDataClients
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationDataClients(this IServiceCollection services)
        {
            services.AddApiClient<IGoogleApiClient, GoogleApiClient, GoogleApiOptions>("Google");
            services.Decorate(typeof(IGoogleApiClient), typeof(GoogleApiClientCachingDecorator));

            services.AddApiClient<IMichelinApiClient, MichelinApiClient, MichelinApiOptions>("Michelin");
            services.AddApiClient<IGeoDbApiClient, GeoDbApiClient>("GeoDb");

            return services;
        }
    }
}
