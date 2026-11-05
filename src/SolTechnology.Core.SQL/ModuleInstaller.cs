using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.SQL.Connections;
using SolTechnology.Core.SQL.Transactions;

namespace SolTechnology.Core.SQL;

/// <summary>
/// Registers the SQL module in the service collection.
/// </summary>
public static class ModuleInstaller
{
    extension(IServiceCollection services)
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
        /// <param name="sqlConfiguration">The SQL settings; its connection string must be non-empty and parsable by
        /// <see cref="SqlConnectionStringBuilder"/>, which is validated when the host starts.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlConfiguration"/> is <see langword="null"/>.</exception>
        /// <returns>
        /// The updated <see cref="IServiceCollection"/> with the SQL services registered.
        /// </returns>
        public IServiceCollection AddSolSQL(SQLConfiguration sqlConfiguration)
        {
            ArgumentNullException.ThrowIfNull(sqlConfiguration);

            services
                .AddOptions<SQLConfiguration>()
                .Configure(options =>
                {
                    options.ConnectionString = sqlConfiguration.ConnectionString;
                })
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                    $"{nameof(SQLConfiguration)}.{nameof(SQLConfiguration.ConnectionString)} must not be empty.")
                .Validate(
                    options => string.IsNullOrWhiteSpace(options.ConnectionString) || IsParsable(options.ConnectionString),
                    $"{nameof(SQLConfiguration)}.{nameof(SQLConfiguration.ConnectionString)} must be a valid SQL Server connection string.")
                .ValidateOnStart();

            services.AddTransient<ISQLConnectionFactory, SQLConnectionFactory>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }

    private static bool IsParsable(string connectionString)
    {
        try
        {
            _ = new SqlConnectionStringBuilder(connectionString);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
