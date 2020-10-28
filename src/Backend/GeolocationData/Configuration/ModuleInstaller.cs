using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.Query.DownloadRoadData;
using DreamTravel.GeolocationData.Query.DownloadRoadData.Clients;
using DreamTravel.GeolocationData.Repository.Cities;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GeolocationData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallGeolocationData(this IServiceCollection services)
        {
            services.AddTransient<ICityRepository, CityRepository>();

            services.AddTransient<IDownloadRoadData, DownloadRoadData>();
            services.AddTransient<IDownloadCostBetweenTwoCities, DownloadCostBetweenTwoCities>();
            services.AddTransient<IDownloadDurationMatrixByFreeRoad, DownloadDurationMatrixByFreeRoad>();
            services.AddTransient<IDownloadDurationMatrixByTollRoad, DownloadDurationMatrixByTollRoad>();

            return services;
        }
    }
}
