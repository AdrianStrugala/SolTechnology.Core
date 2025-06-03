using DreamTravel.Flows.SampleOrderWorkflow;
using DreamTravel.Flows.SampleOrderWorkflow.Steps;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Flow.Workflow.ChainFramework;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
        public static IServiceCollection AddFlows(
            this IServiceCollection services)
        {
            services.AddScoped<IFlowHandler, SampleOrderWorkflowHandler>();
            services.AddScoped<SampleOrderWorkflowHandler>();
            
            services.AddScoped<RequestUserInputStep>();
            services.AddScoped<FetchExternalDataStep>();
            services.AddScoped<BackendProcessingStep>();

            return services;
        }
        
}