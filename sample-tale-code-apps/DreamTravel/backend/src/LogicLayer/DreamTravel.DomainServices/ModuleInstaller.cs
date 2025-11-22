using DreamTravel.DomainServices.CityDomain;
using DreamTravel.DomainServices.CityDomain.SaveSteps;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DomainServices
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDomainServices(this IServiceCollection services)
        {
            services.AddScoped<ICityDomainService, CityDomainService>();
            services.AddScoped<ICityMapper, CityMapper>();
            services.AddScoped<IAssignAlternativeNameStep, AssignAlternativeNameStep>();
            services.AddScoped<IIncrementSearchCountStep, IncrementSearchCountStep>();

            return services;
        }
    }
}