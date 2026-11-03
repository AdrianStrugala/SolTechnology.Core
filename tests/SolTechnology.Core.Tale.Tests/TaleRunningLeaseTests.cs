using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
    private GatedTaleRepository _repository = null!;
    private ServiceProvider _sp = null!;
    private TaleManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new GatedTaleRepository(new InMemoryTaleRepository());
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<ITaleRepository>(_repository);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(TaleRunningLeaseTests).Assembly);
        _sp = services.BuildServiceProvider();
        _manager = _sp.GetRequiredService<TaleManager>();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

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

    // Writes what a run's claim save stores before its host dies: status Running, stamped now.
    private async Task<Auid> LeaveRunningByCrashedHost()
    {
        var start = await _manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var stored = (await _repository.FindById(start.Data!.TaleId, CancellationToken.None))!;
        stored.Status = TaleStatus.Running;
        stored.CurrentChapter = null;
        stored.LastUpdatedAt = _clock.GetUtcNow().UtcDateTime;
        (await _repository.Save(stored, CancellationToken.None)).IsSuccess.Should().BeTrue();
        return stored.TaleId;
    }

    private Task<Result<TaleInstance>> Resume(Auid taleId) =>
        _manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(taleId, Payload);
}
