using System;
using DreamTravel.Api.Configuration;
using DreamTravel.DreamFlights;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamTrips;
using DreamTravel.Identity;
using DreamTravel.Infrastructure.Authentication;
using DreamTravel.Infrastructure.Database;
using Hangfire;
using Hangfire.SqlServer;
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
            services.AddSingleton<IDreamFlightsConfiguration>(appConfig.DreamFlightsConfiguration);


            services.InstallDreamFlights();
            services.InstallDreamTrips();
            services.InstallIdentity();

            services.AddControllers();

            //AUTHENTICATION
            services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
                    .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
                        DreamAuthenticationOptions.AuthenticationScheme,
                        null);


            //HANGFIRE
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(appConfig.SqlDatabaseConfiguration.ConnectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            services.AddHangfireServer();

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
            var policy = new AuthorizationPolicyBuilder()
                         .RequireAuthenticatedUser()
                         .Build();
            services.AddMvc(opts =>
            {
                opts.Filters.Add(new AuthorizeFilter(policy));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
            }

            AddHangfire(app, backgroundJobs);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHangfireDashboard();
            });
        }


        private static void AddHangfire(IApplicationBuilder app, IBackgroundJobClient backgroundJobs)
        {
            app.UseHangfireDashboard(); //https://localhost:44330/hangfire
            backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            RecurringJob.AddOrUpdate<ISendDreamTravelFlightEmail>(a => a.Handle(), "0 0 8 * * *");
        }
    }
}
