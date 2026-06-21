using System.Net.Sockets;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace SolTechnology.Core.Testing.Containers;

/// <summary>
/// Keeps reused containers healthy and provides protocol-level readiness probes. When
/// <see cref="TestContainersContext.ReuseContainers"/> is enabled a container may have been stopped
/// externally (e.g. via Docker Desktop) while a fixture's static "initialized" flag is still set;
/// these helpers restart it and wait until it is genuinely ready to accept connections.
/// </summary>
public static class ContainerLifecycleHelper
{
    // AMQP 1.0 SASL header: 'A','M','Q','P', protocol-id=3 (SASL), major=1, minor=0, revision=0.
    // The Azure Service Bus emulator speaks SASL before raw AMQP. Sending this header and receiving
    // the echoed 'AMQP' back is the only reliable signal that the broker is fully initialised —
    // TCP-accept alone is insufficient because the OS queues connections while the broker boots,
    // which surfaces as a NullReferenceException in AmqpTransportInitiator.
    private static readonly byte[] AmqpSaslHeader = [0x41, 0x4D, 0x51, 0x50, 0x03, 0x01, 0x00, 0x00];

    /// <summary>
    /// Inspects the container and issues a <c>docker start</c> if it is not running, then waits for
    /// its Docker health check (or simply for "running" when no health check is configured).
    /// Failures are logged and swallowed so the original connection error surfaces instead of an
    /// unrelated Docker API exception.
    /// </summary>
    public static async Task EnsureRunningAsync(string containerId, CancellationToken ct = default)
    {
        var shortId = containerId.Length > 12 ? containerId[..12] : containerId;
        try
        {
            using var docker = new DockerClientBuilder().Build();
            var inspect = await docker.Containers.InspectContainerAsync(containerId, ct).ConfigureAwait(false);

            if (inspect.State is null || !inspect.State.Running)
            {
                Console.WriteLine($"[ContainerLifecycle] Container {shortId} was stopped; restarting...");
                await docker.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), ct)
                    .ConfigureAwait(false);
                await WaitForHealthyAsync(docker, containerId, shortId, ct: ct).ConfigureAwait(false);
                Console.WriteLine($"[ContainerLifecycle] Container {shortId} restarted and ready.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContainerLifecycle] Warning: could not ensure container {shortId} is running: {ex.Message}");
        }
    }

    /// <summary>
    /// Polls the Docker health status until the container is <c>healthy</c> (or running with no
    /// health check), or throws on timeout / unhealthy.
    /// </summary>
    public static async Task WaitForHealthyAsync(
        DockerClient docker,
        string containerId,
        string shortId,
        int timeoutSeconds = 60,
        CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var inspect = await docker.Containers.InspectContainerAsync(containerId, ct).ConfigureAwait(false);
            var state = inspect.State;

            if (state is not null && state.Running)
            {
                if (state.Health == null || state.Health.Status == "healthy")
                {
                    return;
                }

                if (state.Health.Status == "unhealthy")
                {
                    throw new InvalidOperationException($"Container {shortId} became unhealthy after restart.");
                }
            }

            await Task.Delay(500, ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"Container {shortId} did not become healthy within {timeoutSeconds}s.");
    }

    /// <summary>
    /// AMQP-level readiness probe: sends the 8-byte AMQP SASL header to <paramref name="hostPort"/>
    /// and waits until the broker echoes 'AMQP' back. The only reliable signal that the Azure Service
    /// Bus emulator is ready to accept sessions.
    /// </summary>
    public static async Task WaitForAmqpReadyAsync(int hostPort, int timeoutSeconds = 120)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            // Each iteration gets its own 3-second budget. A single token bounds ConnectAsync,
            // WriteAsync and ReadAsync — NetworkStream timeouts do not reliably cancel async ops.
            using var iterationCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var ct = iterationCts.Token;

            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("localhost", hostPort, ct).ConfigureAwait(false);

                var stream = tcp.GetStream();
                await stream.WriteAsync(AmqpSaslHeader, ct).ConfigureAwait(false);

                var response = new byte[4];
                var bytesRead = 0;
                while (bytesRead < 4)
                {
                    var n = await stream.ReadAsync(response.AsMemory(bytesRead, 4 - bytesRead), ct)
                        .ConfigureAwait(false);
                    if (n == 0)
                    {
                        break; // server closed connection — not ready yet
                    }

                    bytesRead += n;
                }

                if (bytesRead == 4
                    && response[0] == 0x41  // A
                    && response[1] == 0x4D  // M
                    && response[2] == 0x51  // Q
                    && response[3] == 0x50) // P
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
            {
                // Not ready yet (connection refused, timeout, partial response) — retry.
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException($"AMQP broker on port {hostPort} did not become ready within {timeoutSeconds}s.");
    }
}

