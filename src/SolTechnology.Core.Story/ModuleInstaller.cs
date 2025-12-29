using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Story;

/// <summary>
/// Extension methods for registering Story framework components in DI container.
/// </summary>
public static class ModuleInstaller
{
    /// <summary>
    /// Register the Story framework with automatic chapter discovery.
    /// Scans the calling assembly for all classes implementing IChapter and registers them.
    /// Use this in your Startup/Program.cs to enable the Story framework.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">Optional StoryOptions instance. If null, uses StoryOptions.Default</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // Simple usage (no persistence)
    /// services.RegisterStories();
    ///
    /// // With in-memory persistence using factory method
    /// services.RegisterStories(StoryOptions.WithInMemoryPersistence());
    ///
    /// // With SQLite persistence using factory method
    /// services.RegisterStories(StoryOptions.WithSqlitePersistence());
    ///
    /// // Custom configuration
    /// var opts = StoryOptions.Default;
    /// opts.StopOnFirstError = false;
    /// services.RegisterStories(opts);
    /// </code>
    /// </example>
    public static IServiceCollection RegisterStories(
        this IServiceCollection services,
        StoryOptions? options = null)
    {
        options ??= StoryOptions.Default;

        // Register options as singleton (for StopOnFirstError and other configuration)
        services.AddSingleton(options);

        // Register repository if persistence is enabled
        if (options.EnablePersistence)
        {
            if (options.Repository == null)
            {
                throw new InvalidOperationException(
                    "EnablePersistence is true but Repository is null. " +
                    "Use StoryOptions.WithInMemoryPersistence() or StoryOptions.WithSqlitePersistence(), " +
                    "or manually set options.Repository.");
            }

            services.AddSingleton<Persistence.IStoryRepository>(options.Repository);
            services.AddScoped<Orchestration.StoryManager>();
        }

        // Auto-discover all chapters in the calling assembly
        var callingAssembly = Assembly.GetCallingAssembly();
        var chapterInterfaceType = typeof(IChapter<>);

        var chapterTypes = callingAssembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType &&
                            i.GetGenericTypeDefinition() == chapterInterfaceType));

        foreach (var chapterType in chapterTypes)
        {
            services.AddTransient(chapterType);
        }

        return services;
    }
}
