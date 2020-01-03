using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Matrices;
using DreamTravel.GeolocationData.Cities;
using DreamTravel.GeolocationData.Matrices;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GeolocationData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationData(this IServiceCollection services)
        {
            services.AddTransient<ICityRepository, CityRepository>();
            services.AddTransient<IMatrixRepository, MatrixRepository>();

            return services;
        }
    }
}
