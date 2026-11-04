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

/// <summary>
/// How the library defends itself against an <see cref="ITaleRepository"/> that does not store
/// <see cref="TaleInstance.LeaseExpiresAt"/> as written: the missing-expiry fallback and the read-back check.
/// </summary>
[TestFixture]
public class TaleLeaseRepositoryContractTests
{
    private static readonly TimeSpan Lease = new TaleOptions().RunningLeaseTimeout;
    private static readonly JsonElement Payload = JsonSerializer.SerializeToElement(new PausePayload { Token = "x" });
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly InMemoryTaleRepository _store = new();
    private readonly LongChapterGate _gate = new();
    private RecordingLoggerProvider _logs = null!;
    private ServiceProvider? _sp;

    [SetUp]
    public void SetUp() => _logs = new RecordingLoggerProvider();

    [TearDown]
    public void TearDown()
    {
        _sp?.Dispose();
        _logs.Dispose();
    }

    [Test]
    public async Task Resume_RunningWithoutLeaseExpirySavedWithinTimeout_ReturnsConflictAndWarns()
    {
        // Arrange
        var manager = Manager(_store);
        var taleId = await LeaveRunningWithoutLeaseExpiry(manager);
        var fallbackExpiry = _clock.GetUtcNow() + Lease;
        _clock.Advance(Lease - TimeSpan.FromSeconds(1));

        // Act
        var resume = await manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(taleId, Payload);

        // Assert
        resume.Error.Should().BeOfType<ConflictError>().Which.Message.Should().Be("Tale is already running");
        (await _store.FindById(taleId, CancellationToken.None))!.Status.Should().Be(TaleStatus.Running);
        var warning = _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Warning).Subject;
        warning.Message.Should().Contain("without a stored LeaseExpiresAt").And.Contain("ITaleRepository.Save");
        warning.Properties.Should().Contain(new KeyValuePair<string, object?>("TaleId", taleId))
            .And.Contain(new KeyValuePair<string, object?>("FallbackLeaseExpiresAt", fallbackExpiry));
    }

    [Test]
    public async Task StartStory_RepositoryDropsLeaseExpiresAtOnUpdate_LogsRepositoryContractError()
    {
        // Arrange
        var manager = Manager(new LeaseRewritingRepository(_store, KeepStoredExpiryOnUpdate));
        var claimedExpiry = _clock.GetUtcNow() + Lease;
        var run = manager.StartStory<HeartbeatTale, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        await _gate.Entered.WaitAsync(SignalTimeout);

        // Act
        _clock.Advance(Lease / 3);
        _gate.Release();
        var result = await run.WaitAsync(SignalTimeout);

        // Assert
        result.Data!.Status.Should().Be(TaleStatus.Completed);
        var error = _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Error).Subject;
        error.Message.Should().Contain("ITaleRepository.Save must store LeaseExpiresAt unchanged");
        Property(error, "LeaseWrite").Should().Be("Renewal");
        Property(error, "WrittenLeaseExpiresAt").Should().Be(claimedExpiry + Lease / 3);
        Property(error, "StoredLeaseExpiresAt").Should().Be(claimedExpiry);
    }

    [Test]
    public async Task ResumeStory_RepositoryDropsLeaseExpiresAtOnUpdate_LogsRepositoryContractError()
    {
        // Arrange
        var manager = Manager(new LeaseRewritingRepository(_store, KeepStoredExpiryOnUpdate));
        var start = await manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var claimedExpiry = _clock.GetUtcNow() + Lease;
        _clock.Advance(TimeSpan.FromHours(1));

        // Act
        var resume = await manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            start.Data!.TaleId, Payload);

        // Assert
        resume.Data!.Status.Should().Be(TaleStatus.Completed);
        var error = _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Error).Subject;
        Property(error, "LeaseWrite").Should().Be("ClaimByUpdate");
        Property(error, "WrittenLeaseExpiresAt").Should().Be(claimedExpiry + TimeSpan.FromHours(1));
        Property(error, "StoredLeaseExpiresAt").Should().Be(claimedExpiry);
    }

    [Test]
    public async Task StartStory_RepositoryShiftsLeaseExpiresAtByLocalOffset_LogsRepositoryContractError()
    {
        // Arrange
        var manager = Manager(new LeaseRewritingRepository(_store, (written, _) => written.LeaseExpiresAt - TimeSpan.FromHours(1)));
        var claimedExpiry = _clock.GetUtcNow() + Lease;

        // Act
        await StartTwoTales(manager);

        // Assert
        var error = _logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Error).Subject;
        Property(error, "LeaseWrite").Should().Be("ClaimByInsert");
        Property(error, "WrittenLeaseExpiresAt").Should().Be(claimedExpiry);
        Property(error, "StoredLeaseExpiresAt").Should().Be(claimedExpiry - TimeSpan.FromHours(1));
    }

    [Test]
    public async Task StartStory_RepositoryRoundsLeaseExpiresAtToMilliseconds_LogsNothing()
    {
        // Arrange
        _clock.Advance(TimeSpan.FromTicks(6789));
        var manager = Manager(new LeaseRewritingRepository(_store, (written, _) => written.LeaseExpiresAt is { } lease
            ? new DateTimeOffset(lease.Ticks - lease.Ticks % TimeSpan.TicksPerMillisecond, lease.Offset)
            : null));

        // Act
        var start = await manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        // Assert
        start.Data!.Status.Should().Be(TaleStatus.Completed);
        _logs.Logs.Should().NotContain(log => log.Level >= LogLevel.Warning);
    }

    [Test]
    public async Task StartStory_ReadBackSeesNewerVersion_SkipsCheck()
    {
        // Arrange
        var manager = Manager(new LeaseRewritingRepository(_store, (written, _) => written.LeaseExpiresAt - TimeSpan.FromHours(1))
        {
            ReadsNewerVersion = true
        });

        // Act
        var start = await manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        // Assert
        start.Data!.Status.Should().Be(TaleStatus.Completed);
        _logs.Logs.Should().NotContain(log => log.Level >= LogLevel.Warning);
    }

    private static DateTimeOffset? KeepStoredExpiryOnUpdate(TaleInstance written, TaleInstance? existing) =>
        existing == null ? written.LeaseExpiresAt : existing.LeaseExpiresAt;

    private static object? Property(RecordedLog log, string name)
    {
        var value = log.Properties.Single(property => property.Key == name).Value;
        return value is Enum ? value.ToString() : value;
    }

    private static async Task StartTwoTales(TaleManager manager)
    {
        for (var tale = 0; tale < 2; tale++)
        {
            var start = await manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
                new LifecycleInput { Value = tale });
            start.Data!.Status.Should().Be(TaleStatus.Completed);
        }
    }

    // Writes what a repository without the LeaseExpiresAt column returns for a run's claim save.
    private async Task<Auid> LeaveRunningWithoutLeaseExpiry(TaleManager manager)
    {
        var start = await manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var stored = (await _store.FindById(start.Data!.TaleId, CancellationToken.None))!;
        stored.Status = TaleStatus.Running;
        stored.CurrentChapter = null;
        stored.LastUpdatedAt = _clock.GetUtcNow().UtcDateTime;
        stored.LeaseExpiresAt = null;
        (await _store.Save(stored, CancellationToken.None)).IsSuccess.Should().BeTrue();
        return stored.TaleId;
    }

    private TaleManager Manager(ITaleRepository repository)
    {
        var services = new ServiceCollection().AddLogging(logging => logging.AddProvider(_logs));
        services.AddSingleton(repository);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(_gate);
        services.AddSolTale(assemblies: typeof(TaleLeaseRepositoryContractTests).Assembly);
        _sp = services.BuildServiceProvider();
        return _sp.GetRequiredService<TaleManager>();
    }
}
