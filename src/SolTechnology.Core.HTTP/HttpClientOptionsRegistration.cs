using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.HTTP;

internal static class HttpClientOptionsRegistration
{
    public static void AddClientConfiguration(IServiceCollection services, string httpClientName, HTTPClientConfiguration? fromParameter)
    {
        services
            .AddOptions<HTTPClientConfiguration>(httpClientName)
            .Configure<IConfiguration>((opts, configuration) =>
            {
                var effective = fromParameter
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
    }

    // Precedence: explicit parameter > "HTTPClients:{name}:Policy" > "HttpPolicy" (global) > defaults.
    public static void AddPolicyConfiguration(IServiceCollection services, string httpClientName, HttpPolicyConfiguration? fromParameter)
    {
        services
            .AddOptions<HttpPolicyConfiguration>(httpClientName)
            .Configure<IConfiguration>((opts, configuration) =>
            {
                IConfiguration source = fromParameter is not null
                    ? PolicyConfigurationProjector.ToConfiguration(fromParameter)
                    : configuration.GetSection($"{HTTPClientConfiguration.SectionName}:{httpClientName}:{HTTPClientConfiguration.PolicySubSection}") is { } perClient && perClient.Exists()
                        ? perClient
                        : configuration.GetSection(HttpPolicyConfiguration.SectionName);

                source.Bind(opts);

                // Delegates cannot be bound from configuration, so copy them from the parameter.
                if (fromParameter?.RetryPredicate is not null)
                {
                    opts.RetryPredicate = fromParameter.RetryPredicate;
                }
            })
            .ValidateDataAnnotations()
            .Validate(cfg => cfg.OverallRequestBudget is null || cfg.OverallRequestBudget > cfg.RequestTimeout,
                $"[{nameof(HttpPolicyConfiguration.OverallRequestBudget)}] must be greater than [{nameof(HttpPolicyConfiguration.RequestTimeout)}] to allow at least one full attempt.")
            .ValidateOnStart();
    }

    public static void AddCustomOptions<TOptions>(IServiceCollection services, string httpClientName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration = configuration.GetSection(
                    $"{HTTPClientConfiguration.SectionName}:{httpClientName}:{HTTPClientConfiguration.OptionsSubSection}");
                configuration.Bind(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
