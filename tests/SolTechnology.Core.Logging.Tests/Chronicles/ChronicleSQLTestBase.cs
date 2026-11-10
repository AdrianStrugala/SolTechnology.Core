using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Chronicles.SQL;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

/// <summary>Hosts with a started SQL subscription and a synthetic SqlClient listener; everything is disposed per test.</summary>
public abstract class ChronicleSQLTestBase : ChronicleTestBase
{
    private readonly List<ServiceProvider> _hosts = [];

    protected DiagnosticListener SqlClient { get; private set; } = null!;

    [SetUp]
    public void CreateListener() => SqlClient = new DiagnosticListener(ChronicleSQLClientObserver.ListenerName);

    [TearDown]
    public async Task DisposeHosts()
    {
        foreach (var host in _hosts)
        {
            foreach (var service in host.GetServices<IHostedService>())
            {
                await service.StopAsync(CancellationToken.None);
            }

            await host.DisposeAsync();
        }

        _hosts.Clear();
        SqlClient.Dispose();
    }

    internal async Task<(IChronicle Chronicle, ServiceProvider Host)> StartHost(Action<ChronicleOptions>? configure = null)
    {
        var host = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton<IChronicleRootListener>(Listener)
            .AddSolChronicle(configure)
            .BuildServiceProvider();
        _hosts.Add(host);
        foreach (var service in host.GetServices<IHostedService>())
        {
            await service.StartAsync(CancellationToken.None);
        }

        return (host.GetRequiredService<IChronicle>(), host);
    }

    protected static IDbCommand Command(string text, CommandType type = CommandType.Text)
    {
        var command = Substitute.For<IDbCommand>();
        command.CommandText.Returns(text);
        command.CommandType.Returns(type);
        return command;
    }

    protected Guid ExecuteCommand(IDbCommand command, int durationMs = 1, Exception? error = null)
    {
        var operationId = Guid.NewGuid();
        Write(ChronicleSQLClientObserver.Before, operationId, command);
        Time.Advance(TimeSpan.FromMilliseconds(durationMs));
        Write(error is null ? ChronicleSQLClientObserver.After : ChronicleSQLClientObserver.Error, operationId, command, error);
        return operationId;
    }

    protected void Write(string eventName, Guid operationId, IDbCommand command, Exception? error = null)
    {
        if (!SqlClient.IsEnabled(eventName))
        {
            return;
        }

        List<KeyValuePair<string, object>> payload = [new("OperationId", operationId), new("Operation", "ExecuteReader"), new("Command", command)];
        if (error is not null)
        {
            payload.Add(new("Exception", error));
        }

        SqlClient.Write(eventName, payload);
    }

    internal static ChronicleNode[] SQLSteps(ChronicleRoot root) => root.Node.SnapshotChildren().Where(c => c.Name.StartsWith("SQL")).ToArray();
}
