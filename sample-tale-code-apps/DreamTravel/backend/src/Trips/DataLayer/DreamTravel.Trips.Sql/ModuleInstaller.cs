using DreamTravel.Trips.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;

namespace DreamTravel.Trips.Sql
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallSql(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSql();

            var sqlConfiguration =
                configuration.GetSection("Configuration:Sql").Get<SqlConfiguration>();
            services.AddDbContext<DreamTripsDbContext>(options =>
                options.UseSqlServer(sqlConfiguration.ConnectionString));
            
            services.AddScoped<ICityRepository, CityRepository>();

            return services;
        }
    }
}
