using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Testing.Assertions;

namespace SolTechnology.Core.CQRS.Tests;

[TestFixture]
public class MediatorTests
{
    private IMediator _sut = null!;
    private ServiceProvider _sp = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(assemblies: typeof(MediatorTests).Assembly);
        _sp = services.BuildServiceProvider();
        _sut = _sp.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown() => _sp?.Dispose();

    [Test]
    public async Task Send_Query_ResolvesHandlerAndReturnsResult()
    {
        // Act
        var result = await _sut.Send<string>(new TestQuery { Input = "World" });

        // Assert
        result.ShouldBeSuccess().Should().Be("Hello World");
    }

    [Test]
    public async Task Send_Command_ResolvesHandlerAndReturnsSuccess()
    {
        // Act
        var result = await _sut.Send(new TestCommand());

        // Assert
        result.ShouldBeSuccess();
    }

    [Test]
    public async Task Send_CommandWithResult_ResolvesHandlerAndReturnsData()
    {
        // Act
        var result = await _sut.Send<int>(new TestCommandWithResult { Value = 21 });

        // Assert
        result.ShouldBeSuccess().Should().Be(42);
    }

    [Test]
    public async Task Send_ThreadsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => _sut.Send(new CancellationTestCommand(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

// Helper for cancellation test
public class CancellationTestCommand : ICommand;

public class CancellationTestCommandHandler : ICommandHandler<CancellationTestCommand>
{
    public Task<Result> Handle(CancellationTestCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Result.SuccessAsTask();
    }
}

