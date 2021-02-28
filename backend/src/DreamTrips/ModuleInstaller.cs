using DreamTravel.DreamTrips.CalculateBestPath;
using DreamTravel.DreamTrips.CalculateBestPath.Executors;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.DreamTrips.FindLocationOfCity;
using DreamTravel.DreamTrips.FindNameOfCity;
using DreamTravel.DreamTrips.LimitCostOfPaths;
using DreamTravel.GeolocationData.Configuration;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DreamTrips
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTrips(this IServiceCollection services)
        {
            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            //CalculateBestPath
            services.AddScoped<ICalculateBestPath, CalculateBestPathHandler>();
            services.AddScoped<IFindProfitablePath, FindProfitablePath>();
            services.AddScoped<IFormPathsFromMatrices, FormPathsFromMatrices>();
            services.AddTransient<IDownloadRoadData, DownloadRoadData>();

            //FindNameOfCIty
            services.AddScoped<IFindNameOfCity, FindNameOfCity.FindNameOfCityHandler>();

            //FindLocationOfCityHandler
            services.AddScoped<IFindLocationOfCity, FindLocationOfCity.FindLocationOfCityHandler>();

            //LimitCostOfPathsHandler
            services.AddScoped<ILimitCostOfPaths, LimitCostOfPaths.LimitCostOfPathsHandler>();

            services.InstallGeolocationData();

            return services;
        }
    }
}
