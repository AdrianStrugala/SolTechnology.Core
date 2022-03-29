using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.BlobStorage.Connection;

namespace SolTechnology.Core.BlobStorage
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddBlobStorage(this IServiceCollection services, BlobStorageConfiguration blobStorageConfiguration = null)
        {

            services
                .AddOptions<BlobStorageConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
           {

               if (blobStorageConfiguration == null)
               {
                   blobStorageConfiguration = configuration.GetSection("Configuration:BlobStorage").Get<BlobStorageConfiguration>();
               }

               if (blobStorageConfiguration == null)
               {
                   throw new ArgumentException($"The [{nameof(BlobStorageConfiguration)}] is missing. Provide it by parameter or configuration section");
               }

               config.ConnectionString = blobStorageConfiguration.ConnectionString;
           });


            services.AddSingleton<IBlobConnectionFactory, BlobConnectionFactory>();

            return services;
        }
    }
}
