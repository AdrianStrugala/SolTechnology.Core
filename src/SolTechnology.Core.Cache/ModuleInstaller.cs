﻿﻿﻿﻿using Microsoft.Extensions.Caching.Memory;
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

        /// <summary>
        /// Registers an in-process <see cref="IIdempotencyStore"/> backed by
        /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> with TTL.
        /// No Redis required — suitable for local dev and single-instance deployments.
        /// </summary>
        /// <param name="ttl">How long a stored response is kept before eviction (default 24 h).</param>
        public IServiceCollection AddLocalIdempotency(TimeSpan? ttl = null)
        {
            var expiry = ttl ?? TimeSpan.FromHours(24);
            services.AddSingleton<IIdempotencyStore>(new LocalIdempotencyStore(expiry));
            return services;
        }

        /// <summary>
        /// Registers a Redis-backed <see cref="IIdempotencyStore"/> using <c>SET NX EX</c> for
        /// atomic key reservation. Multi-instance safe. Requires <see cref="AddDistributedCache"/>
        /// to have been called first (reuses the same <see cref="IConnectionMultiplexer"/>).
        /// </summary>
        /// <param name="ttl">How long a stored response is kept in Redis (default 24 h).</param>
        public IServiceCollection AddDistributedIdempotency(TimeSpan? ttl = null)
        {
            var expiry = ttl ?? TimeSpan.FromHours(24);
            services.AddSingleton<IIdempotencyStore>(sp =>
                new RedisIdempotencyStore(
                    sp.GetRequiredService<IConnectionMultiplexer>(),
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DistributedCacheConfiguration>>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisIdempotencyStore>>(),
                    expiry));
            return services;
        }
    }
}
