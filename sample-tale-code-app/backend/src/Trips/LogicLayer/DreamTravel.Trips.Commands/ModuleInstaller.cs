using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Trips.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services)
        {
            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            return services;
        }
    }
}
