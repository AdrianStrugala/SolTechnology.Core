using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Story.Builder;

/// <summary>
/// Builder returned by <c>RegisterStories</c>. Persistence providers plug in through
/// extension methods on this type (<c>UseInMemoryStoryRepository</c>,
/// <c>UseStoryRepository&lt;T&gt;</c>). A repository is
/// always present — the default after <c>RegisterStories()</c> is in-memory. The builder
/// is a thin handle — its only state is the underlying <see cref="IServiceCollection"/>
/// and the <see cref="StoryOptions"/> already registered there.
/// </summary>
public interface IStoryBuilder
{
    /// <summary>The service collection being configured.</summary>
    IServiceCollection Services { get; }

    /// <summary>The singleton <see cref="StoryOptions"/> registered by <c>RegisterStories</c>.</summary>
    StoryOptions Options { get; }
}

