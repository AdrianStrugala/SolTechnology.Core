using System.Collections.Concurrent;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.ServiceBus;

namespace SolTechnology.Core.ServiceBus.Testing;

/// <summary>
/// Fixture for the Azure Service Bus emulator container — the most lifetime-sensitive of the testing
/// companions. Supports multiple named instances so different test fixtures can own their own emulator.
/// </summary>
/// <remarks>
/// <para>
/// The emulator persists its state in SQL Server. The fixture creates that MSSQL sidecar and the network
/// they share, passes them to <c>Testcontainers.ServiceBus</c>, and removes all three in
/// <see cref="DisposeAsync"/>; disposing the emulator container alone would leave the sidecar running.
/// </para>
/// <para>
/// Each instance owns its state: two fixtures with the same name do not share a connection string, and
/// disposing one does not break the other. When <see cref="TestContainersContext.ReuseContainers"/> is
/// enabled, the emulator, sidecar and network get well-known names and stay between runs; a later run finds
/// them by name, starts them if stopped, and re-reads the mapped port to rebuild the connection string.
/// </para>
/// <para>
/// Readiness is gated on <see cref="ContainerLifecycleHelper.WaitForAmqpReady"/> — a TCP-accept alone
/// causes <c>NullReferenceException</c> in <c>AmqpTransportInitiator</c>; do not regress this.
/// </para>
/// </remarks>
/// <param name="containerName">Stable container name (used for reuse-by-name). Suffix with <paramref name="instanceName"/> for multiple emulators.</param>
/// <param name="instanceName">Optional suffix for running multiple named emulator instances side by side.</param>
/// <param name="configFilePath">Optional path to a custom emulator topology; null uses the bundled default.</param>
public sealed class ServiceBusFixture(
    string containerName = "soltech-servicebus-emulator",
    string? instanceName = null,
    string? configFilePath = null) : IAsyncDisposable
{
    // Reuse mode pins one Docker container per name, so fixtures sharing a name start it one at a time.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new();

    private readonly string _containerName =
        string.IsNullOrEmpty(instanceName) ? containerName : $"{containerName}-{instanceName}";

    private string? _connectionString;
    private OwnedEmulator? _emulator;

    /// <summary>The Testcontainers-managed container (only set when created fresh; null when reused by name).</summary>
    public ServiceBusContainer? Container => _emulator?.Container;

    /// <summary>The emulator connection string, whether the container was created fresh or reused.</summary>
    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("ServiceBusFixture not initialized. Call Initialize first.");

    /// <summary>
    /// Starts the emulator with its MSSQL sidecar, or reuses the one kept by an earlier run, and waits until it
    /// accepts AMQP sessions. Calling it again only restarts a container that was stopped externally.
    /// </summary>
    public async Task Initialize(CancellationToken ct = default)
    {
        var semaphore = Semaphores.GetOrAdd(_containerName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_connectionString is not null)
            {
                // Already initialized; the container may have been stopped externally (e.g. via Docker Desktop).
                await EnsureRunning(ct).ConfigureAwait(false);
                return;
            }

            if (TestContainersContext.ReuseContainers)
            {
                _connectionString = await ReusedEmulator.TryReuse(_containerName, OwnedEmulator.AmqpPort, ct).ConfigureAwait(false);
                if (_connectionString is not null)
                {
                    return;
                }

                await ReusedEmulator.Remove(_containerName, ct).ConfigureAwait(false);
            }

            await StartEmulator(ct).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>Removes the emulator, its MSSQL sidecar and their network; keeps them when reuse is enabled.</summary>
    public async ValueTask DisposeAsync()
    {
        if (TestContainersContext.ReuseContainers || _emulator is null)
        {
            return;
        }

        _connectionString = null;
        try
        {
            await _emulator.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _emulator = null;
        }
    }

    private async Task StartEmulator(CancellationToken ct)
    {
        _emulator = new OwnedEmulator(_containerName, configFilePath, TestContainersContext.ReuseContainers);
        try
        {
            _connectionString = await _emulator.Start(ct).ConfigureAwait(false);
        }
        catch (Exception startFailure)
        {
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                // The start failure is the cause; keep it and attach what the clean-up could not remove.
                startFailure.Data["ServiceBusFixture.CleanupFailure"] = cleanupFailure;
                Console.WriteLine($"[ServiceBusFixture] Clean-up after a failed start of '{_containerName}' failed: {cleanupFailure.Message}");
            }

            throw;
        }
    }

    private async Task EnsureRunning(CancellationToken ct)
    {
        if (_emulator is not null)
        {
            await _emulator.EnsureRunning(ct).ConfigureAwait(false);
        }
        else if (TestContainersContext.ReuseContainers)
        {
            _connectionString = await ReusedEmulator.TryReuse(_containerName, OwnedEmulator.AmqpPort, ct).ConfigureAwait(false)
                ?? _connectionString;
        }
    }
}
