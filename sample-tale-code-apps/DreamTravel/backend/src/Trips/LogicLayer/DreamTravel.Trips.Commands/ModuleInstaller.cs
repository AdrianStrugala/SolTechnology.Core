using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Sql;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Trips.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamTripsCommands(this IServiceCollection services, IConfiguration configuration)
        {
            var thisAssembly = typeof(ModuleInstaller).Assembly;

            services.InstallSql(configuration);
            services.InstallGeolocationDataClients();
            services.InstallInfrastructure();

            //TSP engine
            services.AddScoped<ITSP, AntColony>();

            services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
            services.AddValidatorsFromAssembly(thisAssembly);

            return services;
        }
    }
}
