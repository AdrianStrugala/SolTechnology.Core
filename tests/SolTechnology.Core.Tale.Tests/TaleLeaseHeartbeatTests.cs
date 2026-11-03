using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class TaleLeaseHeartbeatTests
{
    private static readonly TimeSpan Lease = new TaleOptions().RunningLeaseTimeout;
    private static readonly TimeSpan Beat = Lease / 3;
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    private TimerCountingTimeProvider _clock = null!;
    private InMemoryTaleRepository _store = null!;
    private GatedTaleRepository _repository = null!;
    private LongChapterGate _gate = null!;
    private RecordingLoggerProvider _logs = null!;
    private SemaphoreSlim _saved = null!;
    private int _saves;
    private ServiceProvider _sp = null!;
    private TaleManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new TimerCountingTimeProvider(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
        _store = new InMemoryTaleRepository();
        _repository = new GatedTaleRepository(_store);
        _gate = new LongChapterGate();
        _logs = new RecordingLoggerProvider();
        _saved = new SemaphoreSlim(0);
        _saves = 0;
        var services = new ServiceCollection().AddLogging(logging => logging.AddProvider(_logs));
        services.AddSingleton<ITaleRepository>(_repository);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(_gate);
        services.AddSolTale(assemblies: typeof(TaleLeaseHeartbeatTests).Assembly);
        _sp = services.BuildServiceProvider();
        _manager = _sp.GetRequiredService<TaleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        _sp.Dispose();
        _logs.Dispose();
        _saved.Dispose();
    }

    [Test]
    public async Task StartStory_ChapterOutlivesLease_RenewsLeaseAndRefusesTakeover()
    {
        // Arrange
        var (run, taleId) = await StartLongChapter();
        var stamps = new List<DateTime>();
        for (var beat = 0; beat < 9; beat++)
        {
            await AdvanceOneBeat();
            stamps.Add((await _store.FindById(taleId, CancellationToken.None))!.LastUpdatedAt);
        }

        // Act
        var takeover = await _manager.ResumeStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(taleId);

        // Assert
        takeover.Error.Should().BeOfType<ConflictError>().Which.Message.Should().Be("Tale is already running");
        stamps.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        stamps[^1].Should().Be(_clock.GetUtcNow().UtcDateTime);
        _gate.Release();
        (await run.WaitAsync(SignalTimeout)).Data!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task StartStory_AfterChapterEnds_StopsRenewingLease()
    {
        // Arrange
        var (run, _) = await StartLongChapter();
        await AdvanceOneBeat();
        _gate.Release();
        var result = await run.WaitAsync(SignalTimeout);
        var savesAtEnd = _saves;

        // Act
        for (var beat = 0; beat < 9; beat++)
        {
            _clock.Advance(Beat);
        }

        // Assert
        result.Data!.Status.Should().Be(TaleStatus.Completed);
        _clock.TimersCreated.Should().Be(1);
        _clock.LiveTimers.Should().Be(0);
        _saves.Should().Be(savesAtEnd);
    }

    [Test]
    public async Task StartStory_AnotherRunTookOver_StopsWithConflictAndKeepsNewOwnersState()
    {
        // Arrange
        var (run, taleId) = await StartLongChapter();
        var newOwner = (await _store.FindById(taleId, CancellationToken.None))!;
        newOwner.LastUpdatedAt = _clock.GetUtcNow().UtcDateTime;
        (await _store.Save(newOwner, CancellationToken.None)).IsSuccess.Should().BeTrue();

        // Act
        _clock.Advance(Beat);
        var result = await run.WaitAsync(SignalTimeout);

        // Assert
        result.Error.Should().BeOfType<ConflictError>();
        _gate.Token.IsCancellationRequested.Should().BeTrue();
        var stored = (await _store.FindById(taleId, CancellationToken.None))!;
        stored.Version.Should().Be(newOwner.Version);
        stored.LastUpdatedAt.Should().Be(newOwner.LastUpdatedAt);
        stored.Status.Should().Be(TaleStatus.Running);
        _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Warning)
            .Which.Message.Should().Be($"Tale [{taleId}] was taken over by another run; stopping this run");
        _clock.LiveTimers.Should().Be(0);
    }

    [Test]
    public async Task Handle_DirectRun_NeverRenewsLease()
    {
        // Arrange
        _repository.AfterSave = (_, _) => Interlocked.Increment(ref _saves);
        var handler = _sp.GetRequiredService<HeartbeatTale>();
        var run = handler.Handle(new LifecycleInput { Value = 1 }, CancellationToken.None);
        await _gate.Entered.WaitAsync(SignalTimeout);

        // Act
        _clock.Advance(Lease * 3);
        _gate.Release();
        var result = await run.WaitAsync(SignalTimeout);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _clock.TimersCreated.Should().Be(0);
        _saves.Should().Be(0);
    }

    // Starts a persisted run and returns once its first chapter is holding; later saves are signalled.
    private async Task<(Task<Result<TaleInstance>> Run, Auid TaleId)> StartLongChapter()
    {
        var run = _manager.StartStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var taleId = await _gate.Entered.WaitAsync(SignalTimeout);
        _repository.AfterSave = (_, _) =>
        {
            Interlocked.Increment(ref _saves);
            _saved.Release();
        };
        return (run, taleId);
    }

    private async Task AdvanceOneBeat()
    {
        _clock.Advance(Beat);
        (await _saved.WaitAsync(SignalTimeout)).Should().BeTrue("the heartbeat renews the lease once per beat");
    }
}

/// <summary>Holds <see cref="LongRunningChapter"/> until the test releases it and exposes the token it ran with.</summary>
public sealed class LongChapterGate
{
    private readonly TaskCompletionSource<Auid> _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<Auid> Entered => _entered.Task;

    public CancellationToken Token { get; private set; }

    public void Release() => _released.TrySetResult();

    public Task Hold(Auid taleId, CancellationToken cancellationToken)
    {
        Token = cancellationToken;
        _entered.TrySetResult(taleId);
        return _released.Task.WaitAsync(cancellationToken);
    }
}

public class LongRunningChapter(LongChapterGate gate) : Chapter<LifecycleContext>
{
    public override async Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken)
    {
        await gate.Hold(ctx.TaleInstanceId, cancellationToken);
        return Result.Success();
    }
}

public class HeartbeatTale(IServiceProvider sp, ILogger<HeartbeatTale> log)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, log)
{
    protected override Tale<LifecycleOutput> Tell() =>
        Open<LongRunningChapter>()
            .Read<LifecycleCompleteChapter>()
            .Finale(ctx => ctx.Output);
}
