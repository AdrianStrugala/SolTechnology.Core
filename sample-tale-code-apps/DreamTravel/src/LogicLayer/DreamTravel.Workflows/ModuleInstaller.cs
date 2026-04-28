using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Builder;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
    /// <summary>
    /// Registers DreamTravel stories. Returns the <see cref="IStoryBuilder"/> so the caller
    /// can pick a persistence provider (e.g. <c>.UseSqliteStoryRepository(...)</c>).
    /// Defaults to in-memory persistence — sufficient for dev/tests.
    /// </summary>
    public static IStoryBuilder AddFlows(
        this IServiceCollection services,
        Action<StoryOptions>? configure = null)
        => services.RegisterStories(configure);
}

