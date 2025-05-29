using DreamTravel.Flows.SampleOrderWorkflow;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Journey.Workflow.ChainFramework;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
        public static IServiceCollection AddFlows(
            this IServiceCollection services)
        {
            services.AddScoped<IJourneyHandler, SampleOrderWorkflowHandler>();
            services.AddScoped<SampleOrderWorkflowHandler>();

            return services;
        }
        
}