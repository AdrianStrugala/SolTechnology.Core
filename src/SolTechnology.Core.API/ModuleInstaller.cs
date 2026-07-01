using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Correlations;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SolTechnology.Core.API;

public static class ModuleInstaller
{
    /// <summary>
    /// One-call setup for everything <c>SolTechnology.Core.Api</c> registers in DI:
    /// exception handling pipeline (ProblemDetails-based), API versioning, and the
    /// dependencies they pull in (<c>Core.Logging</c>, <c>AddProblemDetails</c>).
    /// <para>
    /// After calling this, register the MVC filters globally with
    /// <see cref="ApiCoreMvcOptionsExtensions.AddApiCoreFilters(Microsoft.AspNetCore.Mvc.MvcOptions)"/>
    /// and use <see cref="UseSwaggerWithVersioning"/> in the request pipeline.
    /// </para>
    /// <code>
    /// builder.Services
    ///     .AddApiCore(o =&gt; o.IncludeExceptionDetails = builder.Environment.IsDevelopment(),
    ///                 apiTitle: "DreamTravel API");
    ///
    /// builder.Services.AddControllers(opts =&gt; opts.AddApiCoreFilters());
    ///
    /// var app = builder.Build();
    /// app.UseSwaggerWithVersioning("DreamTravel API");
    /// </code>
    /// <para>
    /// Use the lower-level <see cref="AddApiExceptionHandling"/> / <see cref="AddVersioning"/>
    /// extensions when you need finer-grained control (e.g. registering only one of the two,
    /// or composing your own bootstrap).
    /// </para>
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Optional configuration delegate for <see cref="ApiExceptionOptions"/>.</param>
    /// <param name="apiTitle">API title for the generated Swagger documents (default: <c>"API"</c>).</param>
    /// <param name="defaultMajorVersion">Default major API version (default: <c>1</c>).</param>
    /// <param name="defaultMinorVersion">Default minor API version (default: <c>0</c>).</param>
    public static IServiceCollection AddApiCore(
        this IServiceCollection services,
        Action<ApiExceptionOptions>? configure = null,
        string apiTitle = "API",
        int defaultMajorVersion = 1,
        int defaultMinorVersion = 0)
    {
        services.AddApiExceptionHandling(configure);
        services.AddVersioning(defaultMajorVersion, defaultMinorVersion, apiTitle);
        return services;
    }

    /// <summary>
    /// Registers the API error pipeline:
    /// <list type="bullet">
    ///   <item><see cref="ExceptionFilter"/> — maps known exceptions to RFC 7807
    ///         <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>; rethrows unmapped ones.</item>
    ///   <item><see cref="ResultConversionFilter"/> — converts <c>Result&lt;T&gt;</c> returned
    ///         from controller actions to a raw success body (200) or to <c>ProblemDetails</c>
    ///         derived from the <c>Error</c> subtype.</item>
    ///   <item><see cref="IExceptionStatusCodeMapper"/> — default exception → status mapping;
    ///         registered via <c>TryAddSingleton</c> so consumers can replace it with their
    ///         own implementation (typically extending <see cref="DefaultExceptionStatusCodeMapper"/>).</item>
    ///   <item><see cref="ApiExceptionOptions"/> bound through <see cref="IOptions{TOptions}"/>.</item>
    ///   <item>ASP.NET Core's <c>AddProblemDetails()</c> — produces <c>ProblemDetails</c> for
    ///         status-code pages and any path that does not pass through MVC.</item>
    ///   <item><c>SolTechnology.Core.Logging</c>'s <c>ICorrelationIdService</c> — used to populate
    ///         <c>ProblemDetails.Extensions["correlationId"]</c>.</item>
    /// </list>
    /// <para>
    /// Call once during service configuration, then add the filters to the MVC pipeline:
    /// </para>
    /// <code>
    /// services.AddApiExceptionHandling(o =&gt;
    ///     o.IncludeExceptionDetails = builder.Environment.IsDevelopment());
    /// services.AddControllers(o =&gt; o.AddApiCoreFilters());
    /// </code>
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Optional configuration delegate for <see cref="ApiExceptionOptions"/>.</param>
    public static IServiceCollection AddApiExceptionHandling(
        this IServiceCollection services,
        Action<ApiExceptionOptions>? configure = null)
    {
        // Idempotent: AddSolLogging and AddProblemDetails are both self-guarded.
        services.AddSolLogging();

        // Stamp correlationId on every ProblemDetails the framework produces outside MVC
        // (routing 404, UseStatusCodePages, UseExceptionHandler, auth challenges) so the
        // body shape matches what ExceptionFilter / ResultConversionFilter emit.
        services.AddProblemDetails(opts =>
        {
            opts.CustomizeProblemDetails = ctx =>
            {
                if (ctx.ProblemDetails.Extensions.ContainsKey(ApiProblemDetailsFactory.CorrelationIdKey))
                {
                    return;
                }

                var correlationIdService = ctx.HttpContext.RequestServices
                    .GetService<ICorrelationIdService>();
                if (correlationIdService is null)
                {
                    return;
                }

                ctx.ProblemDetails.Extensions[ApiProblemDetailsFactory.CorrelationIdKey] =
                    correlationIdService.GetOrGenerate().Value;
            };
        });

        // [ApiController]'s 400 ValidationProblemDetails is built before MVC filters run; wrap the
        // default factory to stamp correlationId without disturbing the per-field errors dictionary.
        services.PostConfigure<ApiBehaviorOptions>(opts =>
        {
            var defaultFactory = opts.InvalidModelStateResponseFactory;
            opts.InvalidModelStateResponseFactory = actionContext =>
            {
                var response = defaultFactory(actionContext);

                if (response is ObjectResult { Value: ProblemDetails problem } &&
                    !problem.Extensions.ContainsKey(ApiProblemDetailsFactory.CorrelationIdKey))
                {
                    var correlationIdService = actionContext.HttpContext.RequestServices
                        .GetService<ICorrelationIdService>();
                    if (correlationIdService is not null)
                    {
                        problem.Extensions[ApiProblemDetailsFactory.CorrelationIdKey] =
                            correlationIdService.GetOrGenerate().Value;
                    }
                }

                return response;
            };
        });

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
    /// document generation. Pair with <see cref="UseSwaggerWithVersioning"/>.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="defaultMajorVersion">Default major version (default: 1)</param>
    /// <param name="defaultMinorVersion">Default minor version (default: 0)</param>
    /// <param name="apiTitle">API title for Swagger documentation (default: "API")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddVersioning(
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
    /// Wires <c>UseSwagger</c> + <c>UseSwaggerUI</c> with one Swagger endpoint per registered
    /// API version, ordered newest-first. Combine with <see cref="AddVersioning"/> /
    /// <see cref="AddApiCore"/>, which generated the per-version <c>SwaggerDoc</c>s.
    /// <para>
    /// Replaces the boilerplate loop that every API consumer otherwise repeats:
    /// </para>
    /// <code>
    /// var provider = app.Services.GetRequiredService&lt;IApiVersionDescriptionProvider&gt;();
    /// app.UseSwagger();
    /// app.UseSwaggerUI(c =&gt;
    /// {
    ///     foreach (var d in provider.ApiVersionDescriptions.Reverse())
    ///         c.SwaggerEndpoint($"/swagger/{d.GroupName}/swagger.json", $"{title} {d.GroupName.ToUpperInvariant()}");
    /// });
    /// </code>
    /// </summary>
    /// <param name="app">Application pipeline.</param>
    /// <param name="apiTitle">Title shown in the Swagger UI version dropdown (default: <c>"API"</c>).</param>
    /// <param name="configureUi">Optional callback to further customize <see cref="SwaggerUIOptions"/>
    /// (e.g. <c>RoutePrefix</c>, <c>OAuth*</c> settings, document expansion).</param>
    public static IApplicationBuilder UseSwaggerWithVersioning(
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

/// <summary>
/// Generates one Swagger document per registered API version.
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
