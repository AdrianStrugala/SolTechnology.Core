using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Networks;

namespace SolTechnology.Core.Testing.Containers;

/// <summary>
/// Shared Testcontainers context for a test assembly: owns the docker <see cref="INetwork"/> that
/// container fixtures attach to, and centralises the <c>TESTCONTAINERS_REUSE</c> reuse policy so the
/// individual fixtures (SQL, Redis, Blob, Service Bus) never re-implement it.
/// </summary>
/// <remarks>
/// When reuse is enabled, containers are kept alive between runs to cut repeated boot cost; the
/// network is named and reusable, and <see cref="DisposeAsync"/> is a no-op. Ryuk (the resource
/// reaper) is disabled so the suite runs under Docker Desktop Enhanced Container Isolation (ECI),
/// which blocks the <c>testcontainers/ryuk</c> image.
/// </remarks>
public sealed class TestContainersContext : IAsyncDisposable
{
    /// <summary>
    /// When <see langword="true"/>, containers and the network are kept alive between test runs to
    /// speed up iteration. Controlled by the <c>TESTCONTAINERS_REUSE</c> environment variable.
    /// Defaults to <see langword="false"/> so CI stays hermetic.
    /// </summary>
    public static bool ReuseContainers { get; } = ResolveReuseContainers();

    /// <summary>The docker network container fixtures attach to.</summary>
    public INetwork Network { get; } = CreateNetwork();

    /// <summary>Stable network alias used by inter-container references.</summary>
    public string NetworkAlias { get; } = "soltech-test-network";

    static TestContainersContext()
    {
        // Disable Ryuk to allow running under Docker Desktop Enhanced Container Isolation (ECI),
        // which blocks the testcontainers/ryuk image.
        TestcontainersSettings.ResourceReaperEnabled = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (!ReuseContainers)
        {
            await Network.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static bool ResolveReuseContainers()
    {
        var envVar = Environment.GetEnvironmentVariable("TESTCONTAINERS_REUSE");
        var enabled = string.Equals(envVar, "true", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine(
            $"[Testcontainers] Reuse {(enabled ? "enabled" : "disabled")} (TESTCONTAINERS_REUSE={envVar ?? "not set"})");
        return enabled;
    }

    private static INetwork CreateNetwork()
    {
        var builder = new NetworkBuilder();

        if (ReuseContainers)
        {
            builder = builder
                .WithReuse(true)
                .WithLabel("testcontainers.reuse", "true")
                .WithName("soltech-core-integration-tests");
        }

        return builder.Build();
    }
}

