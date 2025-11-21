using DreamTravel.DomainServices.CityDomain;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DomainServices
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDomainServices(this IServiceCollection services)
        {
            services.AddScoped<ICityDomainService, CityDomainService>();

            return services;
        }
    }
}