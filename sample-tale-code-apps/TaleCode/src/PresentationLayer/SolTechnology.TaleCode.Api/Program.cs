using System.Text.Encodings.Web;
using System.Text.Unicode;
using Hangfire;
using Microsoft.OpenApi.Models;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.Authentication;
using SolTechnology.Core.Logging.Middleware;
using SolTechnology.Core.Sql;
using SolTechnology.TaleCode.PlayerRegistry.Queries;
using Swashbuckle.AspNetCore.Filters;

namespace SolTechnology.TaleCode.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Add service defaults & Aspire components.
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProblemDetails();

        builder.Services.AddLogging(c =>
                c.AddConsole()
                .AddApplicationInsights());
        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.InstallQueries();


        var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
        builder.Services.AddControllers(opts =>
            {
                opts.Filters.Add(authenticationFiler);
                opts.Filters.Add<ExceptionFilter>();
                opts.Filters.Add<ResponseEnvelopeFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                options.JsonSerializerOptions.WriteIndented = true;
            });


        //SWAGGER
        builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaleCode API", Version = "v1" });
            c.ExampleFilters();
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = SolTechnologyAuthenticationOptions.AuthenticationHeaderName,
                Description = "Authentication: Api Key for TaleCode"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                    },
                    new string[] { }
                }
            });
        });

        //HANGFIRE
        var sqlConnectionString = builder.Configuration.GetSection("Configuration:Sql").Get<SqlConfiguration>().ConnectionString;
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(sqlConnectionString));

        var app = builder.Build();


        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseExceptionHandler("/error");
        app.UseMiddleware<LoggingMiddleware>();


        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.UseAuthentication();

        app.MapControllers();

        app.Run();
    }
}