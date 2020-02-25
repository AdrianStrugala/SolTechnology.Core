using DreamTravel.Domain.Airports;
using DreamTravel.FlightProviderData.Airports;
using DreamTravel.FlightProviderData.Query.GetFlights;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.FlightProviderData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallFlightProviderData(this IServiceCollection services)
        {
            services.AddTransient<IAirportRepository, AirportRepository>();
            services.AddTransient<IGetFlights, GetFlights>();

            return services;
        }
    }
}
