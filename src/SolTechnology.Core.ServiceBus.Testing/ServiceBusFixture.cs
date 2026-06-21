using System.Collections.Concurrent;
using Docker.DotNet;
using Docker.DotNet.Models;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.ServiceBus;

namespace SolTechnology.Core.ServiceBus.Testing;

/// <summary>
/// Fixture for the Azure Service Bus emulator container — the most lifetime-sensitive of the testing
/// companions. Supports multiple named instances so different test fixtures can own their own emulator.
/// </summary>
/// <remarks>
/// <para>
/// The emulator persists its state in SQL Server; <c>Testcontainers.ServiceBus</c> provisions and manages
/// that MSSQL sidecar internally (you must not wire your own — see <see cref="ServiceBusInstanceBuilder"/>).
/// </para>
/// <para>
/// When <see cref="TestContainersContext.ReuseContainers"/> is enabled, the fixture manages the
/// container manually via Docker.DotNet using a well-known name. This avoids Testcontainers' reuse hash,
/// which is unstable here because the emulator references its MSSQL sidecar. On first run it creates the
/// container via Testcontainers with a fixed name; on subsequent runs it detects the existing container by
/// name, starts it if stopped, and re-reads the mapped port to rebuild the connection string.
/// </para>
/// <para>
/// Readiness is gated on <see cref="ContainerLifecycleHelper.WaitForAmqpReadyAsync"/> — a TCP-accept alone
/// causes <c>NullReferenceException</c> in <c>AmqpTransportInitiator</c>; do not regress this.
/// </para>
/// </remarks>
public sealed class ServiceBusFixture : IAsyncDisposable
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionStrings = new();
    private static readonly ConcurrentDictionary<string, bool> InitializedInstances = new();

    private const int ServiceBusPort = 5672;

    private readonly string? _configFilePath;
    private readonly string _containerName;

    /// <summary>The Testcontainers-managed container (only set when created fresh; null when reused by name).</summary>
    public ServiceBusContainer? Container { get; private set; }

    /// <param name="containerName">Stable container name (used for reuse-by-name). Suffix with <paramref name="instanceName"/> for multiple emulators.</param>
    /// <param name="instanceName">Optional suffix for running multiple named emulator instances side by side.</param>
    /// <param name="configFilePath">Optional path to a custom emulator topology; null uses the bundled default.</param>
    public ServiceBusFixture(
        string containerName = "soltech-servicebus-emulator",
        string? instanceName = null,
        string? configFilePath = null)
    {
        _configFilePath = configFilePath;
        _containerName = string.IsNullOrEmpty(instanceName) ? containerName : $"{containerName}-{instanceName}";
    }

    /// <summary>The emulator connection string, whether the container was created fresh or reused.</summary>
    public string ConnectionString =>
        ConnectionStrings.TryGetValue(_containerName, out var cs) ? cs
        : Container?.GetConnectionString()
        ?? throw new InvalidOperationException("ServiceBusFixture not initialized. Call InitializeAsync first.");

    public async Task InitializeAsync()
    {
        var semaphore = Semaphores.GetOrAdd(_containerName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (InitializedInstances.TryGetValue(_containerName, out var initialized) && initialized)
            {
                // Already initialized this run. The container may have been stopped externally
                // (e.g. via Docker Desktop) — make sure it is running again.
                if (Container != null)
                {
                    await ContainerLifecycleHelper.EnsureRunningAsync(Container.Id).ConfigureAwait(false);
                }
                else if (TestContainersContext.ReuseContainers)
                {
                    await TryReuseExistingContainer().ConfigureAwait(false);
                }

                return;
            }

            if (TestContainersContext.ReuseContainers && await TryReuseExistingContainer().ConfigureAwait(false))
            {
                InitializedInstances[_containerName] = true;
                return;
            }

            // Remove a stale container with our name before creating a new one.
            if (TestContainersContext.ReuseContainers)
            {
                await RemoveContainerByName().ConfigureAwait(false);
            }

            var builder = ServiceBusInstanceBuilder.CreateBuilder(_configFilePath);

            // Only pin the name when reusing across runs.
            if (TestContainersContext.ReuseContainers)
            {
                builder = builder.WithName(_containerName);
            }

            Container = builder.Build();
            await Container.StartAsync().ConfigureAwait(false);

            // Testcontainers' built-in wait only confirms the TCP port is open. Gate on the AMQP probe.
            var amqpPort = Container.GetMappedPublicPort(ServiceBusPort);
            await ContainerLifecycleHelper.WaitForAmqpReadyAsync(amqpPort).ConfigureAwait(false);

            ConnectionStrings[_containerName] = Container.GetConnectionString();
            InitializedInstances[_containerName] = true;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!TestContainersContext.ReuseContainers && Container != null)
        {
            await Container.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Finds and reuses an existing emulator container from a previous run by its well-known name.</summary>
    private async Task<bool> TryReuseExistingContainer()
    {
        try
        {
            using var docker = new DockerClientBuilder().Build();
            var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [$"^/{_containerName}$"] = true }
                }
            }).ConfigureAwait(false);

            if (containers.Count == 0)
            {
                return false;
            }

            var existing = containers[0];

            if (!existing.State.Equals("running", StringComparison.OrdinalIgnoreCase))
            {
                await docker.Containers.StartContainerAsync(existing.ID, new ContainerStartParameters()).ConfigureAwait(false);
                await ContainerLifecycleHelper.EnsureRunningAsync(existing.ID).ConfigureAwait(false);
            }

            var hostPort = GetMappedPort(existing.Ports, ServiceBusPort);
            if (hostPort == null)
            {
                var inspected = await docker.Containers.InspectContainerAsync(existing.ID).ConfigureAwait(false);
                hostPort = GetMappedPortFromInspect(inspected, ServiceBusPort);
            }

            if (hostPort == null)
            {
                return false;
            }

            // Gate the reused container on AMQP readiness too — it may have just been restarted.
            await ContainerLifecycleHelper.WaitForAmqpReadyAsync(hostPort.Value).ConfigureAwait(false);

            ConnectionStrings[_containerName] = BuildConnectionString(hostPort.Value);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ServiceBusFixture] Error trying to reuse container '{_containerName}': {ex.Message}");
            return false;
        }
    }

    private async Task RemoveContainerByName()
    {
        try
        {
            using var docker = new DockerClientBuilder().Build();
            var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [$"^/{_containerName}$"] = true }
                }
            }).ConfigureAwait(false);

            foreach (var container in containers)
            {
                await docker.Containers.StopContainerAsync(container.ID,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 0 }).ConfigureAwait(false);
                await docker.Containers.RemoveContainerAsync(container.ID,
                    new ContainerRemoveParameters { Force = true }).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ServiceBusFixture] Failed to remove container '{_containerName}' by name: {ex.Message}");
        }
    }

    private static ushort? GetMappedPort(IList<PortSummary> ports, int containerPort)
    {
        var mapping = ports.FirstOrDefault(p => p.PrivatePort == containerPort && p.PublicPort > 0);
        return mapping?.PublicPort;
    }

    private static ushort? GetMappedPortFromInspect(ContainerInspectResponse inspect, int containerPort)
    {
        var key = $"{containerPort}/tcp";
        if (inspect.NetworkSettings?.Ports == null || !inspect.NetworkSettings.Ports.TryGetValue(key, out var bindings))
        {
            return null;
        }

        var binding = bindings?.FirstOrDefault(b => !string.IsNullOrEmpty(b.HostPort));
        return binding == null ? null : ushort.Parse(binding.HostPort);
    }

    private static string BuildConnectionString(ushort hostPort) =>
        $"Endpoint=sb://localhost:{hostPort};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
}

