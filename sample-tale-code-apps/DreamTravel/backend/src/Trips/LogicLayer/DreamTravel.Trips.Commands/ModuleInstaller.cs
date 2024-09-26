using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Trips.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services, IConfiguration configuration)
        {
            services.InstallSql(configuration);

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            return services;
        }
    }
}
