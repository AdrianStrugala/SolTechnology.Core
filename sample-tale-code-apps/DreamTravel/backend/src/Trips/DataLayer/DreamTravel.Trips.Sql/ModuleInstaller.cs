using DreamTravel.Trips.Sql.Repositories;
using EntityGraphQL.AspNet;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;

namespace DreamTravel.Trips.Sql
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallTripsSql(this IServiceCollection services, SqlConfiguration sqlConfiguration)
        {
            services.AddSql(sqlConfiguration);
            
            services.AddDbContext<DreamTripsDbContext>(options =>
                options.UseSqlServer(sqlConfiguration.ConnectionString));
                // options.UseInMemoryDatabase("DreamTravelDatabase"));

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(sqlConfiguration.ConnectionString));
                // .UseInMemoryStorage());
                
            services.AddGraphQLSchema<DreamTripsDbContext>();

            services.AddTransient<ICityRepository, CityRepository>();
            services.AddTransient<ICityStatisticsRepository, CityStatisticsRepository>();
            services.AddTransient<ICountryStatisticsRepository, CountryStatisticsRepository>();

            return services;
        }
    }
}
