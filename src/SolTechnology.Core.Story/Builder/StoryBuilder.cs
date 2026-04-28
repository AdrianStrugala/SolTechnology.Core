using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Story.Builder;

/// <summary>Internal <see cref="IStoryBuilder"/> implementation.</summary>
internal sealed class StoryBuilder : IStoryBuilder
{
    public IServiceCollection Services { get; }
    public StoryOptions Options { get; }

    public StoryBuilder(IServiceCollection services, StoryOptions options)
    {
        Services = services;
        Options = options;
    }
}

