using System.Data.SqlClient;
using DreamTravel.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Infrastructure
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallInfrastructure(this IServiceCollection services)
        {
            SqlDatabaseConfiguration databaseDataConfiguration = new SqlDatabaseConfiguration();

            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(databaseDataConfiguration.ConnectionString));

            services.AddScoped(sp =>
            {
                var sqlConnection = new SqlConnection(databaseDataConfiguration.ConnectionString);
                sqlConnection.Open();
                return sqlConnection;
            });

            services.AddScoped<DbContext, DreamTravelsDbContext>(sp =>
            {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<DreamTravelsDbContext>();
                var dbConnection = sp.GetService<SqlConnection>();
                dbContextOptionsBuilder.UseSqlServer(dbConnection);

                var ctx = new DreamTravelsDbContext(dbContextOptionsBuilder.Options);

                return (DreamTravelsDbContext)ctx;
            });
            services.AddScoped(s => s.GetService<DbContext>() as DreamTravelsDbContext);

            return services;
        }
    }
}
