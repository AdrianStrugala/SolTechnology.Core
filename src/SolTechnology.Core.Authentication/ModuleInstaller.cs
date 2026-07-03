﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Authentication
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSolAuthentication(
            this IServiceCollection services,
            AuthenticationConfiguration authenticationConfiguration)
        {
            if (string.IsNullOrEmpty(authenticationConfiguration?.ApiKey))
            {
                throw new ArgumentException($"The [{nameof(AuthenticationConfiguration)}{nameof(authenticationConfiguration.ApiKey)}] is missing. Provide it by parameter.");
            }

            var apiKey = authenticationConfiguration.ApiKey;

            services
                .AddOptions<AuthenticationConfiguration>()
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
