using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Persistence;
using SolTechnology.Core.Journey.Workflow.Persistence.InMemory;

namespace SolTechnology.Core.Journey.Workflow;

public static class ModuleInstaller
{
        public static IServiceCollection AddJourneyFramework(
            this IServiceCollection services)
        {
            services.AddSingleton<IJourneyInstanceRepository, InMemoryJourneyInstanceRepository>();
            services.AddScoped<JourneyManager>();

            return services;
        }
        
}