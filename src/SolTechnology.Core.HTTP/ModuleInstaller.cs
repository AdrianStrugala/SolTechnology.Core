using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.HTTP.Handlers;
using SolTechnology.Core.HTTP.Telemetry;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.HTTP
{
    public static class ModuleInstaller
    {
        /// <summary>
        /// Registers a typed HTTP client with the resilience pipeline,
        /// correlation propagation, and metrics wired up.
        /// </summary>
        /// <param name="propagateCorrelation">
        /// When <c>true</c> (default), <see cref="CorrelationPropagatingHandler"/>
        /// is inserted before the resilience handler so every retry attempt
        /// carries the same <c>X-Correlation-Id</c> / <c>traceparent</c>. Set
        /// <c>false</c> for hosts that already own correlation propagation
        /// (OpenTelemetry, firm-specific middleware) and want this library to
        /// keep its hands off the headers and the AsyncLocal correlation store.
        /// </param>
        public static IServiceCollection AddHTTPClient<TIClient, TClient>(
            this IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfiguration = null,
            HttpPolicyConfiguration? httpPolicyConfiguration = null,
            bool propagateCorrelation = true)
            where TIClient : class where TClient : class, TIClient
        {
            AddHTTPClientInternal<TIClient, TClient>(services, httpClientName, httpClientConfiguration, httpPolicyConfiguration, propagateCorrelation);
            return services;
        }

        /// <inheritdoc cref="AddHTTPClient{TIClient,TClient}(IServiceCollection,string,HTTPClientConfiguration?,HttpPolicyConfiguration?,bool)"/>
        public static IServiceCollection AddHTTPClient<TIClient, TClient, TOptions>(
            this IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfiguration = null,
            HttpPolicyConfiguration? httpPolicyConfiguration = null,
            bool propagateCorrelation = true)
            where TIClient : class where TClient : class, TIClient where TOptions : class
        {
            AddHTTPClientInternal<TIClient, TClient>(services, httpClientName, httpClientConfiguration, httpPolicyConfiguration, propagateCorrelation);

            services
                .AddOptions<TOptions>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    configuration = configuration.GetSection(
                        $"{HTTPClientConfiguration.SectionName}:{httpClientName}:{HTTPClientConfiguration.OptionsSubSection}");
                    configuration.Bind(options);
                });

            return services;
        }

        private static void AddHTTPClientInternal<TIClient, TClient>(
            IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfigurationParam,
            HttpPolicyConfiguration? httpPolicyConfigurationParam,
            bool propagateCorrelation)
            where TIClient : class where TClient : class, TIClient
        {
            // Shared singletons. HttpClientMetrics owns the Meter so all clients
            // emit on the same well-known meter name (see HttpClientMetrics).
            services.TryAddSingleton<HttpPolicyFactory>();
            services.TryAddSingleton<HttpClientMetrics>();

            if (propagateCorrelation)
            {
                // AddCorrelationIdService is idempotent (TryAddSingleton under the
                // hood) and shared with Core.Logging.AddCoreLogging.
                services.AddCorrelationIdService();
                services.TryAddTransient<CorrelationPropagatingHandler>();
            }

            // ---- Named options: HTTPClientConfiguration ------------------------------
            services
                .AddOptions<HTTPClientConfiguration>(httpClientName)
                .Configure<IConfiguration>((opts, configuration) =>
                {
                    var effective = httpClientConfigurationParam
                        ?? configuration.GetSection($"{HTTPClientConfiguration.SectionName}:{httpClientName}").Get<HTTPClientConfiguration>();

                    if (effective == null)
                    {
                        throw new ArgumentException(
                            $"The [{nameof(HTTPClientConfiguration)}] for client: [{httpClientName}] is missing. Provide it by parameter or configuration section");
                    }

                    opts.BaseAddress = effective.BaseAddress;
                    opts.TimeoutSeconds = effective.TimeoutSeconds;
                    opts.Headers = [.. effective.Headers];
                })
                .Validate(cfg => !string.IsNullOrWhiteSpace(cfg.BaseAddress),
                    $"[{nameof(HTTPClientConfiguration.BaseAddress)}] is required for HTTP client [{httpClientName}].")
                .ValidateOnStart();

            // ---- Named options: HttpPolicyConfiguration ------------------------------
            //
            // Precedence: explicit parameter > "HTTPClients:{name}:Policy" > "HttpPolicy" (global) > defaults.
            // ValidateDataAnnotations + ValidateOnStart enforces [Range] constraints
            // at host startup so misconfiguration cannot pass a health-check
            // and surface only on the first production request.
            services
                .AddOptions<HttpPolicyConfiguration>(httpClientName)
                .Configure<IConfiguration>((opts, configuration) =>
                {
                    IConfiguration source = httpPolicyConfigurationParam is not null
                        ? BuildInMemorySource(httpPolicyConfigurationParam)
                        : configuration.GetSection($"{HTTPClientConfiguration.SectionName}:{httpClientName}:{HTTPClientConfiguration.PolicySubSection}") is { } perClient && perClient.Exists()
                            ? perClient
                            : configuration.GetSection(HttpPolicyConfiguration.SectionName);

                    source.Bind(opts);
                })
                .ValidateDataAnnotations()
                .Validate(cfg => cfg.OverallRequestBudget is null || cfg.OverallRequestBudget > cfg.RequestTimeout,
                    $"[{nameof(HttpPolicyConfiguration.OverallRequestBudget)}] must be greater than [{nameof(HttpPolicyConfiguration.RequestTimeout)}] to allow at least one full attempt.")
                .ValidateOnStart();

            // ---- HttpClient registration --------------------------------------------
            var httpClientBuilder = services
                .AddHttpClient<TIClient, TClient>(httpClientName, (sp, httpClient) =>
                {
                    var cfg = sp.GetRequiredService<IOptionsMonitor<HTTPClientConfiguration>>().Get(httpClientName);
                    var policyCfg = sp.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get(httpClientName);
                    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SolTechnology.Core.HTTP");

                    httpClient.BaseAddress = new Uri(cfg.BaseAddress);

                    // Two timeout systems were a confusing source of incidents:
                    // Polly's per-attempt RequestTimeout and HttpClient.Timeout
                    // could fight, with the latter killing a retry mid-flight.
                    // When the resilience pipeline is active, Polly owns time;
                    // we set HttpClient.Timeout to InfiniteTimeSpan so the only
                    // deadline is the one configured on HttpPolicyConfiguration.
                    if (policyCfg.UsePolly)
                    {
                        httpClient.Timeout = Timeout.InfiniteTimeSpan;
                        if (cfg.TimeoutSeconds.HasValue)
                        {
                            logger.LogWarning(
                                "HTTP client [{ClientName}] has TimeoutSeconds={Seconds}s but UsePolly=true; HttpClient.Timeout is ignored in favour of the Polly per-attempt RequestTimeout.",
                                httpClientName,
                                cfg.TimeoutSeconds.Value);
                        }
                    }
                    else if (cfg.TimeoutSeconds.HasValue)
                    {
                        // Fallback path: caller explicitly disabled Polly, the
                        // only remaining deadline is HttpClient's.
                        httpClient.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds.Value);
                    }

                    foreach (var header in cfg.Headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                    }
                });

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
                    // Operationally significant: an operator who toggles this
                    // flag in production removes retry, breaker, and per-attempt
                    // timeout for the client. Log once per pipeline build (i.e.
                    // once per client) so it shows up in startup logs and is
                    // searchable when an incident asks "where did the timeouts
                    // go?".
                    var logger = context.ServiceProvider
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("SolTechnology.Core.HTTP");
                    logger.LogWarning(
                        "HTTP client [{ClientName}] runs without resilience policies (UsePolly=false). Retry, circuit breaker and per-attempt timeout are disabled.",
                        httpClientName);
                }

                factory.Configure(pipelineBuilder, policyCfg, httpClientName);
            });
        }

        /// <summary>
        /// Projects an in-memory <see cref="HttpPolicyConfiguration"/> into an
        /// <see cref="IConfiguration"/> so the explicit-parameter path goes
        /// through the same ConfigurationBinder.Bind code path as appsettings.
        /// <para>
        /// Property types are restricted to primitives, <see cref="string"/>,
        /// <see cref="bool"/>, <see cref="Enum"/> and <see cref="IFormattable"/>.
        /// Anything else (e.g. a hypothetical <c>List&lt;T&gt;</c> property)
        /// would be silently stringified by <c>object.ToString()</c> and dropped
        /// by the binder — so we fail fast here and force a maintainer to
        /// extend the projector.
        /// </para>
        /// </summary>
        private static IConfiguration BuildInMemorySource(HttpPolicyConfiguration source)
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in typeof(HttpPolicyConfiguration).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }

                var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (underlying != typeof(string)
                    && !underlying.IsPrimitive
                    && !underlying.IsEnum
                    && !typeof(IFormattable).IsAssignableFrom(underlying))
                {
                    throw new InvalidOperationException(
                        $"BuildInMemorySource cannot project property '{prop.Name}' of type {prop.PropertyType}. " +
                        "Extend the projector to handle this type, or expose the policy via configuration section.");
                }

                var value = prop.GetValue(source);
                // Force invariant culture so that, e.g., a German host doesn't
                // serialise 0.3 as "0,3" — ConfigurationBinder parses the
                // produced string with InvariantCulture and would reject it.
                data[prop.Name] = value switch
                {
                    null => null,
                    IFormattable fmt => fmt.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                    _ => value.ToString(),
                };
            }

            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }
}
