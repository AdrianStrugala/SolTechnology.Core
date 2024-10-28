using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
using DreamTravel.Infrastructure.Events;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.PipelineBehaviors;

namespace DreamTravel.Trips.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsQueries(this IServiceCollection services)
        {
            var thisAssembly = typeof(ModuleInstaller).Assembly;

            services.RegisterQueries();
            services.AddMediatR(
                config =>
                {
                    config.RegisterServicesFromAssembly(thisAssembly);
                    config.AddOpenBehavior(typeof(FluentValidationPipelineBehavior<,>));
                    config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
                });
            services.AddValidatorsFromAssembly(thisAssembly);

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            //CalculateBestPath
            services.AddTransient<IFindProfitablePath, FindProfitablePath>();
            services.AddTransient<IFormCalculateBestPathResult, FormCalculateBestPathResult>();
            services.AddTransient<ISolveTsp, SolveTsp>();
            services.AddTransient<IDownloadRoadData, DownloadRoadData>();

            services.InstallGeolocationDataClients();
            services.InstallInfrastructure();

            return services;
        }
    }
}
