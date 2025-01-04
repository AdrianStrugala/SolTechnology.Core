using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql.Connections;
using SolTechnology.Core.Sql.Transactions;

namespace SolTechnology.Core.Sql
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSql(this IServiceCollection services, SqlConfiguration sqlConfiguration)
        {
            if (sqlConfiguration == null)
            {
                throw new ArgumentException($"The [{nameof(SqlConfiguration)}] is missing. Provide it by parameter.");
            }

            services
                .AddOptions<SqlConfiguration>()
                .Configure(options =>
                {
                    options.ConnectionString = sqlConfiguration.ConnectionString;
                });

            services.AddTransient<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
