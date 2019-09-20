using DreamTravel.GeolocationData.Cities;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GeolocationData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationData(this IServiceCollection services)
        {
            services.AddTransient<ICityRepository, CityRepository>();

            return services;
        }
    }
}
