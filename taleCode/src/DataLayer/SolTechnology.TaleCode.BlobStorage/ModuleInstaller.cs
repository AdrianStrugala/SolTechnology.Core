using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.BlobStorage;
using SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository;

namespace SolTechnology.TaleCode.BlobData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallBlobStorage(this IServiceCollection services)
        {
            services.AddBlobStorage();

            services.AddScoped<IPlayerStatisticsRepository, PlayerStatisticsRepository.PlayerStatisticsRepository>();

            return services;
        }
    }
}
