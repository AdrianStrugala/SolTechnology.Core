using System;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
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
            services.AddTransient<IProcessOutputData, ProcessOutputData>();
            services.AddTransient<IBestPathCalculator, BestPathCalculator>();
            services.AddTransient<ITSP, Itsp>();




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
