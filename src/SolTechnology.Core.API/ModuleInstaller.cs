using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SolTechnology.Core.API;

public static class ModuleInstaller
{
    /// <summary>
    /// Configures API versioning using header-based versioning (X-API-VERSION)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="defaultMajorVersion">Default major version (default: 2)</param>
    /// <param name="defaultMinorVersion">Default minor version (default: 0)</param>
    /// <param name="apiTitle">API title for Swagger documentation (default: "API")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddVersioning(
        this IServiceCollection services,
        int defaultMajorVersion = 2,
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
