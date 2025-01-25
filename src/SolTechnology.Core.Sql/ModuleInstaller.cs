using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql.Connections;
using SolTechnology.Core.Sql.Transactions;

namespace SolTechnology.Core.Sql
{
    public static class ModuleInstaller
    {
        /// <summary>
        /// Configures and registers SQL services using the provided <see cref="SqlConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item><description>Configures an options instance of <see cref="SqlConfiguration"/> with the specified connection string.</description></item>
        /// <item><description>Registers <see cref="ISqlConnectionFactory"/> for creating SQL connections.</description></item>
        /// <item><description>Registers <see cref="IUnitOfWork"/> for managing transactions.</description></item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// The updated <see cref="IServiceCollection"/> with the SQL services registered.
        /// </returns>
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
