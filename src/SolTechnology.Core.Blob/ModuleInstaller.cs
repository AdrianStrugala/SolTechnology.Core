﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using SolTechnology.Core.Blob.Connection;

namespace SolTechnology.Core.Blob
{
    /// <summary>
    /// Registers the Blob module in the service collection.
    /// </summary>
    public static class ModuleInstaller
    {
        /// <param name="services">The service collection to add the Blob services to.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Registers <see cref="BlobConfiguration"/> as options and <see cref="IBlobConnectionFactory"/> as a singleton.
            /// </summary>
            /// <param name="blobConfiguration">The Blob settings; its connection string must be non-empty and parsable as
            /// an Azure Storage connection string, which is validated when the host starts.</param>
            /// <exception cref="ArgumentException"><paramref name="blobConfiguration"/> is <see langword="null"/>.</exception>
            /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
            public IServiceCollection AddSolBlob(BlobConfiguration blobConfiguration)
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
                    .Validate(c => !string.IsNullOrWhiteSpace(c.ConnectionString), "Blob ConnectionString is required")
                    .Validate(
                        c => string.IsNullOrWhiteSpace(c.ConnectionString) || IsParsable(c.ConnectionString),
                        "Blob ConnectionString is not a valid Azure Storage connection string")
                    .ValidateOnStart();

                services.AddSingleton<IBlobConnectionFactory, BlobConnectionFactory>();

                return services;
            }
        }

        private static bool IsParsable(string connectionString)
        {
            try
            {
                _ = new BlobServiceClient(connectionString);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
