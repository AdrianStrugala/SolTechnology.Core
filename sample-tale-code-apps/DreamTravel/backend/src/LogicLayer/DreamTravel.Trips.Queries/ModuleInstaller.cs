using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallTripsQueries(this IServiceCollection services)
        {
            services.RegisterQueries();
            services.RegisterChain();

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            return services;
        }
    }
}
