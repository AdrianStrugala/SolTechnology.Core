using DreamTravel.Identity.Commands;
using DreamTravel.Infrastructure.Authentication;
using DreamTravel.Trips.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Globalization;
using DreamTravel.Infrastructure.Events;
using DreamTravel.Trips.Sql;
using Microsoft.AspNetCore.Mvc.Authorization;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using SolTechnology.Core.Api.Filters;
using Microsoft.AspNetCore.Http;
using SolTechnology.Core.Logging.Middleware;

namespace DreamTravel.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        readonly string CorsPolicy = "dupa";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var cultureInfo = new CultureInfo("en-US");
            cultureInfo.NumberFormat.CurrencySymbol = "�";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


            //CORS
            var policy = new AuthorizationPolicyBuilder()
                         .RequireAuthenticatedUser()
                         .Build();

            services.AddCors(options =>
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

            services.InstallDreamTripsQueries();
            services.InstallIdentityCommands();
            services.InstallSql(configuration);

            services.AddControllers();

            var thisAssembly = typeof(Startup).Assembly;
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(thisAssembly);
            });

            //AUTHENTICATION
            services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
                    .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
                        DreamAuthenticationOptions.AuthenticationScheme,
                        null);


            //SWAGGER
            services.AddSwaggerGen(c =>
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
            services.AddFluentValidationRulesToSwagger();


            //MVC
            services.AddMvc(opts =>
            {
                opts.Filters.Add(new AuthorizeFilter(policy));
                opts.Filters.Add<ExceptionFilter>();
                opts.Filters.Add<ResponseEnvelopeFilter>();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
