﻿using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
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
            services.InstallGeolocationDataClients();
            services.InstallInfrastructure();

            services.RegisterQueries();

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            //CalculateBestPath
            services.AddTransient<IFindProfitablePath, FindProfitablePath>();
            services.AddTransient<IFormCalculateBestPathResult, FormCalculateBestPathResult>();
            services.AddTransient<ISolveTsp, SolveTsp>();
            services.AddTransient<IDownloadRoadData, DownloadRoadData>();

            return services;
        }
    }
}
