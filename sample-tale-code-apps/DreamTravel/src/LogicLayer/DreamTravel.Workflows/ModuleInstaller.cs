using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Story;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
    public static IServiceCollection AddSolStories(this IServiceCollection services, StoryOptions? options = null)
    {
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
