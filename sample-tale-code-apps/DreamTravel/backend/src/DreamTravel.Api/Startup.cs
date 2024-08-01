using DreamTravel.Identity.Commands;
using DreamTravel.Infrastructure.Authentication;
using DreamTravel.Trips.Commands;
using DreamTravel.Trips.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Authorization;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using SolTechnology.Core.Api.Filters;
using Microsoft.AspNetCore.Http;

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
            cultureInfo.NumberFormat.CurrencySymbol = "€";

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

            services.InstallDreamTripsCommands();
            services.InstallDreamTripsQueries();
            services.InstallIdentityCommands();

            services.AddControllers();

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


            services.AddScoped<ExceptionFilter>();
            services.AddScoped<ResponseEnvelopeFilter>();

            //MVC
            services.AddMvc(opts =>
            {
                opts.Filters.Add(new AuthorizeFilter(policy));
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dream Travel"));
            }

            app.UseCors(CorsPolicy);
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

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
