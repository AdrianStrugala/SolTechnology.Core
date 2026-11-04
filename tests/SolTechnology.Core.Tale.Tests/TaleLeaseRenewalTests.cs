using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Builder;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class TaleLeaseRenewalTests
{
    private static readonly TimeSpan Lease = new TaleOptions().RunningLeaseTimeout;
    private static readonly TimeSpan Beat = Lease / 3;
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    private TimerCountingTimeProvider _clock = null!;
    private InMemoryTaleRepository _store = null!;
    private LongChapterGate _gate = null!;
    private RecordingLoggerProvider _logs = null!;
    private ServiceProvider _sp = null!;
    private TaleManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new TimerCountingTimeProvider(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
        _store = new InMemoryTaleRepository();
        _gate = new LongChapterGate();
        _logs = new RecordingLoggerProvider();
        _sp = BuildHost(services => services.AddSingleton<ITaleRepository>(_store));
        _manager = _sp.GetRequiredService<TaleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        _gate.Release();
        _sp.Dispose();
        _logs.Dispose();
    }

    [Test]
    public async Task StartStory_ScopedRepository_RenewsOutsideTheChapterScope()
    {
        // Arrange
        var probe = new RenewalScopeProbe();
        await using var sp = BuildHost(
            services => services.AddSingleton(probe),
            tale => tale.UseTaleRepository<ScopedRecordingRepository>(ServiceLifetime.Scoped));
        await using var scope = sp.CreateAsyncScope();
        var run = scope.ServiceProvider.GetRequiredService<TaleManager>()
            .StartStory<ScopedRepositoryTale, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        await _gate.Entered.WaitAsync(SignalTimeout);
        (await probe.Saved.WaitAsync(SignalTimeout)).Should().BeTrue("the claim saved before the chapter started");
        var claim = probe.Saves.Single();

        // Act
        for (var beat = 0; beat < 2; beat++)
        {
            _clock.Advance(Beat);
            (await probe.Saved.WaitAsync(SignalTimeout)).Should().BeTrue("the heartbeat renews the lease once per beat");
        }

        var renewals = probe.Saves.Skip(1).ToList();
        _gate.Release();
        var result = await run.WaitAsync(SignalTimeout);

        // Assert
        result.Data!.Status.Should().Be(TaleStatus.Completed);
        claim.Should().BeSameAs(probe.ChapterRepository);
        renewals.Should().HaveCount(2).And.OnlyHaveUniqueItems().And.NotContain(claim);
        probe.Disposed.Should().Contain(renewals);
    }

    [Test]
    public async Task StartStory_ChapterRunsThreeLeaseTimeouts_WarnsOnceThatItMayHang()
    {
        // Arrange
        var run = StartHeartbeatTale();
        var taleId = await _gate.Entered.WaitAsync(SignalTimeout);
        for (var beat = 0; beat < 8; beat++)
        {
            _clock.Advance(Beat);
        }

        var warningsBeforeThreeLeases = HungChapterWarnings().Count;

        // Act
        for (var beat = 0; beat < 6; beat++)
        {
            _clock.Advance(Beat);
        }

        // Assert
        warningsBeforeThreeLeases.Should().Be(0);
        var warning = HungChapterWarnings().Should().ContainSingle().Subject;
        warning.Message.Should().Be(
            $"Tale [{taleId}] chapter [LongRunningChapter] has been running for [00:15:00], three Running lease " +
            "timeouts; the lease proves the host is alive, not that the chapter progresses, so recover a hung chapter with CancelStory");
        warning.Properties.Should().Contain(new KeyValuePair<string, object?>("Elapsed", Lease * 3));
        _gate.Release();
        (await run.WaitAsync(SignalTimeout)).Data!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task StartStory_WithIdempotencyKeyAnotherRunTookOver_ReturnsNewOwnersState()
    {
        // Arrange
        var run = StartHeartbeatTale("order-42");
        var taleId = await _gate.Entered.WaitAsync(SignalTimeout);
        var newOwner = (await _store.FindById(taleId, CancellationToken.None))!;
        newOwner.LeaseExpiresAt = _clock.GetUtcNow() + Lease;
        (await _store.Save(newOwner, CancellationToken.None)).IsSuccess.Should().BeTrue();

        // Act
        _clock.Advance(Beat);
        var result = await run.WaitAsync(SignalTimeout);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(TaleStatus.Running);
        result.Data.Version.Should().Be(newOwner.Version);
        result.Data.LeaseExpiresAt.Should().Be(newOwner.LeaseExpiresAt);
        result.Data.IdempotencyKey.Should().Be("order-42");
        _gate.Token.IsCancellationRequested.Should().BeTrue();
    }

    private Task<Result<TaleInstance>> StartHeartbeatTale(string? idempotencyKey = null) =>
        _manager.StartStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 }, idempotencyKey);

    private List<RecordedLog> HungChapterWarnings() =>
        _logs.Logs.Where(log => log.Level == LogLevel.Warning && log.Message.Contains("has been running for")).ToList();

    private ServiceProvider BuildHost(Action<IServiceCollection> configure, Action<ITaleBuilder>? useRepository = null)
    {
        var services = new ServiceCollection().AddLogging(logging => logging.AddProvider(_logs));
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(_gate);
        configure(services);
        var tale = services.AddSolTale(assemblies: typeof(TaleLeaseRenewalTests).Assembly);
        useRepository?.Invoke(tale);
        return services.BuildServiceProvider();
    }
}

/// <summary>Records which <see cref="ScopedRecordingRepository"/> instance saved, which were disposed, and the chapter's.</summary>
public sealed class RenewalScopeProbe
{
    public InMemoryTaleRepository Store { get; } = new();

    public ConcurrentQueue<ScopedRecordingRepository> Saves { get; } = new();

    public ConcurrentQueue<ScopedRecordingRepository> Disposed { get; } = new();

    public SemaphoreSlim Saved { get; } = new(0);

    public ITaleRepository? ChapterRepository { get; set; }
}

/// <summary>A scoped repository, like one over a shared <c>DbContext</c>, that reports every save and its disposal.</summary>
public sealed class ScopedRecordingRepository(RenewalScopeProbe probe) : ITaleRepository, IDisposable
{
    public Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken) =>
        probe.Store.FindById(taleId, cancellationToken);

    public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken) =>
        probe.Store.FindByIdempotencyKey(idempotencyKey, cancellationToken);

    public async Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken)
    {
        probe.Saves.Enqueue(this);
        var saved = await probe.Store.Save(taleInstance, cancellationToken);
        probe.Saved.Release();
        return saved;
    }

    public Task Delete(Auid taleId, CancellationToken cancellationToken) => probe.Store.Delete(taleId, cancellationToken);

    public void Dispose() => probe.Disposed.Enqueue(this);
}

public class ScopeProbeChapter(ITaleRepository repository, RenewalScopeProbe probe, LongChapterGate gate) : Chapter<LifecycleContext>
{
    public override async Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken)
    {
        probe.ChapterRepository = repository;
        await gate.Hold(ctx.TaleInstanceId, cancellationToken);
        return Result.Success();
    }
}

public class ScopedRepositoryTale(IServiceProvider sp, ILogger<ScopedRepositoryTale> log)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, log)
{
    protected override Tale<LifecycleOutput> Tell() =>
        Open<ScopeProbeChapter>()
            .Read<LifecycleCompleteChapter>()
            .Finale(ctx => ctx.Output);
}
