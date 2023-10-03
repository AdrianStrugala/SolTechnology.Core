using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.TaleCode.BlobData;
using SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallQueries(this IServiceCollection services)
        {
            services.InstallBlobStorage();

            services.AddScoped<IQueryHandler<GetPlayerStatisticsQuery, GetPlayerStatisticsResult>, GetPlayerStatisticsHandler>();

            return services;
        }
    }
}
