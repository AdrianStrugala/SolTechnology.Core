using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Builder;

/// <summary>
/// Persistence-provider extension methods for <see cref="ITaleBuilder"/>.
/// <c>UseTaleRepository&lt;T&gt;</c> replaces whatever <see cref="ITaleRepository"/> was
/// registered previously. A persistence provider is always present — the default after
/// <c>AddSolTale()</c> is in-memory.
/// </summary>
public static class TaleBuilderExtensions
{
    /// <summary>
    /// Use the built-in <see cref="InMemoryTaleRepository"/>. This is already the default
    /// after <c>AddSolTale()</c>; call this only for explicit intent.
    /// </summary>
    public static ITaleBuilder UseInMemoryTaleRepository(this ITaleBuilder builder)
    {
        ReplaceRepository(builder.Services, ServiceLifetime.Singleton,
            _ => new InMemoryTaleRepository());
        EnsureTaleManager(builder.Services);
        return builder;
    }

    /// <summary>
    /// Register an arbitrary <see cref="ITaleRepository"/> implementation. The caller is
    /// responsible for registering any transitive dependencies of
    /// <typeparamref name="TRepository"/> (e.g. <c>DbContext</c>, connection pool, …).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="lifetime">Desired lifetime. Singleton by default.</param>
    public static ITaleBuilder UseTaleRepository<TRepository>(
        this ITaleBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TRepository : class, ITaleRepository
    {
        builder.Services.RemoveAll<ITaleRepository>();
        builder.Services.Add(new ServiceDescriptor(
            typeof(ITaleRepository), typeof(TRepository), lifetime));
        EnsureTaleManager(builder.Services);
        return builder;
    }

    private static void ReplaceRepository(
        IServiceCollection services,
        ServiceLifetime lifetime,
        Func<IServiceProvider, ITaleRepository> factory)
    {
        services.RemoveAll<ITaleRepository>();
        services.Add(new ServiceDescriptor(typeof(ITaleRepository), factory, lifetime));
    }

    private static void EnsureTaleManager(IServiceCollection services)
    {
        // TaleManager is Scoped and reads ITaleRepository through the service scope factory.
        // Multiple calls to Use*TaleRepository must not duplicate the registration.
        if (services.Any(s => s.ServiceType == typeof(TaleManager))) return;
        services.AddScoped<TaleManager>();
    }
}

