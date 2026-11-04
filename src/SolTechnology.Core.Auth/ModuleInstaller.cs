﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Auth
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSolAuth(
            this IServiceCollection services,
            AuthConfiguration authConfiguration)
        {
            if (string.IsNullOrEmpty(authConfiguration?.ApiKey))
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
