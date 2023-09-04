using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.GeolocationData.MichelinApi;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GeolocationData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationDataClients(this IServiceCollection services)
        {
            services.AddTransient<IMichelinApiClient, MichelinApiClient>();
            services.AddTransient<IGoogleApiClient, GoogleApiClient>();

            return services;
        }
    }
}
