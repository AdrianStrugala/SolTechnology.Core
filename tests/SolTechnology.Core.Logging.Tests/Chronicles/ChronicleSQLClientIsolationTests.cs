using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Chronicles.SQL;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleSQLClientIsolationTests : ChronicleSQLTestBase
{
    [Test]
    public async Task After_OutsideAmbientFlow_StillCompletesPendingStep()
    {
        // Arrange
        var (sut, _) = await StartHost();
        var command = Command("SELECT 1");
        var operationId = Guid.NewGuid();

        // Act
        using (sut.Begin("Import"))
        {
            Write(ChronicleSQLClientObserver.Before, operationId, command);
            Task completion;
            using (ExecutionContext.SuppressFlow())
            {
                completion = Task.Run(() => Write(ChronicleSQLClientObserver.After, operationId, command));
            }

            await completion;
        }

        // Assert
        var leaf = SQLSteps(Listener.Completed.Single()).Single();
        leaf.IsClosed.Should().BeTrue();
        leaf.IsUnfinished.Should().BeFalse();
    }

    [Test]
    public async Task After_WithEmptyOperationId_Ignored()
    {
        // Arrange
        var (sut, _) = await StartHost();
        var command = Command("SELECT 1");

        // Act
        using (sut.Begin("Import"))
        {
            Write(ChronicleSQLClientObserver.Before, Guid.NewGuid(), command);
            Write(ChronicleSQLClientObserver.After, Guid.Empty, command);
        }

        // Assert
        SQLSteps(Listener.Completed.Single()).Single().IsUnfinished.Should().BeTrue();
    }

    [Test]
    public async Task TwoSubscriptions_OneCommand_RecordedOnce()
    {
        // Arrange
        var (sut, _) = await StartHost();
        await StartHost();

        // Act
        using (sut.Begin("Import"))
        {
            ExecuteCommand(Command("SELECT 1"));
        }

        // Assert
        SQLSteps(Listener.Completed.Single()).Should().ContainSingle();
    }

    [Test]
    public async Task Subscription_Stopped_ObserverReceivesNoEvents()
    {
        // Arrange
        var (sut, host) = await StartHost();
        var subscription = host.GetServices<IHostedService>().OfType<ChronicleSQLClientSubscription>().Single();
        await subscription.StopAsync(CancellationToken.None);

        // Act
        using (sut.Begin("Import"))
        {
            SqlClient.IsEnabled(ChronicleSQLClientObserver.Before).Should().BeFalse();
            SqlClient.Write(ChronicleSQLClientObserver.Before, new List<KeyValuePair<string, object>> { new("OperationId", Guid.NewGuid()), new("Command", Command("SELECT 1")) });
        }

        // Assert
        SQLSteps(Listener.Completed.Single()).Should().BeEmpty();
        subscription.Observer.Should().BeNull();
    }

    [Test]
    public async Task OtherSubscriberEnabled_BeforeOutsideRoot_RecordsNothing()
    {
        // Arrange
        var (sut, host) = await StartHost();
        var observer = host.GetServices<IHostedService>().OfType<ChronicleSQLClientSubscription>().Single().Observer!;
        using var other = SqlClient.Subscribe(new NullObserver(), _ => true);
        var command = Command("SELECT 1");

        // Act
        ExecuteCommand(command);
        using (sut.Begin("Import"))
        {
        }

        // Assert
        observer.PendingCount.Should().Be(0);
        SQLSteps(Listener.Completed.Single()).Should().BeEmpty();
    }

    [Test]
    public async Task SQLStep_WithOperationsListener_StartsNoActivity()
    {
        // Arrange
        var (sut, _) = await StartHost();
        var started = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = started.Enqueue
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using (sut.Begin("Import"))
        {
            ExecuteCommand(Command("SELECT 1"));
        }

        // Assert
        SQLSteps(Listener.Completed.Single()).Should().ContainSingle();
        started.Select(a => a.OperationName).Should().Equal("Import");
    }

    private sealed class NullObserver : IObserver<KeyValuePair<string, object?>>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
        }
    }
}
