using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.BlobData;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddQueries(this IServiceCollection services)
        {
            services.AddBlobData();

            services.AddScoped<IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult>, GetPlayerStatisticsHandler>();

            return services;
        }
    }
}
