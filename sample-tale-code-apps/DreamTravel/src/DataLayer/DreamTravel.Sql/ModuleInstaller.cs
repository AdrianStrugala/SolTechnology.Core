using EntityGraphQL.AspNet;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.SQL;

namespace DreamTravel.Sql
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallTripsSql(this IServiceCollection services, SQLConfiguration sqlConfiguration)
        {
            services.AddSolSQL(sqlConfiguration);
            
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
            return services;
        }
    }
}
