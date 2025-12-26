using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallTripsQueries(this IServiceCollection services)
        {
            services.RegisterQueries();
            services.RegisterStories();

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            return services;
        }
    }
}
