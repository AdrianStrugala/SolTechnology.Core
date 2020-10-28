using DreamTravel.Domain.Airports;
using DreamTravel.FlightProviderData.Query.GetFlights;
using DreamTravel.FlightProviderData.Repository.Airports;
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
