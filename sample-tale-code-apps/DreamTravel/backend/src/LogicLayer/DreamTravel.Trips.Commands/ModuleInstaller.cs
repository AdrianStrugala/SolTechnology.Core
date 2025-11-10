using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Commands.DomainServices;
using DreamTravel.Trips.Commands.DomainServices.CityDomain;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services)
        {
            //Domain services
            services.AddScoped<ICityDomainService, CityDomainService>();
            services.AddScoped<ICityStatisticsDomainService, CityStatisticsDomainService>();

            //Commands
            services.RegisterCommands();

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            return services;
        }
    }
}
