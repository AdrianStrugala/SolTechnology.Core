using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Flow.Workflow.ChainFramework;
using SolTechnology.Core.Flow.Workflow.Persistence;
using SolTechnology.Core.Flow.Workflow.Persistence.InMemory;
using SolTechnology.Core.Flow.Workflow.Persistence.Sqlite;

namespace SolTechnology.Core.Flow.Workflow;

public static class ModuleInstaller
{
    public static IServiceCollection AddFlowFramework(
        this IServiceCollection services)
    {
        services.AddScoped<IFlowInstanceRepository, SqliteFlowInstanceRepository>();
        services.AddScoped<FlowManager>();

        return services;
    }

}