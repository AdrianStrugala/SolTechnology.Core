using System.Globalization;
using DreamTravel.Flows;
using DreamTravel.GraphDatabase;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.GeolocationDataClients;
using DreamTravel.Trips.Queries;
using DreamTravel.Trips.Sql;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.Authentication;
using SolTechnology.Core.Cache;
using SolTechnology.Core.Flow.Workflow;
using SolTechnology.Core.Journey.Workflow;
using SolTechnology.Core.Logging.Middleware;
using SolTechnology.Core.Sql;

namespace DreamTravel.Api;

public class Program
{
    private static readonly string CorsPolicy = "dupa";

    // This method gets called by the runtime. Use this method to add services to the container.
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();


        var cultureInfo = new CultureInfo("en-US");
        cultureInfo.NumberFormat.CurrencySymbol = "�";

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


        //CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy,
                builder =>
                {
                    builder.WithOrigins("http://localhost:4200",
                            "https://dreamtravels.azurewebsites.net",
                            "https://dreamtravels-demo.azurewebsites.net",
                            "http://localhost:55855",
                            "https://localhost:7024",
                            "https://avroconvertonline.azurewebsites.net")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        //SQL
        var sqlConfiguration = builder.Configuration.GetSection("Sql").Get<SqlConfiguration>()!;
        builder.Services.AddSql(sqlConfiguration);


        //Trips
        builder.Services.InstallTripsSql(sqlConfiguration);
        builder.Services.InstallGeolocationDataClients();
        builder.Services.InstallInfrastructure();
        builder.Services.InstallTripsQueries();
        
        
        //Graph
        builder.Services.Configure<Neo4jSettings>(
            builder.Configuration.GetSection("Neo4j"));
        builder.Services.InstallGraphDatabase();
        
        //Journey
        builder.Services.AddFlows();
        builder.Services.AddFlowFramework();
        
        //The rest
        builder.Services.AddCache();

        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(thisAssembly); });

        var authenticationConfiguration = builder.Configuration.GetRequiredSection("Authentication").Get<AuthenticationConfiguration>()!;
        var authFilter = builder.Services.AddAuthenticationAndBuildFilter(authenticationConfiguration);

        builder.Services.AddControllers();

        //SWAGGER
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DreamTravel", Version = "v1" });
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

        builder.Services.AddScoped<ExceptionFilter>();
        builder.Services.AddScoped<ResponseEnvelopeFilter>();

        //MVC
        builder.Services.AddMvc(opts =>
        {
            opts.Filters.Add(authFilter);
            opts.Filters.Add<ExceptionFilter>();
            opts.Filters.Add<ResponseEnvelopeFilter>();
        });


        var app = builder.Build();

        app.MapDefaultEndpoints();

        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dream Travel"));

        app.UseCors(CorsPolicy);
        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();
        app.UseAuthentication();
        app.UseMiddleware<LoggingMiddleware>();

        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });

        app.MapControllers();

        app.Run();
    }
}