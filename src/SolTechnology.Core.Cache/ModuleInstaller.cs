﻿﻿﻿﻿﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache;

public static class ModuleInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSolLocalCache(CacheConfiguration? cacheConfiguration = null)
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
                .Validate(config => config.ExpirationSeconds > 0,
                    $"{nameof(CacheConfiguration)}.{nameof(CacheConfiguration.ExpirationSeconds)} must be greater than 0.")
                .ValidateOnStart();

            services.AddMemoryCache();
            services.AddSingleton<ISingletonCache, SingletonCache>();
            services.AddScoped(typeof(IScopedCache<,>), typeof(ScopedCache<,>));

            return services;
        }

        public IServiceCollection AddSolDistributedCache(DistributedCacheConfiguration configuration)
        {
            services
                .AddOptions<DistributedCacheConfiguration>()
                .Configure(config =>
                {
                    config.ConnectionString = configuration.ConnectionString;
                    config.InstanceName = configuration.InstanceName;
                    config.ExpirationSeconds = configuration.ExpirationSeconds;
                })
                .Validate(config => !string.IsNullOrWhiteSpace(config.ConnectionString),
                    $"{nameof(DistributedCacheConfiguration)}.{nameof(DistributedCacheConfiguration.ConnectionString)} must not be empty.")
                .Validate(config => !string.IsNullOrWhiteSpace(config.InstanceName),
                    $"{nameof(DistributedCacheConfiguration)}.{nameof(DistributedCacheConfiguration.InstanceName)} must not be empty; \"SolTechnology:\" keeps the 1.x lock and idempotency keys.")
                .Validate(config => config.ExpirationSeconds > 0,
                    $"{nameof(DistributedCacheConfiguration)}.{nameof(DistributedCacheConfiguration.ExpirationSeconds)} must be greater than 0.")
                .ValidateOnStart();

            services.AddStackExchangeRedisCache(options => options.InstanceName = configuration.InstanceName);
            services
                .AddOptions<RedisCacheOptions>()
                .Configure<IServiceProvider>((options, provider) => options.ConnectionMultiplexerFactory =
                    () => Task.FromResult(provider.GetRequiredService<IConnectionMultiplexer>()));

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(configuration.ConnectionString));

            services.AddSingleton<IRedisCache, RedisCache>();

            return services;
        }

        /// <summary>
        /// Registers an in-process <see cref="IDistributedLockService"/> that honours the lock expiry
        /// through the registered <see cref="TimeProvider"/> (or <see cref="TimeProvider.System"/>).
        /// No Redis required — suitable for local dev and single-instance deployments.
        /// </summary>
        public IServiceCollection AddSolLocalLock()
        {
            services.AddSingleton<IDistributedLockService>(sp =>
                new LocalDistributedLockService(
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalDistributedLockService>>(),
                    sp.GetService<TimeProvider>() ?? TimeProvider.System));
            return services;
        }

        /// <summary>
        /// Registers a Redis-backed <see cref="IDistributedLockService"/> using <c>SET NX EX</c>.
        /// Requires <see cref="AddSolDistributedCache"/> to have been called first (reuses the same
        /// <see cref="IConnectionMultiplexer"/>).
        /// </summary>
        public IServiceCollection AddSolDistributedLock()
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
        public IServiceCollection AddSolLocalIdempotency(TimeSpan? ttl = null)
        {
            var expiry = ttl ?? TimeSpan.FromHours(24);
            services.AddSingleton<IIdempotencyStore>(sp =>
                new LocalIdempotencyStore(
                    expiry,
                    sp.GetService<TimeProvider>() ?? TimeProvider.System));
            return services;
        }

        /// <summary>
        /// Registers a Redis-backed <see cref="IIdempotencyStore"/> using <c>SET NX EX</c> for
        /// atomic key reservation. Multi-instance safe. Requires <see cref="AddSolDistributedCache"/>
        /// to have been called first (reuses the same <see cref="IConnectionMultiplexer"/>).
        /// </summary>
        /// <param name="ttl">How long a stored response is kept in Redis (default 24 h).</param>
        public IServiceCollection AddSolDistributedIdempotency(TimeSpan? ttl = null)
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
