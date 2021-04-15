using DreamTravel.Domain.Airports;
using DreamTravel.FlightProviderData.Repository.Airports;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.FlightProviderData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallStaticData(this IServiceCollection services)
        {
            services.AddTransient<IAirportRepository, AirportRepository>();

            return services;
        }
    }
}
