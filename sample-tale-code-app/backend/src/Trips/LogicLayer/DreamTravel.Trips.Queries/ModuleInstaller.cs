using DreamTravel.GeolocationData.Configuration;
using DreamTravel.Infrastructure;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using DreamTravel.Trips.Queries.CalculateBestPath.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Trips.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsQueries(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(IQueryHandler<,>));
            services.RegisterAllImplementations(typeof(IService<,>));

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            //CalculateBestPath
            services.AddScoped<IFindProfitablePath, FindProfitablePath>();
            services.AddScoped<IFormPathsFromMatrices, FormPathsFromMatrices>();
            services.AddScoped<IDownloadRoadData, DownloadRoadData>();


            services.InstallGeolocationData();

            return services;
        }
    }
}
