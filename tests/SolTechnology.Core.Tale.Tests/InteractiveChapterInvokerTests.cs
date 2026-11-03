using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class InteractiveChapterInvokerTests
{
    private RecordingLoggerProvider _logs = null!;
    private ServiceProvider _sp = null!;
    private InMemoryTaleRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _logs = new RecordingLoggerProvider();
        _repository = new InMemoryTaleRepository();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(_logs));
        services.AddSingleton<ITaleRepository>(_repository);
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(InteractiveChapterInvokerTests).Assembly);
        _sp = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _sp.Dispose();
        _logs.Dispose();
    }

    [Test]
    public async Task Resume_ReadWithInputThrowsCancellationSynchronously_StoresCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _sp.GetRequiredService<TestService>().RunCanceller = cts;
        var taleId = await StartPaused();

        // Act
        var resume = await Resume(taleId, cts.Token);

        // Assert
        resume.Error.Should().BeOfType<TaleCancelledError>();
        (await _repository.FindById(taleId, CancellationToken.None))!.Status.Should().Be(TaleStatus.Cancelled);
    }

    [Test]
    public async Task Resume_ReadWithInputThrowsSynchronously_LogsTheChapterException()
    {
        // Arrange
        var taleId = await StartPaused();

        // Act
        var resume = await Resume(taleId, CancellationToken.None);

        // Assert
        resume.IsFailure.Should().BeTrue();
        (await _repository.FindById(taleId, CancellationToken.None))!.Status.Should().Be(TaleStatus.Failed);
        _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Error && log.Message.StartsWith("Chapter ["))
            .Which.Exception.Should().BeOfType<InvalidOperationException>();
    }

    private async Task<SolTechnology.Core.AUID.Auid> StartPaused()
    {
        var start = await _sp.GetRequiredService<TaleManager>()
            .StartStory<SyncThrowingInteractiveTale, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        start.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
        return start.Data.TaleId;
    }

    private Task<Result<TaleInstance>> Resume(SolTechnology.Core.AUID.Auid taleId, CancellationToken cancellationToken) =>
        _sp.GetRequiredService<TaleManager>()
            .ResumeStory<SyncThrowingInteractiveTale, LifecycleInput, LifecycleContext, LifecycleOutput>(
                taleId, JsonSerializer.SerializeToElement(new PausePayload { Token = "x" }), cancellationToken);
}

public class SyncThrowingInteractiveChapter(TestService service) : InteractiveChapter<LifecycleContext, PausePayload>
{
    public override Task<Result> ReadWithInput(LifecycleContext ctx, PausePayload input, CancellationToken cancellationToken)
    {
        service.RunCanceller?.Cancel();
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("synchronous failure");
    }
}

public class SyncThrowingInteractiveTale(IServiceProvider sp, ILogger<SyncThrowingInteractiveTale> logger)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, logger)
{
    protected override Tale<LifecycleOutput> Tell() =>
        Open<SyncThrowingInteractiveChapter>().Finale(ctx => ctx.Output);
}
