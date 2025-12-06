using DreamTravel.DomainServices.CityDomain;
using DreamTravel.DomainServices.CityDomain.SaveSteps;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DomainServices
{
    /// <summary>
    /// Extension methods for registering DreamTravel domain services in the dependency injection container.
    /// </summary>
    public static class ModuleInstaller
    {
        /// <summary>
        /// Registers all domain services and related dependencies.
        /// This includes city domain services, mappers, and city save steps.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
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