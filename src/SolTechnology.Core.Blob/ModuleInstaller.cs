﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Blob.Connection;

namespace SolTechnology.Core.Blob
{
    public static class ModuleInstaller
    {
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
