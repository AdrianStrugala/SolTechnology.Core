using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.HTTP.Handlers;
using SolTechnology.Core.HTTP.Telemetry;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.HTTP
{
    /// <summary>
    /// Registers typed HTTP clients with the SolTechnology resilience pipeline.
    /// </summary>
    public static class ModuleInstaller
    {
        /// <param name="services">The service collection.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Registers a typed HTTP client with the resilience pipeline,
            /// correlation propagation, metrics, and a Chronicle step per call wired up.
            /// </summary>
            /// <param name="httpClientName">Client name; also the configuration key under <c>HTTPClients</c>.</param>
            /// <param name="httpClientConfiguration">Connection settings; when <c>null</c>, bound from <c>HTTPClients:{name}</c>.</param>
            /// <param name="httpPolicyConfiguration">Resilience policy; when <c>null</c>, bound from <c>HTTPClients:{name}:Policy</c>, then <c>HttpPolicy</c>, then the defaults.</param>
            /// <param name="propagateCorrelation">
            /// When <c>true</c> (default), <see cref="CorrelationPropagatingHandler"/>
            /// is inserted before the resilience handler so every retry attempt
            /// carries the same <c>X-Correlation-Id</c> / <c>traceparent</c>. Set
            /// <c>false</c> for hosts that already own correlation propagation
            /// (OpenTelemetry, firm-specific middleware) and want this library to
            /// keep its hands off the headers and the AsyncLocal correlation store.
            /// </param>
            public IServiceCollection AddSolHTTPClient<TIClient, TClient>(
                string httpClientName,
                HTTPClientConfiguration? httpClientConfiguration = null,
                HttpPolicyConfiguration? httpPolicyConfiguration = null,
                bool propagateCorrelation = true)
                where TIClient : class where TClient : class, TIClient
            {
                AddSolHTTPClientInternal<TIClient, TClient>(services, httpClientName, httpClientConfiguration, httpPolicyConfiguration, propagateCorrelation);
                return services;
            }

            /// <inheritdoc cref="AddSolHTTPClient{TIClient,TClient}(IServiceCollection,string,HTTPClientConfiguration?,HttpPolicyConfiguration?,bool)"/>
            public IServiceCollection AddSolHTTPClient<TIClient, TClient, TOptions>(
                string httpClientName,
                HTTPClientConfiguration? httpClientConfiguration = null,
                HttpPolicyConfiguration? httpPolicyConfiguration = null,
                bool propagateCorrelation = true)
                where TIClient : class where TClient : class, TIClient where TOptions : class
            {
                AddSolHTTPClientInternal<TIClient, TClient>(services, httpClientName, httpClientConfiguration, httpPolicyConfiguration, propagateCorrelation);
                HttpClientOptionsRegistration.AddCustomOptions<TOptions>(services, httpClientName);
                return services;
            }
        }

        private static void AddSolHTTPClientInternal<TIClient, TClient>(
            IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfigurationParam,
            HttpPolicyConfiguration? httpPolicyConfigurationParam,
            bool propagateCorrelation)
            where TIClient : class where TClient : class, TIClient
        {
            // HttpClientMetrics owns the shared Meter so all clients emit on the same name.
            services.TryAddSingleton<HttpPolicyFactory>();
            services.TryAddSingleton<HttpClientMetrics>();
            services.AddSolChronicle();

            if (propagateCorrelation)
            {
                // Idempotent; shared with Core.Logging.AddSolLogging.
                services.AddSolCorrelationIdService();
                services.TryAddTransient<CorrelationPropagatingHandler>();
            }

            HttpClientOptionsRegistration.AddClientConfiguration(services, httpClientName, httpClientConfigurationParam);
            HttpClientOptionsRegistration.AddPolicyConfiguration(services, httpClientName, httpPolicyConfigurationParam);

            var httpClientBuilder = services
                .AddHttpClient<TIClient, TClient>(httpClientName, (sp, httpClient) => ConfigureClient(sp, httpClient, httpClientName));
            ChronicleHTTPHandlerRegistration.AddChronicleCallHandler(httpClientBuilder, httpClientName);

            if (propagateCorrelation)
            {
                httpClientBuilder.AddHttpMessageHandler<CorrelationPropagatingHandler>();
            }

            httpClientBuilder.AddResilienceHandler($"core-http-{httpClientName}", (pipelineBuilder, context) =>
            {
                var factory = context.ServiceProvider.GetRequiredService<HttpPolicyFactory>();
                var policyCfg = context.ServiceProvider
                    .GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>()
                    .Get(httpClientName);

                if (!policyCfg.UsePolly)
                {
                    // Surface once at startup: no retry, breaker, or per-attempt timeout for this client.
                    CreateLogger(context.ServiceProvider).LogWarning(
                        "HTTP client [{ClientName}] runs without resilience policies (UsePolly=false). Retry, circuit breaker and per-attempt timeout are disabled.",
                        httpClientName);
                }

                factory.Configure(pipelineBuilder, policyCfg, httpClientName);
            });
            ChronicleHTTPHandlerRegistration.AddChronicleAttemptHandler(httpClientBuilder);
        }

        private static void ConfigureClient(IServiceProvider sp, HttpClient httpClient, string httpClientName)
        {
            var cfg = sp.GetRequiredService<IOptionsMonitor<HTTPClientConfiguration>>().Get(httpClientName);
            var policyCfg = sp.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get(httpClientName);

            httpClient.BaseAddress = new Uri(cfg.BaseAddress);

            // Polly owns the deadline when active; HttpClient.Timeout would otherwise kill retries mid-flight.
            if (policyCfg.UsePolly)
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                if (cfg.TimeoutSeconds.HasValue)
                {
                    CreateLogger(sp).LogWarning(
                        "HTTP client [{ClientName}] has TimeoutSeconds=[{Seconds}]s but UsePolly=true; HttpClient.Timeout is ignored in favour of the Polly per-attempt RequestTimeout.",
                        httpClientName,
                        cfg.TimeoutSeconds.Value);
                }
            }
            else if (cfg.TimeoutSeconds.HasValue)
            {
                // No Polly: HttpClient.Timeout is the only deadline.
                httpClient.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds.Value);
            }

            foreach (var header in cfg.Headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
            }
        }

        private static ILogger CreateLogger(IServiceProvider sp)
            => sp.GetRequiredService<ILoggerFactory>().CreateLogger("SolTechnology.Core.HTTP");
    }
}
