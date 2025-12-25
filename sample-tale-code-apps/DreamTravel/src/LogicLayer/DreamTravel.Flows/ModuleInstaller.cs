using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
    public static IServiceCollection AddFlows(this IServiceCollection services)
    {
        services.RegisterStories();

        return services;
    }
}