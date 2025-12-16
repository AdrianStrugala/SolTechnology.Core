using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries
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
