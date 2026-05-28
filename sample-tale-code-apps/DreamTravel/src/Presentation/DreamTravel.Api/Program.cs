using System.Globalization;
using Asp.Versioning.ApiExplorer;
using DreamTravel.DomainServices;
using DreamTravel.Flows;
using DreamTravel.GraphDatabase;
using DreamTravel.Infrastructure;
using DreamTravel.ServiceDefaults;
using DreamTravel.GeolocationDataClients;
using DreamTravel.Queries;
using DreamTravel.Sql;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Trace;
using SolTechnology.Core.API;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.Authentication;
using SolTechnology.Core.Cache;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Enrichment;
using SolTechnology.Core.Logging.Operations;
using SolTechnology.Core.SQL;

namespace DreamTravel.Api;

public class Program
{
    private static readonly string CorsPolicy = "dupa";

    // This method gets called by the runtime. Use this method to add services to the container.
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Aspire's ServiceDefaults already wires AddAspNetCoreInstrumentation()
        // + AddHttpClientInstrumentation(). Subscribe SolTechnology.Core's operation
        // ActivitySource so CQRS requests show up as child spans alongside the
        // HTTP request and dependency calls in the same trace (App Insights / Jaeger / OTLP).
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(CoreLoggingActivitySources.OperationsName));


        var cultureInfo = new CultureInfo("en-US");
        cultureInfo.NumberFormat.CurrencySymbol = "�";

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


        //CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy,
                policyBuilder =>
                {
                    policyBuilder.SetIsOriginAllowed(origin =>
                        {
                            if (string.IsNullOrWhiteSpace(origin)) return false;

                            // Allow all localhost origins (for development)
                            if (origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) ||
                                origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }

                            // Allow specific production origins
                            var allowedOrigins = new[]
                            {
                                "https://dreamtravels.azurewebsites.net",
                                "https://dreamtravels-demo.azurewebsites.net",
                                "https://avroconvertonline.azurewebsites.net"
                            };

                            return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
                        })
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        //SQL
        var sqlConfiguration = builder.Configuration.GetSection("Sql").Get<SQLConfiguration>()!;
        builder.Services.AddSQL(sqlConfiguration);


        //Trips
        builder.Services.InstallTripsSql(sqlConfiguration);
        builder.Services.InstallGeolocationDataClients();
        builder.Services.InstallInfrastructure();
        builder.Services.InstallDomainServices();
        builder.Services.InstallTripsQueries();


        //Graph
        builder.Services.Configure<Neo4jSettings>(
            builder.Configuration.GetSection("Neo4j"));
        builder.Services.InstallGraphDatabase();

        //Journey (migrated to Story framework) — defaults to in-memory persistence.
        builder.Services.AddFlows();

        //The rest
        builder.Services.AddCache();
        builder.Services.AddCoreLogging();
        builder.Services.LogDetail(
            "name",
            asName: "CityName",
            source: LogDetailSource.Body,
            endpoints: ["/api/v1/FindLocationOfCity", "/api/FindCityByName"]);


        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddCQRS(assemblies: thisAssembly);

        var authenticationConfiguration = builder.Configuration.GetRequiredSection("Authentication").Get<AuthenticationConfiguration>()!;
        var authFilter = builder.Services.AddAuthenticationAndBuildFilter(authenticationConfiguration);

        // SolTechnology.Core.Api one-liner — wires:
        //   - Header-based API versioning (X-API-VERSION) + per-version Swagger docs
        //   - ExceptionFilter (mapped exceptions → RFC 7807 ProblemDetails; unmapped → LogCritical+rethrow)
        //   - ResultConversionFilter (Result<T> → unwrapped DTO / ProblemDetails by Error subtype)
        //   - IExceptionStatusCodeMapper (default mapping; replaceable)
        //   - Microsoft AddProblemDetails() for non-MVC paths
        //   - Core.Logging's ICorrelationIdService (used as ProblemDetails.Extensions["correlationId"])
        builder.Services.AddApiCore(
            o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment(),
            apiTitle: "DreamTravel API",
            defaultMajorVersion: 2);

        //SWAGGER — security definitions (project-specific)
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiKeyAuthenticationSchemeOptions.AuthenticationHeaderName,
                Description = "Authentication: Api Key for using Dream Travel"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = ApiKeyAuthenticationSchemeOptions.AuthenticationScheme
                        }
                    },
                    []
                }
            });
        });

        builder.Services.AddFluentValidationRulesToSwagger();

        builder.Services.AddControllers(opts =>
        {
            opts.Filters.Add(authFilter);
            opts.AddApiCoreFilters();
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        app.UseDeveloperExceptionPage();

        // SolTechnology.Core.Api: per-version Swagger UI (newest first, deprecation badges).
        app.UseSwaggerWithVersioning("DreamTravel API");

        app.UseCors(CorsPolicy);
        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();
        app.UseAuthentication();
        app.UseCoreLogging();

        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });

        app.MapControllers();
        LogAvailableEndpoints(app.Services);

        app.Run();
    }

    private static void LogAvailableEndpoints(IServiceProvider services)
    {
        var endpointDataSource = services.GetRequiredService<EndpointDataSource>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");

        var availableEndpoints = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new {
                Methods = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? ["UNKNOWN"],
                Route = e.RoutePattern.RawText
            })
            .SelectMany(e => e.Methods.Select(m => $"{m} {e.Route}"))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        logger.LogInformation("AvailableEndpoints: {Endpoints}", availableEndpoints);
    }
}
