﻿﻿﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(configuration.ConnectionString));

            services.AddSingleton<IRedisCache, RedisCache>();

            return services;
        }

        /// <summary>
        /// Registers an in-process <see cref="IDistributedLockService"/> backed by
        /// <see cref="SemaphoreSlim"/>. No Redis required — suitable for local dev and
        /// single-instance deployments.
        /// </summary>
        public IServiceCollection AddLocalLock()
        {
            services.AddSingleton<IDistributedLockService, LocalDistributedLockService>();
            return services;
        }

        /// <summary>
        /// Registers a Redis-backed <see cref="IDistributedLockService"/> using <c>SET NX EX</c>.
        /// Requires <see cref="AddDistributedCache"/> to have been called first (reuses the same
        /// <see cref="IConnectionMultiplexer"/>).
        /// </summary>
        public IServiceCollection AddDistributedLock()
        {
            services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

            return services;
        }
    }
}
