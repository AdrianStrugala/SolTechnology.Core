using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services)
        {
            //Commands
            services.RegisterCommands();

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            return services;
        }
    }
}
