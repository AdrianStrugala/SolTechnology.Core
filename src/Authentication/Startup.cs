using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Authentication
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
            //  services.AddAuthentication(nameof(DreamAuthentication));


            services.AddAuthentication(nameof(DreamAuthentication))
                .AddScheme<DreamAuthenticationOptions, DreamAuthentication>(nameof(DreamAuthentication), null);

            //            services.AddAuthorization(options =>
            //            {
            //                options.AddPolicy(nameof(DreamAuthentication), policy =>
            //                {
            //                    policy.AuthenticationSchemes.Add(nameof(DreamAuthentication));
            //                    policy.Requirements
            //                });
            //            });
            //
            //            services.AddAuthentication(options =>
            //            {
            //                options.DefaultAuthenticateScheme = nameof(DreamAuthentication).ToString();
            //                options.DefaultScheme = nameof(DreamAuthentication).ToString();
            //            });


            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
