﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Blob.Connection;

namespace SolTechnology.Core.Blob
{
    /// <summary>
    /// Registers the Blob module in the service collection.
    /// </summary>
    public static class ModuleInstaller
    {
        /// <summary>
        /// Registers <see cref="BlobConfiguration"/> as options and <see cref="IBlobConnectionFactory"/> as a singleton.
        /// </summary>
        /// <param name="services">The service collection to add the Blob services to.</param>
        /// <param name="blobConfiguration">The Blob settings.</param>
        /// <exception cref="ArgumentException"><paramref name="blobConfiguration"/> is <see langword="null"/>.</exception>
        /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
        public static IServiceCollection AddSolBlob(this IServiceCollection services, BlobConfiguration blobConfiguration)
        {
            if (blobConfiguration == null)
            {
                throw new ArgumentException($"The [{nameof(BlobConfiguration)}] is missing. Provide it by parameter.");
            }

            services
                .AddOptions<BlobConfiguration>()
                .Configure(config =>
                {
                    config.ConnectionString = blobConfiguration.ConnectionString;
                })
                .ValidateOnStart();

            services.AddSingleton<IBlobConnectionFactory, BlobConnectionFactory>();

            return services;
        }
    }
}
