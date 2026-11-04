﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Auth
{
    /// <summary>
    /// Service registrations of <c>SolTechnology.Core.Auth</c>.
    /// </summary>
    public static class ModuleInstaller
    {
        /// <param name="services">DI container.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Registers API-key authentication as the default scheme and a global MVC filter that requires an
            /// authenticated user. Call <c>UseAuthentication()</c> before <c>UseAuthorization()</c> in the pipeline.
            /// </summary>
            /// <param name="authConfiguration">Configuration with the expected API key.</param>
            /// <exception cref="ArgumentException"><paramref name="authConfiguration"/> has no API key, or it is blank.</exception>
            public IServiceCollection AddSolAuth(AuthConfiguration authConfiguration)
            {
                if (string.IsNullOrWhiteSpace(authConfiguration?.ApiKey))
                {
                    throw new ArgumentException($"The [{nameof(AuthConfiguration)}{nameof(authConfiguration.ApiKey)}] is missing. Provide it by parameter.");
                }

                var apiKey = authConfiguration.ApiKey;

                services
                    .AddOptions<AuthConfiguration>()
                    .Configure(config => config.ApiKey = apiKey)
                    .ValidateOnStart();

                services.AddAuthentication(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme)
                    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                        ApiKeyAuthenticationSchemeOptions.AuthenticationScheme, c => c.ApiKey = apiKey);

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // Register the global authorization filter through MvcOptions so consumers no longer thread a returned filter.
                services.Configure<MvcOptions>(options => options.Filters.Add(new AuthorizeFilter(policy)));

                return services;
            }
        }
    }
}
