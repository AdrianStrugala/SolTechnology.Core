using DreamTravel.FlightProviderData.Airports;
using DreamTravel.FlightProviderData.Flights.GetFlights;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.FlightProviderData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallFlightProviderData(this IServiceCollection services)
        {
            services.AddTransient<IAirportRepository, AirportRepository>();
            services.AddTransient<IFlightRepository, FlightRepository>();

            return services;
        }
    }
}
