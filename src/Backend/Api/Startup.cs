using DreamTravel.DreamFlights;
using DreamTravel.DreamTrips;
using DreamTravel.Identity;
using DreamTravel.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        readonly string CorsPolicy = "dupa";

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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
                               "https://dreamtravels-demo.azurewebsites.net")
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials();
                });
            });

            services.AddApplicationInsightsTelemetry();

            services.InstallDreamFlights();
            services.InstallDreamTrips();
            services.InstallIdentity();

            services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
                    .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
                        DreamAuthenticationOptions.AuthenticationScheme,
                        null);
            
            services.AddMvc(opts =>
                            {
                                opts.Filters.Add(new AuthorizeFilter(policy));
                            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors(CorsPolicy);

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
