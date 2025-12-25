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
    /// <param name="configureOptions">Optional callback to configure StoryOptions (for persistence, REST API, etc)</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // Simple usage (no persistence)
    /// services.RegisterStories();
    ///
    /// // With in-memory persistence
    /// services.RegisterStories(options =>
    /// {
    ///     options.EnablePersistence = true;
    ///     options.Repository = new InMemoryStoryRepository();
    /// });
    ///
    /// // With SQLite persistence
    /// services.RegisterStories(options =>
    /// {
    ///     options.EnablePersistence = true;
    ///     options.Repository = new SqliteStoryRepository();
    ///     options.EnableRestApi = true;
    /// });
    ///
    /// // Or use factory methods
    /// services.RegisterStories(options => options = StoryOptions.WithSqlitePersistence());
    /// </code>
    /// </example>
    public static IServiceCollection RegisterStories(
        this IServiceCollection services,
        Action<StoryOptions>? configureOptions = null)
    {
        var options = StoryOptions.Default;
        configureOptions?.Invoke(options);

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

    /// <summary>
    /// Register the Story framework using a pre-configured StoryOptions instance.
    /// Useful when you want to build options elsewhere and pass them in.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">Pre-configured StoryOptions</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterStories(
        this IServiceCollection services,
        StoryOptions options)
    {
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
