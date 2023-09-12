using DreamTravel.GeolocationData;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsQueries(this IServiceCollection services)
        {
            services.RegisterQueries();

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            //CalculateBestPath
            services.AddTransient<IFindProfitablePath, FindProfitablePath>();
            services.AddTransient<IFormPathsFromMatrices, FormPathsFromMatrices>();
            services.AddTransient<IDownloadRoadData, DownloadRoadData>();


            services.InstallGeolocationDataClients();

            return services;
        }
    }
}
