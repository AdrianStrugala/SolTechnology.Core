using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class TaleRunningLeaseTests
{
    private static readonly TimeSpan Lease = new TaleOptions().RunningLeaseTimeout;
    private static readonly JsonElement Payload = JsonSerializer.SerializeToElement(new PausePayload { Token = "x" });
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
    private InMemoryTaleRepository _store = null!;
    private GatedTaleRepository _repository = null!;
    private RecordingLoggerProvider _logs = null!;
    private ServiceProvider _sp = null!;
    private TaleManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryTaleRepository();
        _repository = new GatedTaleRepository(_store);
        _logs = new RecordingLoggerProvider();
        var services = new ServiceCollection().AddLogging(logging => logging.AddProvider(_logs));
        services.AddSingleton<ITaleRepository>(_repository);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(TaleRunningLeaseTests).Assembly);
        _sp = services.BuildServiceProvider();
        _manager = _sp.GetRequiredService<TaleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        _sp.Dispose();
        _logs.Dispose();
    }

    [Test]
    public async Task Resume_RunningWithinLease_ReturnsConflict()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost();
        _clock.Advance(Lease - TimeSpan.FromSeconds(1));

        // Act
        var resume = await Resume(taleId);

        // Assert
        resume.Error.Should().BeOfType<ConflictError>().Which.Message.Should().Be("Tale is already running");
        (await _repository.FindById(taleId, CancellationToken.None))!.Status.Should().Be(TaleStatus.Running);
    }

    [Test]
    public async Task Resume_RunningAfterLease_TakesOverAndCompletes()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost();
        _clock.Advance(Lease);

        // Act
        var resume = await Resume(taleId);

        // Assert
        resume.IsSuccess.Should().BeTrue();
        resume.Data!.Status.Should().Be(TaleStatus.Completed);
        var context = JsonSerializer.Deserialize<LifecycleContext>(resume.Data.Context, TaleJsonOptions.Default)!;
        context.Token.Should().Be("x");
        context.Output.Message.Should().Be("done");
    }

    [Test]
    public async Task Resume_TwoConcurrentTakeovers_ExactlyOneSucceeds()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost();
        _clock.Advance(Lease);
        _repository.GateReads(2);

        // Act
        var results = await Task.WhenAll(Resume(taleId), Resume(taleId));

        // Assert
        results.Should().ContainSingle(r => r.IsSuccess).Which.Data!.Status.Should().Be(TaleStatus.Completed);
        results.Should().ContainSingle(r => r.IsFailure).Which.Error.Should().BeOfType<ConflictError>();
        (await _repository.FindById(taleId, CancellationToken.None))!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task Resume_RunningAfterLease_LogsLeaseExpiryAndNow()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost();
        var expiry = _clock.GetUtcNow() + Lease;
        _clock.Advance(Lease + TimeSpan.FromSeconds(1));

        // Act
        await Resume(taleId);

        // Assert
        var takeover = _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Warning).Subject;
        takeover.Message.Should().Be(FormattableString.Invariant(
            $"Tale [{taleId}] Running lease expired at [{expiry}] (now [{_clock.GetUtcNow()}]); taking it over"));
        takeover.Properties.Should().Contain(new KeyValuePair<string, object?>("LeaseExpiresAt", expiry))
            .And.Contain(new KeyValuePair<string, object?>("Now", _clock.GetUtcNow()));
    }

    [Test]
    public async Task Resume_RunningWithoutLeaseExpiry_TreatsLeaseAsExpired()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost(leaseExpiresAt: null);

        // Act
        var resume = await Resume(taleId);

        // Assert
        resume.IsSuccess.Should().BeTrue();
        resume.Data!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task Resume_LeaseExpiryAheadButLastUpdatedAtOld_ReturnsConflict()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost(
            leaseExpiresAt: _clock.GetUtcNow() + TimeSpan.FromMinutes(1),
            lastUpdatedAt: _clock.GetUtcNow().UtcDateTime - TimeSpan.FromDays(1));

        // Act
        var resume = await Resume(taleId);

        // Assert
        resume.Error.Should().BeOfType<ConflictError>().Which.Message.Should().Be("Tale is already running");
    }

    [Test]
    public async Task Resume_LeaseWrittenByHostWithLongerTimeout_ShorterTimeoutHostRespectsIt()
    {
        // Arrange
        var hostAGate = new LongChapterGate();
        await using var hostA = BuildHost(hostAGate, TimeSpan.FromMinutes(5));
        var hostBGate = new LongChapterGate();
        hostBGate.Release();
        await using var hostB = BuildHost(hostBGate, TimeSpan.FromMinutes(1));
        var run = hostA.GetRequiredService<TaleManager>()
            .StartStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        var taleId = await hostAGate.Entered.WaitAsync(SignalTimeout);
        var claimedAt = _clock.GetUtcNow();
        _clock.Advance(TimeSpan.FromSeconds(90));

        // Act
        var takeover = await hostB.GetRequiredService<TaleManager>()
            .ResumeStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(taleId)
            .WaitAsync(SignalTimeout);

        // Assert
        takeover.Error.Should().BeOfType<ConflictError>().Which.Message.Should().Be("Tale is already running");
        (await _store.FindById(taleId, CancellationToken.None))!.LeaseExpiresAt
            .Should().Be(claimedAt + TimeSpan.FromMinutes(5));
        hostAGate.Release();
        (await run.WaitAsync(SignalTimeout)).Data!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task StartAndResume_EverySave_StoresLeaseExpiryOnlyWhileRunning()
    {
        // Arrange
        var saves = new List<(TaleStatus Status, DateTimeOffset? LeaseExpiresAt)>();
        _repository.AfterSave = (instance, _) => saves.Add((instance.Status, instance.LeaseExpiresAt));
        var expiry = _clock.GetUtcNow() + Lease;

        // Act
        var start = await _manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        await Resume(start.Data!.TaleId);

        // Assert
        saves.Should().Equal(
            (TaleStatus.Running, expiry),
            (TaleStatus.Running, expiry),
            (TaleStatus.WaitingForInput, null),
            (TaleStatus.WaitingForInput, null),
            (TaleStatus.Running, expiry),
            (TaleStatus.Running, expiry),
            (TaleStatus.Running, expiry),
            (TaleStatus.Completed, null));
        (await _store.FindById(start.Data.TaleId, CancellationToken.None))!.LeaseExpiresAt.Should().BeNull();
    }

    [Test]
    public async Task CancelStory_RunningTale_ClearsLeaseExpiry()
    {
        // Arrange
        var taleId = await LeaveRunningByCrashedHost();

        // Act
        var cancel = await _manager.CancelStory(taleId);

        // Assert
        cancel.IsSuccess.Should().BeTrue();
        var stored = (await _store.FindById(taleId, CancellationToken.None))!;
        stored.Status.Should().Be(TaleStatus.Cancelled);
        stored.LeaseExpiresAt.Should().BeNull();
    }

    [TestCase(999)]
    [TestCase(49L * 24 * 60 * 60 * 1000 + 1)]
    [TestCase(150L * 24 * 60 * 60 * 1000)]
    [TestCase(long.MaxValue)]
    public void RunningLeaseTimeout_OutOfRange_Throws(long milliseconds)
    {
        // Arrange
        var options = new TaleOptions();
        var timeout = milliseconds == long.MaxValue ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(milliseconds);

        // Act
        var act = () => options.RunningLeaseTimeout = timeout;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
        options.RunningLeaseTimeout.Should().Be(Lease);
    }

    [Test]
    public void RunningLeaseTimeout_AtBounds_IsAcceptedAndFitsTheSystemTimer()
    {
        // Arrange
        var options = new TaleOptions();
        var longest = TimeSpan.FromDays(49);

        // Act
        options.RunningLeaseTimeout = TimeSpan.FromSeconds(1);
        options.RunningLeaseTimeout = longest;
        using var timer = TimeProvider.System.CreateTimer(_ => { }, null, longest, longest);

        // Assert
        options.RunningLeaseTimeout.Should().Be(longest);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void RunningLeaseTimeout_NotPositive_Throws(int seconds)
    {
        // Arrange
        var options = new TaleOptions();

        // Act
        var act = () => options.RunningLeaseTimeout = TimeSpan.FromSeconds(seconds);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // Writes what a run's claim save stores before its host dies: status Running, lease from now.
    private Task<Auid> LeaveRunningByCrashedHost() =>
        LeaveRunningByCrashedHost(_clock.GetUtcNow() + Lease);

    private async Task<Auid> LeaveRunningByCrashedHost(DateTimeOffset? leaseExpiresAt, DateTime? lastUpdatedAt = null)
    {
        var start = await _manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var stored = (await _repository.FindById(start.Data!.TaleId, CancellationToken.None))!;
        stored.Status = TaleStatus.Running;
        stored.CurrentChapter = null;
        stored.LastUpdatedAt = lastUpdatedAt ?? _clock.GetUtcNow().UtcDateTime;
        stored.LeaseExpiresAt = leaseExpiresAt;
        (await _repository.Save(stored, CancellationToken.None)).IsSuccess.Should().BeTrue();
        return stored.TaleId;
    }

    // A second host sharing the store and the clock, with its own options.
    private ServiceProvider BuildHost(LongChapterGate gate, TimeSpan runningLeaseTimeout)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<ITaleRepository>(_store);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton<TestService>();
        services.AddSingleton(gate);
        services.AddSolTale(o => o.RunningLeaseTimeout = runningLeaseTimeout, [typeof(TaleRunningLeaseTests).Assembly]);
        return services.BuildServiceProvider();
    }

    private Task<Result<TaleInstance>> Resume(Auid taleId) =>
        _manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(taleId, Payload);
}
