using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SolTechnology.Core.API;

/// <summary>
/// Generates one Swagger document per registered API version.
/// </summary>
internal sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, string apiTitle)
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
