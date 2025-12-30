using DreamTravel.GeolocationDataClients.GeoDb;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.GeolocationDataClients.MichelinApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.HTTP;

namespace DreamTravel.GeolocationDataClients
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationDataClients(this IServiceCollection services)
        {
            services.AddHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
            services.Decorate(typeof(IGoogleHTTPClient), typeof(GoogleHTTPClientCachingDecorator));

            services.AddHTTPClient<IMichelinHTTPClient, MichelinHTTPClient, MichelinHTTPOptions>("Michelin");
            services.AddHTTPClient<IGeoDbHTTPClient, GeoDbHTTPClient>("GeoDb");

            return services;
        }
    }
}
