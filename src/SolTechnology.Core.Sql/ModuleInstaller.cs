using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql.Connection;

namespace SolTechnology.Core.Sql
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSql(this IServiceCollection services, SqlConfiguration? sqlConfiguration = null)
        {

            services
                .AddOptions<SqlConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
           {

               if (sqlConfiguration == null)
               {
                   sqlConfiguration = configuration.GetSection("Configuration:Sql").Get<SqlConfiguration>();
               }

               if (sqlConfiguration == null)
               {
                   throw new ArgumentException($"The [{nameof(SqlConfiguration)}] is missing. Provide it by parameter or configuration section");
               }

               config.ConnectionString = sqlConfiguration.ConnectionString;
           });


            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

            return services;
        }
    }
}
