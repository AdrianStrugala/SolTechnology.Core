using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services)
        {
            services.RegisterCommands();

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            return services;
        }
    }
}
