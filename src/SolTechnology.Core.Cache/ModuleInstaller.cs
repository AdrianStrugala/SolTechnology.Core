using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Cache
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddCache(this IServiceCollection services, CacheConfiguration cacheConfiguration = null)
        {

            services
                .AddOptions<CacheConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
                {
                    if (cacheConfiguration == null)
                    {
                        cacheConfiguration = configuration.GetSection("Configuration:Cache").Get<CacheConfiguration>();
                    }

                    if (cacheConfiguration == null)
                    {
                        //Apply default values
                        cacheConfiguration = new CacheConfiguration
                        {
                            ExpirationMode = ExpirationMode.Sliding,
                            ExpirationSeconds = 300
                        };
                    }

                    config.ExpirationMode = cacheConfiguration.ExpirationMode;
                    config.ExpirationSeconds = cacheConfiguration.ExpirationSeconds;
                });


            services.AddSingleton<ILazyTaskCache, LazyTaskCache>();

            return services;
        }
    }
}