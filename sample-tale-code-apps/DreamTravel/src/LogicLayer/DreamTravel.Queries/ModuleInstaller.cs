﻿using DreamTravel.Queries.CalculateBestPath;
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
            // Explicit assembly: GetCallingAssembly() is unreliable under JIT inlining / WAF.
            services.RegisterStories(assemblies: typeof(CalculateBestPathStory).Assembly);

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            return services;
        }
    }
}


