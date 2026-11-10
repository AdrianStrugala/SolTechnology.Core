using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class TaleChronicleManagedRunTests : TaleChronicleTestBase
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    [Test]
    public async Task StartStory_ManagedRun_TaleStepHasChapterChildren()
    {
        // Act
        var start = await Manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });

        // Assert
        start.Data!.Status.Should().Be(TaleStatus.Completed);
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Tale LifecycleTaleV2] [Ok]")
            .And.Contain("\n|- Chapter LifecyclePassingChapter [@").And.Contain("\n`- Chapter LifecycleCompleteChapter [@");
    }

    [Test]
    public async Task Handle_Resume_CompletedChaptersHaveNoStep()
    {
        // Arrange
        var start = await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        var input = JsonSerializer.SerializeToElement(new PausePayload { Token = "abc" });

        // Act
        var resumed = await Manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(start.Data!.TaleId, input);

        // Assert
        resumed.Data!.Status.Should().Be(TaleStatus.Completed);
        Reports.Should().HaveCount(2);
        Reports[1].Message.Should().StartWith("Chronicle [Tale LifecycleTaleV1] [Ok]")
            .And.Contain("\n|- Chapter LifecyclePauseChapter [@").And.Contain("\n`- Chapter LifecycleCompleteChapter [@")
            .And.NotContain("LifecyclePassingChapter").And.NotContain("abc");
    }

    [Test]
    public async Task Handle_LeaseTakenOver_ChapterCancelledAndTaleFailed()
    {
        // Arrange
        var clock = new TimerCountingTimeProvider(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTaleRepository();
        var gate = new LongChapterGate();
        Services.Dispose();
        Services = Build(services => services
            .AddSingleton<ITaleRepository>(new GatedTaleRepository(store))
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton(gate));
        var run = Manager.StartStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        var taleId = await gate.Entered.WaitAsync(SignalTimeout);
        await TakeOver(store, taleId, clock);

        // Act
        clock.Advance(new TaleOptions().RunningLeaseTimeout / 3);
        var result = await run.WaitAsync(SignalTimeout);

        // Assert
        result.Error.Should().BeOfType<ConflictError>();
        var lines = TreeLines();
        lines[0].Should().StartWith("Tale HeartbeatTale [@0 ms]").And.Contain("[failed] Error=[ConflictError: ");
        lines.Should().ContainSingle(l => l.Contains("Chapter LongRunningChapter")).Which.Should().EndWith("[cancelled] Outcome=LeaseLost");
    }

    private TaleManager Manager => Services.GetRequiredService<TaleManager>();

    private static async Task TakeOver(InMemoryTaleRepository store, Auid taleId, TimeProvider clock)
    {
        var newOwner = (await store.FindById(taleId, CancellationToken.None))!;
        newOwner.LeaseExpiresAt = clock.GetUtcNow() + new TaleOptions().RunningLeaseTimeout;
        (await store.Save(newOwner, CancellationToken.None)).IsSuccess.Should().BeTrue();
    }
}
