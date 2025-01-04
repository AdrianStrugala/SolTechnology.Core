using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Cache
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddCache(this IServiceCollection services, CacheConfiguration? cacheConfiguration = null)
        {
            if (cacheConfiguration == null)
            {
                // Default values: absolute expiration, 20 minutes
                cacheConfiguration = new CacheConfiguration
                {
                    ExpirationMode = ExpirationMode.Absolute,
                    ExpirationSeconds = 20 * 60
                };
            }

            services
                .AddOptions<CacheConfiguration>()
                .Configure(config =>
                {
                    config.ExpirationMode = cacheConfiguration.ExpirationMode;
                    config.ExpirationSeconds = cacheConfiguration.ExpirationSeconds;
                });

            services.AddMemoryCache();
            services.AddSingleton<ISingletonCache, SingletonCache>();
            services.AddScoped(typeof(IScopedCache<,>), typeof(ScopedCache<,>));

            return services;
        }
    }
}