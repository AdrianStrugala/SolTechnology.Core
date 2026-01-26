using System.Globalization;
using Asp.Versioning.ApiExplorer;
using DreamTravel.Api.HealthChecks;
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
using SolTechnology.Core.API;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.Authentication;
using SolTechnology.Core.Cache;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Middleware;
using SolTechnology.Core.SQL;
using SolTechnology.Core.Story;

namespace DreamTravel.Api;

public class Program
{
    private static readonly string CorsPolicy = "dupa";

    // This method gets called by the runtime. Use this method to add services to the container.
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        string[] logIdentifiers = ["trip", "city"];
        builder.UseSerilogDefaults(logIdentifiers);
        builder.AddServiceDefaults();


        var cultureInfo = new CultureInfo("en-US");
        cultureInfo.NumberFormat.CurrencySymbol = "€";

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

        //Health Checks
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<GoogleApiHealthCheck>("google-api", tags: ["ready"]);
        builder.Services.AddHttpClient();

        //Exception Filter (converts exceptions to RFC 7807 ProblemDetails in Result envelope)
        builder.Services.AddScoped<ExceptionFilter>();
        builder.Services.AddScoped<ResponseEnvelopeFilter>();

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

        //Story
        builder.Services.AddFlows(StoryOptions.WithInMemoryPersistence());

        //The rest
        builder.Services.AddCache();

        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(thisAssembly); });

        var authenticationConfiguration = builder.Configuration.GetRequiredSection("Authentication").Get<AuthenticationConfiguration>()!;
        var authFilter = builder.Services.AddAuthenticationAndBuildFilter(authenticationConfiguration);

        // API Versioning
        builder.Services.AddVersioning(apiTitle: "DreamTravel API");

        //SWAGGER
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
            opts.Filters.Add<ExceptionFilter>();
            opts.Filters.Add<ResponseEnvelopeFilter>();
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        app.UseSwagger();

        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwaggerUI(c =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
            {
                c.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"DreamTravel API {description.GroupName.ToUpperInvariant()}" +
                    $"{(description.IsDeprecated ? " (Deprecated)" : "")}");
            }
        });

        app.UseCors(CorsPolicy);
        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();
        app.UseAuthentication();
        app.UseLoggingMiddleware(options =>
        {
            options.Identifiers = logIdentifiers;
        });

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
