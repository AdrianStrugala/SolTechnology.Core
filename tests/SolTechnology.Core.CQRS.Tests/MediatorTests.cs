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
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(MediatorTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(MediatorTests).Assembly)
            .RegisterEventsFromAssembly(typeof(MediatorTests).Assembly));
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

    private static IEnumerable<TestCaseData> UnregisteredRequests()
    {
        yield return new TestCaseData(
            (Func<IMediator, Task>)(mediator => mediator.Send(new UnhandledCommand())),
            nameof(UnhandledCommand),
            nameof(CQRSOptions.RegisterCommandsFromAssembly)).SetArgDisplayNames("command");
        yield return new TestCaseData(
            (Func<IMediator, Task>)(mediator => mediator.Send<string>(new InternalHandlerQuery())),
            nameof(InternalHandlerQuery),
            nameof(CQRSOptions.RegisterQueriesFromAssembly)).SetArgDisplayNames("query with internal handler");
    }

    [TestCaseSource(nameof(UnregisteredRequests))]
    public async Task Send_UnregisteredHandler_ThrowsInvalidOperationWithRequestName(
        Func<IMediator, Task> send, string requestName, string registrationHint)
    {
        // Act
        var act = () => send(_sut);

        // Assert
        var exception = (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which;
        exception.Message.Should().Contain(requestName).And.Contain(registrationHint);
    }

    [Test]
    public async Task Send_BehaviorWithExplicitInterfaceImpl_IsInvoked()
    {
        // Arrange
        var log = new BehaviorLog();
        await using var provider = BuildProvider(services => services
            .AddSingleton(log)
            .AddTransient<IPipelineBehavior<TestCommand, Result>, ExplicitInterfaceBehavior>());
        var sut = provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await sut.Send(new TestCommand());

        // Assert
        result.ShouldBeSuccess();
        log.Entries.Should().Equal("explicit");
    }

    [Test]
    public async Task Send_HandlerThrowsSynchronously_ExceptionIsNotWrapped()
    {
        // Act
        var act = () => _sut.Send(new SyncThrowingCommand());

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("handler threw synchronously");
    }

    [Test]
    public async Task Send_BehaviorThrowsSynchronously_ExceptionIsNotWrapped()
    {
        // Arrange
        await using var provider = BuildProvider(services => services
            .AddTransient<IPipelineBehavior<TestCommand, Result>, SyncThrowingBehavior>());
        var sut = provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var act = () => sut.Send(new TestCommand());

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("behavior threw synchronously");
    }

    [Test]
    public async Task Send_TypeImplementingQueryAndCommand_DispatchesBoth()
    {
        // Arrange
        var request = new QueryAndCommandRequest();

        // Act
        var queryResult = await _sut.Send((IQuery<string>)request);
        var commandResult = await _sut.Send((ICommand<string>)request);

        // Assert
        queryResult.ShouldBeSuccess().Should().Be("query");
        commandResult.ShouldBeSuccess().Should().Be("command");
    }

    [Test]
    public async Task Send_BehaviorOrder_IsRegistrationOrder()
    {
        // Arrange
        var log = new BehaviorLog();
        await using var provider = BuildProvider(services => services
            .AddSingleton(log)
            .AddTransient<IPipelineBehavior<TestCommand, Result>, FirstRecordingBehavior>()
            .AddTransient<IPipelineBehavior<TestCommand, Result>, SecondRecordingBehavior>());
        var sut = provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await sut.Send(new TestCommand());

        // Assert
        log.Entries.Should().Equal("first:before", "second:before", "second:after", "first:after");
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> registerExtras)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(MediatorTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(MediatorTests).Assembly));
        registerExtras(services);
        return services.BuildServiceProvider();
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

