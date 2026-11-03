using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class TaleTerminalStatusTests
{
    [Test]
    public async Task Start_TokenCancelledDuringSecondSave_StoresCancelled()
    {
        // Arrange
        var repository = new GatedTaleRepository(new InMemoryTaleRepository());
        await using var sp = Build(repository);
        using var cts = new CancellationTokenSource();
        var saves = 0;
        repository.BeforeSave = async _ =>
        {
            if (Interlocked.Increment(ref saves) == 2)
            {
                await cts.CancelAsync();
            }
        };

        // Act
        var start = await sp.GetRequiredService<TaleManager>()
            .StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
                new LifecycleInput { Value = 1 }, cancellationToken: cts.Token);

        // Assert
        start.Error.Should().BeOfType<TaleCancelledError>();
        (await repository.List()).Should().ContainSingle().Which.Status.Should().Be(TaleStatus.Cancelled);
    }

    [Test]
    public async Task Start_InlineStepThrows_StoresFailed()
    {
        // Arrange
        var repository = new GatedTaleRepository(new InMemoryTaleRepository());
        await using var sp = Build(repository);

        // Act
        var start = await sp.GetRequiredService<TaleManager>()
            .StartStory<ThrowingInlineStepTale, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });

        // Assert
        start.IsFailure.Should().BeTrue();
        start.Error!.Message.Should().NotContain(ThrowingInlineStepTale.Secret);
        var stored = (await repository.List()).Should().ContainSingle().Subject;
        stored.Status.Should().Be(TaleStatus.Failed);
        stored.History.Should().NotContain(h => h.ChapterId == nameof(LifecycleCompleteChapter));
    }

    private static ServiceProvider Build(ITaleRepository repository)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(repository);
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(TaleTerminalStatusTests).Assembly);
        return services.BuildServiceProvider();
    }
}

public class ThrowingInlineStepTale(IServiceProvider sp, ILogger<ThrowingInlineStepTale> logger)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, logger)
{
    public const string Secret = "inline-step-secret";

    protected override Tale<LifecycleOutput> Tell() =>
        Open<LifecyclePassingChapter>()
            .Do((Action<LifecycleContext>)(_ => throw new InvalidOperationException(Secret)))
            .Read<LifecycleCompleteChapter>()
            .Finale(ctx => ctx.Output);
}
