using System.Globalization;
using DreamTravel.Authentication;
using DreamTravel.ExternalConnection;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.TSPControllerHandlers;
using DreamTravel.TSPControllerHandlers.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelingSalesmanProblem;

namespace DreamTravel
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
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            services.AddMvc(opts =>
            {
                opts.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddDistributedMemoryCache();

            //TSP engine
            services.AddTransient<ITSP, AntColony>();

            services.AddTransient<ICallAPI, CallAPI>();
            services.AddTransient<IProcessInputData, ProcessInputData>();
            services.AddTransient<IFormOutputDataForBestPath, FormOutputDataForBestPath>();
            services.AddTransient<ICalculateBestPath, CalculateBestPath>();
            services.AddTransient<IBreakCostLimit, BreakCostLimit>();
            services.AddTransient<IEvaluationBrain, EvaluationBrain>();
            services.AddTransient<IDownloadLocationOfCity, DownloadLocationOfCity>();
            services.AddTransient<IDownloadCityNameByLocation, DownloadCityNameByLocation>();

            services.AddSession();

            var configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables("APPSETTING_")
                .Build();

            services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
                .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
                    DreamAuthenticationOptions.AuthenticationScheme,
                    null);
            services.Configure<DreamAuthenticationOptions>(DreamAuthenticationOptions.AuthenticationScheme, configurationRoot);
            services.AddSingleton<IAuthenticationHandler, DreamAuthentication>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var cultureInfo = new CultureInfo("en-US");
            cultureInfo.NumberFormat.CurrencySymbol = "€";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();

            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
