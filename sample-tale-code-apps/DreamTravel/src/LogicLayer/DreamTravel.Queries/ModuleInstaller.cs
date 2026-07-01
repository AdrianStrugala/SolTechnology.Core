﻿﻿﻿using DreamTravel.Queries.CalculateBestPath;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Tale;

namespace DreamTravel.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallTripsQueries(this IServiceCollection services)
        {
            services.AddSolCQRS(o => o.RegisterQueriesFromAssembly(typeof(ModuleInstaller).Assembly));
            // Explicit assembly: GetCallingAssembly() is unreliable under JIT inlining / WAF.
            services.AddSolTale(assemblies: typeof(CalculateBestPathTale).Assembly);

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            return services;
        }
    }
}


