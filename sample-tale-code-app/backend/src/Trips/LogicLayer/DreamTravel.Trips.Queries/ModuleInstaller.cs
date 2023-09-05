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
            services.AddScoped<ITSP, AntColony>();

            //CalculateBestPath
            services.AddScoped<IFindProfitablePath, FindProfitablePath>();
            services.AddScoped<IFormPathsFromMatrices, FormPathsFromMatrices>();
            services.AddScoped<IDownloadRoadData, DownloadRoadData>();


            services.InstallGeolocationDataClients();

            return services;
        }
    }
}
