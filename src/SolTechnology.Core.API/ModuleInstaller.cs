using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SolTechnology.Core.API;

/// <summary>
/// Service and pipeline registrations of <c>SolTechnology.Core.Api</c>: the ProblemDetails error
/// pipeline, header-based API versioning and per-version Swagger.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// One-call setup of the API error pipeline (<see cref="AddSolApiExceptionHandling"/>) and header-based
    /// versioning with per-version Swagger (<see cref="AddSolVersioning"/>). Then add the filters with
    /// <see cref="ApiCoreMvcOptionsExtensions.AddSolApiCoreFilters(Microsoft.AspNetCore.Mvc.MvcOptions)"/> and call
    /// <see cref="UseSolSwaggerWithVersioning"/> in the request pipeline.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Optional configuration delegate for <see cref="ApiExceptionOptions"/>.</param>
    /// <param name="apiTitle">API title for the generated Swagger documents (default: <c>"API"</c>).</param>
    /// <param name="defaultMajorVersion">Default major API version (default: <c>1</c>).</param>
    /// <param name="defaultMinorVersion">Default minor API version (default: <c>0</c>).</param>
    public static IServiceCollection AddSolApiCore(
        this IServiceCollection services,
        Action<ApiExceptionOptions>? configure = null,
        string apiTitle = "API",
        int defaultMajorVersion = 1,
        int defaultMinorVersion = 0)
    {
        services.AddSolApiExceptionHandling(configure);
        services.AddSolVersioning(defaultMajorVersion, defaultMinorVersion, apiTitle);
        return services;
    }

    /// <summary>
    /// Registers the API error pipeline: <see cref="ExceptionFilter"/>, <see cref="ResultConversionFilter"/>, a
    /// replaceable <see cref="IExceptionStatusCodeMapper"/> (<c>TryAddSingleton</c>), <see cref="ApiExceptionOptions"/>,
    /// ASP.NET Core's <c>AddProblemDetails()</c> with the correlation id, and <c>SolTechnology.Core.Logging</c>.
    /// Add the filters to MVC with <see cref="ApiCoreMvcOptionsExtensions.AddSolApiCoreFilters(Microsoft.AspNetCore.Mvc.MvcOptions)"/>.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Optional configuration delegate for <see cref="ApiExceptionOptions"/>.</param>
    public static IServiceCollection AddSolApiExceptionHandling(
        this IServiceCollection services,
        Action<ApiExceptionOptions>? configure = null)
    {
        // Idempotent: AddSolLogging and AddProblemDetails are both self-guarded.
        services.AddSolLogging();

        services.AddProblemDetails(opts => opts.CustomizeProblemDetails = ProblemDetailsCorrelation.Stamp);
        services.PostConfigure<ApiBehaviorOptions>(opts =>
            opts.InvalidModelStateResponseFactory = ProblemDetailsCorrelation.StampInvalidModelState(opts.InvalidModelStateResponseFactory));

        // TryAdd: a consumer's custom IExceptionStatusCodeMapper wins. Mapper is stateless → singleton.
        services.TryAddSingleton<IExceptionStatusCodeMapper, DefaultExceptionStatusCodeMapper>();

        var optionsBuilder = services.AddOptions<ApiExceptionOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.AddScoped<ExceptionFilter>();
        services.AddScoped<ResultConversionFilter>();
        return services;
    }

    /// <summary>
    /// Configures header-based API versioning (<c>X-API-VERSION</c>) and per-version Swagger
    /// document generation. Pair with <see cref="UseSolSwaggerWithVersioning"/>.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="defaultMajorVersion">Default major version (default: 1)</param>
    /// <param name="defaultMinorVersion">Default minor version (default: 0)</param>
    /// <param name="apiTitle">API title for Swagger documentation (default: "API")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSolVersioning(
        this IServiceCollection services,
        int defaultMajorVersion = 1,
        int defaultMinorVersion = 0,
        string apiTitle = "API")
    {
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

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>>(sp =>
            new ConfigureSwaggerOptions(
                sp.GetRequiredService<IApiVersionDescriptionProvider>(),
                apiTitle));

        return services;
    }

    /// <summary>
    /// Wires <c>UseSwagger</c> and <c>UseSwaggerUI</c> with one Swagger endpoint per registered API version,
    /// newest first. Pair with <see cref="AddSolVersioning"/> or <see cref="AddSolApiCore"/>.
    /// </summary>
    /// <param name="app">Application pipeline.</param>
    /// <param name="apiTitle">Title shown in the Swagger UI version dropdown (default: <c>"API"</c>).</param>
    /// <param name="configureUi">Optional callback to further customize <see cref="SwaggerUIOptions"/>
    /// (e.g. <c>RoutePrefix</c>, <c>OAuth*</c> settings, document expansion).</param>
    public static IApplicationBuilder UseSolSwaggerWithVersioning(
        this IApplicationBuilder app,
        string apiTitle = "API",
        Action<SwaggerUIOptions>? configureUi = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Newest version first in the dropdown — typical UX expectation.
            foreach (var description in provider.ApiVersionDescriptions.Reverse())
            {
                var label = $"{apiTitle} {description.GroupName.ToUpperInvariant()}" +
                            (description.IsDeprecated ? " (Deprecated)" : string.Empty);

                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", label);
            }

            configureUi?.Invoke(options);
        });

        return app;
    }
}
