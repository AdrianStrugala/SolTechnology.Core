using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.SQL.Connections;
using SolTechnology.Core.SQL.Transactions;

namespace SolTechnology.Core.SQL
{
    public static class ModuleInstaller
    {
        /// <summary>
        /// Configures and registers SQL services using the provided <see cref="SQLConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item><description>Configures an options instance of <see cref="SQLConfiguration"/> with the specified connection string.</description></item>
        /// <item><description>Registers <see cref="ISQLConnectionFactory"/> for creating SQL connections.</description></item>
        /// <item><description>Registers <see cref="IUnitOfWork"/> for managing transactions.</description></item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// The updated <see cref="IServiceCollection"/> with the SQL services registered.
        /// </returns>
        public static IServiceCollection AddSQL(this IServiceCollection services, SQLConfiguration sqlConfiguration)
        {
            if (sqlConfiguration == null)
            {
                throw new ArgumentException($"The [{nameof(SQLConfiguration)}] is missing. Provide it by parameter.");
            }

            services
                .AddOptions<SQLConfiguration>()
                .Configure(options =>
                {
                    options.ConnectionString = sqlConfiguration.ConnectionString;
                });

            services.AddTransient<ISQLConnectionFactory, SQLConnectionFactory>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
