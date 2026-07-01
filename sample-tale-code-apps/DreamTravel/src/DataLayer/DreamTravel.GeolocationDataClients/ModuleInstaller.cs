﻿using DreamTravel.GeolocationDataClients.GeoDb;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.GeolocationDataClients.MichelinApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.HTTP;

namespace DreamTravel.GeolocationDataClients
{
    public static class ModuleInstaller
    {
        /// <summary>
        /// Registers the geolocation-related typed HTTP clients.
        /// <para>
        /// All three integrations expose <c>GET</c>-only endpoints (geocoding,
        /// distance matrix, city lookup), so the <c>SolTechnology.Core.HTTP</c>
        /// 0.7.0 idempotent-only retry default is exactly what we want — no
        /// <c>RetryOnUnsafeVerbs</c> opt-in needed. Per-client timeouts and
        /// retry budgets live in <c>appsettings*.json</c> under
        /// <c>HTTPClients:{name}:Policy</c> (see
        /// <c>docs/HTTP-Production-Checklist.md</c> in SolTechnology.Core).
        /// </para>
        /// <para>
        /// Correlation propagation stays on (the default) because both the API
        /// and Worker hosts use <c>AddSolLogging</c>; the outbound
        /// <c>X-Correlation-Id</c> matches the inbound one written by the
        /// logging middleware.
        /// </para>
        /// </summary>
        public static IServiceCollection InstallGeolocationDataClients(this IServiceCollection services)
        {
            services.AddSolHTTPClient<IGoogleHTTPClient, GoogleHTTPClient, GoogleHTTPOptions>("Google");
            services.Decorate(typeof(IGoogleHTTPClient), typeof(GoogleHTTPClientCachingDecorator));

            services.AddSolHTTPClient<IMichelinHTTPClient, MichelinHTTPClient, MichelinHTTPOptions>("Michelin");
            services.AddSolHTTPClient<IGeoDbHTTPClient, GeoDbHTTPClient>("GeoDb");

            return services;
        }
    }
}


