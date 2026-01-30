using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SolTechnology.Core.API.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SolTechnology.Core.API;

public static class ModuleInstaller
{
    /// <param name="services">Service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures API versioning using header-based versioning (X-API-VERSION)
        /// </summary>
        /// <param name="defaultMajorVersion">Default major version (default: 2)</param>
        /// <param name="defaultMinorVersion">Default minor version (default: 0)</param>
        /// <param name="apiTitle">API title for Swagger documentation (default: "API")</param>
        /// <returns>Service collection for chaining</returns>
        public IServiceCollection AddSolVersioning(int defaultMajorVersion = 2,
            int defaultMinorVersion = 0,
            string apiTitle = "API")
        {
            // API Versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(defaultMajorVersion, defaultMinorVersion);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("X-API-VERSION");
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";  // Major version only
                options.SubstituteApiVersionInUrl = true;
            });

            // Swagger configuration for versioning
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>>(sp =>
                new ConfigureSwaggerOptions(
                    sp.GetRequiredService<IApiVersionDescriptionProvider>(),
                    apiTitle));

            return services;
        }

        /// <summary>
        /// Adds a default liveness health check tagged with "live".
        /// </summary>
        public IHealthChecksBuilder AddSolHealthChecks()
        {
            var healthChecksBuilder = services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return healthChecksBuilder;
        }
    }

    /// <summary>
    /// Maps health check endpoints:
    /// - /health - liveness probe (plain text, fast)
    /// - /health/ready - readiness probe with detailed JSON response
    /// </summary>
    public static WebApplication MapSolHealthCheckEndpoints(this WebApplication app)
    {
        // Liveness probe - quick check if app is running
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        // Readiness probe - detailed JSON with all health checks
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });

        return app;
    }
}

/// <summary>
/// Configures Swagger to generate documentation for each API version
/// </summary>
public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, string apiTitle)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = apiTitle,
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "⚠️ This API version is deprecated. Please migrate to newer version."
                    : "Current stable API version"
            });
        }
    }
}
