using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddSingleton(Configuration.Get<DbConnectionFactory>());

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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
