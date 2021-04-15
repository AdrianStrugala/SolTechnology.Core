using DreamTravel.DreamTrips.CalculateBestPath.Executors;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.GeolocationData.Configuration;
using DreamTravel.Infrastructure;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DreamTrips
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTrips(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(IQueryHandler<,>));

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
