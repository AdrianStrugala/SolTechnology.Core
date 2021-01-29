using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.GeolocationData.MichelinApi;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GeolocationData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationData(this IServiceCollection services)
        {
            services.AddTransient<IMichelinApiClient, MichelinApiClient>();
            services.AddTransient<IGoogleApiClient, GoogleApiClient>();

            return services;
        }
    }
}
