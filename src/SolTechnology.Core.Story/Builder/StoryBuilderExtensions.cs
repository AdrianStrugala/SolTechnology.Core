using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Builder;

/// <summary>
/// Persistence-provider extension methods for <see cref="IStoryBuilder"/>. Each
/// <c>UseXxxStoryRepository</c> replaces whatever <see cref="IStoryRepository"/> was
/// registered previously. A persistence provider is always present — the default after
/// <c>RegisterStories()</c> is in-memory.
/// </summary>
public static class StoryBuilderExtensions
{
    /// <summary>
    /// Use the built-in <see cref="InMemoryStoryRepository"/>. This is already the default
    /// after <c>RegisterStories()</c>; call this only for explicit intent.
    /// </summary>
    public static IStoryBuilder UseInMemoryStoryRepository(this IStoryBuilder builder)
    {
        ReplaceRepository(builder.Services, ServiceLifetime.Singleton,
            _ => new InMemoryStoryRepository());
        EnsureStoryManager(builder.Services);
        return builder;
    }

    /// <summary>
    /// Use the built-in <see cref="SqliteStoryRepository"/> with the given connection string.
    /// </summary>
    /// <example><c>.UseSqliteStoryRepository("Data Source=stories.db")</c></example>
    public static IStoryBuilder UseSqliteStoryRepository(
        this IStoryBuilder builder,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        return builder.UseSqliteStoryRepository(opts => opts.ConnectionString = connectionString);
    }

    /// <summary>
    /// Use the built-in <see cref="SqliteStoryRepository"/> with full control over
    /// <see cref="SqliteStoryRepositoryOptions"/> (connection string, retries, WAL mode).
    /// </summary>
    public static IStoryBuilder UseSqliteStoryRepository(
        this IStoryBuilder builder,
        Action<SqliteStoryRepositoryOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        var options = new SqliteStoryRepositoryOptions();
        configure(options);

        builder.Services.RemoveAll<SqliteStoryRepositoryOptions>();
        builder.Services.AddSingleton(options);

        ReplaceRepository(builder.Services, ServiceLifetime.Singleton,
            _ => new SqliteStoryRepository(options));
        EnsureStoryManager(builder.Services);
        return builder;
    }

    /// <summary>
    /// Register an arbitrary <see cref="IStoryRepository"/> implementation. The caller is
    /// responsible for registering any transitive dependencies of
    /// <typeparamref name="TRepository"/> (e.g. <c>DbContext</c>, connection pool, …).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="lifetime">Desired lifetime. Singleton by default.</param>
    public static IStoryBuilder UseStoryRepository<TRepository>(
        this IStoryBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TRepository : class, IStoryRepository
    {
        builder.Services.RemoveAll<IStoryRepository>();
        builder.Services.Add(new ServiceDescriptor(
            typeof(IStoryRepository), typeof(TRepository), lifetime));
        EnsureStoryManager(builder.Services);
        return builder;
    }

    private static void ReplaceRepository(
        IServiceCollection services,
        ServiceLifetime lifetime,
        Func<IServiceProvider, IStoryRepository> factory)
    {
        services.RemoveAll<IStoryRepository>();
        services.Add(new ServiceDescriptor(typeof(IStoryRepository), factory, lifetime));
    }

    private static void EnsureStoryManager(IServiceCollection services)
    {
        // StoryManager is Scoped and reads IStoryRepository through the service scope factory.
        // Multiple calls to Use*StoryRepository must not duplicate the registration.
        if (services.Any(s => s.ServiceType == typeof(StoryManager))) return;
        services.AddScoped<StoryManager>();
    }
}

