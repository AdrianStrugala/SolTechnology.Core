using Docker.DotNet;
using Docker.DotNet.Models;
using SolTechnology.Core.Testing.Containers;

namespace SolTechnology.Core.ServiceBus.Testing;

/// <summary>
/// Finds, restarts and removes an emulator kept between runs (<c>TESTCONTAINERS_REUSE</c>) by its well-known
/// names: the emulator <c>{name}</c>, its MSSQL sidecar <c>{name}-mssql</c> and their network <c>{name}-network</c>.
/// Testcontainers' reuse hash is unstable for the emulator because it references the sidecar.
/// </summary>
internal static class ReusedEmulator
{
    internal static string DatabaseName(string containerName) => $"{containerName}-mssql";

    internal static string NetworkName(string containerName) => $"{containerName}-network";

    /// <summary>Starts the named emulator (and its sidecar) if stopped and returns its connection string, or null.</summary>
    internal static async Task<string?> TryReuse(string containerName, int amqpPort, CancellationToken ct)
    {
        try
        {
            using var docker = new DockerClientBuilder().Build();
            var emulator = await Find(docker, containerName, ct).ConfigureAwait(false);
            if (emulator is null)
            {
                return null;
            }

            if (await Find(docker, DatabaseName(containerName), ct).ConfigureAwait(false) is { } database)
            {
                await StartIfStopped(docker, database, ct).ConfigureAwait(false);
            }

            await StartIfStopped(docker, emulator, ct).ConfigureAwait(false);

            var inspected = await docker.Containers.InspectContainerAsync(emulator.ID, ct).ConfigureAwait(false);
            var hostPort = MappedPort(inspected, amqpPort);
            if (hostPort is null)
            {
                return null;
            }

            // Gate the reused container on AMQP readiness too — it may have just been restarted.
            await ContainerLifecycleHelper.WaitForAmqpReadyAsync(hostPort.Value).ConfigureAwait(false);
            return ConnectionString(hostPort.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[ServiceBusFixture] Error trying to reuse container '{containerName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>Removes a stale emulator, its sidecar and their network, so a fresh one can take the names.</summary>
    internal static async Task Remove(string containerName, CancellationToken ct)
    {
        try
        {
            using var docker = new DockerClientBuilder().Build();
            foreach (var name in new[] { containerName, DatabaseName(containerName) })
            {
                if (await Find(docker, name, ct).ConfigureAwait(false) is { } container)
                {
                    await docker.Containers.RemoveContainerAsync(container.ID,
                        new ContainerRemoveParameters { Force = true }, ct).ConfigureAwait(false);
                }
            }

            var networks = await docker.Networks.ListNetworksAsync(new NetworksListParameters
            {
                Filters = NameFilter($"^{NetworkName(containerName)}$")
            }, ct).ConfigureAwait(false);

            foreach (var network in networks)
            {
                await docker.Networks.DeleteNetworkAsync(network.ID, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[ServiceBusFixture] Failed to remove container '{containerName}' by name: {ex.Message}");
        }
    }

    internal static string ConnectionString(ushort hostPort) =>
        $"Endpoint=sb://localhost:{hostPort};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    private static async Task<ContainerListResponse?> Find(DockerClient docker, string name, CancellationToken ct)
    {
        var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = NameFilter($"^/{name}$")
        }, ct).ConfigureAwait(false);

        return containers.FirstOrDefault();
    }

    private static async Task StartIfStopped(DockerClient docker, ContainerListResponse container, CancellationToken ct)
    {
        if (!container.State.Equals("running", StringComparison.OrdinalIgnoreCase))
        {
            await docker.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), ct).ConfigureAwait(false);
            await ContainerLifecycleHelper.EnsureRunningAsync(container.ID, ct).ConfigureAwait(false);
        }
    }

    private static Dictionary<string, IDictionary<string, bool>> NameFilter(string pattern) =>
        new() { ["name"] = new Dictionary<string, bool> { [pattern] = true } };

    private static ushort? MappedPort(ContainerInspectResponse inspect, int containerPort)
    {
        if (inspect.NetworkSettings?.Ports is null
            || !inspect.NetworkSettings.Ports.TryGetValue($"{containerPort}/tcp", out var bindings))
        {
            return null;
        }

        var binding = bindings?.FirstOrDefault(b => !string.IsNullOrEmpty(b.HostPort));
        return binding is null ? null : ushort.Parse(binding.HostPort);
    }
}
