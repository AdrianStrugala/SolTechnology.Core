using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.BlobStorage.Connection;

namespace SolTechnology.Core.BlobStorage
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddBlobStorage(this IServiceCollection services, BlobStorageConfiguration blobStorageConfiguration)
        {
            if (blobStorageConfiguration == null)
            {
                throw new ArgumentException($"The [{nameof(BlobStorageConfiguration)}] is missing. Provide it by parameter.");
            }

            services
                .AddOptions<BlobStorageConfiguration>()
                .Configure(config =>
                {
                    config.ConnectionString = blobStorageConfiguration.ConnectionString;
                });

            services.AddSingleton<IBlobConnectionFactory, BlobConnectionFactory>();

            return services;
        }
    }
}
