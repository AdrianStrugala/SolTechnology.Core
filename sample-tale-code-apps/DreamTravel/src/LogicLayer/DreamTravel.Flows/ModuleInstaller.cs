using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
    public static IServiceCollection AddFlows(this IServiceCollection services, StoryOptions? options = null)
    {
        // If options provided, use them; otherwise use default
        if (options != null)
        {
            services.RegisterStories(options);
        }
        else
        {
            services.RegisterStories();
        }

        return services;
    }
}