using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.GeolocationData.MichelinApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient;
using SolTechnology.Core.Cache;

namespace DreamTravel.GeolocationData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationDataClients(this IServiceCollection services)
        {
            services.AddCache();

            services.AddApiClient<IGoogleApiClient, GoogleApiClient, GoogleApiOptions>("Google");
            services.Decorate(typeof(IGoogleApiClient), typeof(GoogleApiClientCachingDecorator));

            services.AddApiClient<IMichelinApiClient, MichelinApiClient, MichelinApiOptions>("Michelin");

            return services;
        }
    }
}
