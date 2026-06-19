﻿﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Cache;

public static class ModuleInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLocalCache(CacheConfiguration? cacheConfiguration = null)
        {
            cacheConfiguration ??= new CacheConfiguration
            {
                ExpirationMode = ExpirationMode.Absolute,
                ExpirationSeconds = 20 * 60
            };

            services
                .AddOptions<CacheConfiguration>()
                .Configure(config =>
                {
                    config.ExpirationMode = cacheConfiguration.ExpirationMode;
                    config.ExpirationSeconds = cacheConfiguration.ExpirationSeconds;
                })
                .ValidateOnStart();

            services.AddMemoryCache();
            services.AddSingleton<ISingletonCache, SingletonCache>();
            services.AddScoped(typeof(IScopedCache<,>), typeof(ScopedCache<,>));

            return services;
        }

        public IServiceCollection AddDistributedCache(DistributedCacheConfiguration configuration)
        {
            services
                .AddOptions<DistributedCacheConfiguration>()
                .Configure(config =>
                {
                    config.ConnectionString = configuration.ConnectionString;
                    config.InstanceName = configuration.InstanceName;
                    config.ExpirationSeconds = configuration.ExpirationSeconds;
                })
                .ValidateOnStart();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.ConnectionString;
                options.InstanceName = configuration.InstanceName;
            });

            services.AddSingleton<IRedisCache, RedisCache>();

            return services;
        }
    }
}
