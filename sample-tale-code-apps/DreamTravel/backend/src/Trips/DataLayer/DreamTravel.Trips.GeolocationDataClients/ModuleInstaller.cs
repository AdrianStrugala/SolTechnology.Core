using DreamTravel.Trips.GeolocationDataClients.GeoDb;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using DreamTravel.Trips.GeolocationDataClients.MichelinApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;

namespace DreamTravel.Trips.GeolocationDataClients
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
