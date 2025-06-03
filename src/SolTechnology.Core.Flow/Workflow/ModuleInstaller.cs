using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Flow.Workflow.ChainFramework;
using SolTechnology.Core.Flow.Workflow.Persistence;
using SolTechnology.Core.Flow.Workflow.Persistence.InMemory;

namespace SolTechnology.Core.Flow.Workflow;

public static class ModuleInstaller
{
        public static IServiceCollection AddFlowFramework(
            this IServiceCollection services)
        {
            services.AddSingleton<IFlowInstanceRepository, InMemoryFlowInstanceRepository>();
            services.AddScoped<FlowManager>();

            return services;
        }

}