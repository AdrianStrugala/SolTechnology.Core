using DreamTravel.Api.Configuration;
using DreamTravel.DreamTrips;
using DreamTravel.Identity;
using DreamTravel.Identity.Commands;
using DreamTravel.Infrastructure.Authentication;
using DreamTravel.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

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

            var environmentName = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
            var appConfig = ConfigurationResolver.GetConfiguration(environmentName);

            services.AddSingleton<ISqlDatabaseConfiguration>(appConfig.SqlDatabaseConfiguration);

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

            services.InstallDreamTrips();
            services.InstallIdentity();

            services.AddControllers();

            //AUTHENTICATION
            services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
                    .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
                        DreamAuthenticationOptions.AuthenticationScheme,
                        null);


            //SWAGGER
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = DreamAuthenticationOptions.AuthenticationHeaderName,
                    Description = "Authentication: Api Key for using Dream Travels"
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
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
            }


            app.UseCors(CorsPolicy);
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
