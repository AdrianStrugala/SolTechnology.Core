using DreamTravel.Identity.Commands;
using DreamTravel.Infrastructure.Authentication;
using DreamTravel.Trips.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using System.Globalization;
using DreamTravel.Trips.Sql;
using Microsoft.AspNetCore.Mvc.Authorization;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.Logging.Middleware;

namespace DreamTravel.Api;

public class Program
{
    public IConfiguration Configuration { get; }
    static readonly string CorsPolicy = "dupa";

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
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy,
                builder =>
                {
                    builder.WithOrigins("http://localhost:4200",
                            "https://dreamtravels.azurewebsites.net",
                            "https://dreamtravels-demo.azurewebsites.net",
                            "http://localhost:55855",
                            "https://avroconvertonline.azurewebsites.net")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        builder.Services.InstallDreamTripsQueries();
        builder.Services.InstallIdentityCommands();
        builder.Services.InstallSql(builder.Configuration);

        builder.Services.AddControllers();

        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(thisAssembly); });

        //AUTHENTICATION
        builder.Services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
            .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
                DreamAuthenticationOptions.AuthenticationScheme,
                null);


        //SWAGGER
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DreamTravel", Version = "v1" });
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = DreamAuthenticationOptions.AuthenticationHeaderName,
                Description = "Authentication: Api Key for using Dream Travel"
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
        builder.Services.AddFluentValidationRulesToSwagger();

        builder.Services.AddScoped<ExceptionFilter>();
        builder.Services.AddScoped<ResponseEnvelopeFilter>();

        //MVC
        builder.Services.AddMvc(opts =>
        {
            opts.Filters.Add(new AuthorizeFilter(policy));
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