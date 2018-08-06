using System.Globalization;
using DreamTravel.ExternalConnection;
using DreamTravel.TSPControllerHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddMvc();

            services.AddDistributedMemoryCache();


            services.AddTransient<ICallAPI, CallAPI>();
            services.AddTransient<IProcessInputData, ProcessInputData>();
            services.AddTransient<IFormOutputDataForBestPath, FormOutputDataForBestPath>();
            services.AddTransient<ICalculateBestPath, CalculateBestPath>();
            services.AddTransient<IBreakCostLimit, BreakCostLimit>();
            services.AddTransient<IEvaluationBrain, EvaluationBrain>();
            services.AddTransient<IDownloadLocationOfCity, DownloadLocationOfCity>();

            //TSP engine
            services.AddTransient<ITSP, AntColony>();
            

            services.AddSession();


            //AUTHENTICATION TURNED OFF

            //            var configurationRoot = new ConfigurationBuilder()
            //                .AddJsonFile("appsettings.json")
            //                .AddEnvironmentVariables("APPSETTING_")
            //                .Build();
            //
            //            services.AddAuthentication(DreamAuthenticationOptions.AuthenticationScheme)
            //                .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(
            //                    DreamAuthenticationOptions.AuthenticationScheme,
            //                    null);

            //            services.Configure<DreamAuthenticationOptions>(DreamAuthenticationOptions.AuthenticationScheme, configurationRoot);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
