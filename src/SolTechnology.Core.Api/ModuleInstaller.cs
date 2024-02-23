using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Api.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace SolTechnology.Core.Api
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiMiddlewares(this IServiceCollection services)
        {
            services.AddSingleton<IActionResultExecutor<ObjectResult>, ResponseEnvelopeResultExecutor>();

            return services;
        }


        public static IApplicationBuilder UseApiMiddlewares(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}