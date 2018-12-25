using DreamTravel.Authentication;
using DreamTravel.BestPath;
using DreamTravel.BestPath.DataAccess;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.CostLimit;
using DreamTravel.CostLimit.Interfaces;
using DreamTravel.LocationOfCity;
using DreamTravel.LocationOfCity.Interfaces;
using DreamTravel.NameOfCity;
using DreamTravel.NameOfCity.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using TravelingSalesmanProblem;

namespace DreamTravel
{
    using System;
    using BestPath.Executors;

    public class Startup
    {
        public static Action<IServiceCollection> RegisterServices;

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

            services.AddTransient<IDownloadDurationMatrixByTollRoad, DownloadDurationMatrixByTollRoad>();
            services.AddTransient<IDownloadDurationMatrixByFreeRoad, DownloadDurationMatrixByFreeRoad>();
            services.AddTransient<IDownloadCostBetweenTwoCities, DownloadCostBetweenTwoCities>();
            services.AddTransient<IDownloadRoadData, DownloadRoadData>();
            services.AddTransient<IFormOutputData, FormOutputData>();
            services.AddTransient<ICalculateBestPath, CalculateBestPath>();
            services.AddTransient<IBreakCostLimit, BreakCostLimit>();
            services.AddTransient<IEvaluationBrain, EvaluationBrain>();
            services.AddTransient<IFindLocationOfCity, FindLocationOfCity>();
            services.AddTransient<IFindNameOfCity, FindNameOfCity>();

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

            RegisterServices?.Invoke(services);
            RegisterServices = null;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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

            loggerFactory.AddLog4Net();
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
