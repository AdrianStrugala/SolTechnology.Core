using DreamTravel.Infrastructure;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DreamTrips
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(IQueryHandler<,>));
            services.RegisterAllImplementations(typeof(IService<,>));

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            return services;
        }
    }
}
