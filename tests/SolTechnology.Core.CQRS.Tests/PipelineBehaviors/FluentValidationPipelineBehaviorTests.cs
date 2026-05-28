using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS.Tests.PipelineBehaviors;

[TestFixture]
public class FluentValidationPipelineBehaviorTests
{
    private IMediator _sut = null!;
    private ServiceProvider _sp = null!;

    [SetUp]
    public void Setup()
    {
        ValidatedCommandHandler.WasCalled = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(assemblies: typeof(FluentValidationPipelineBehaviorTests).Assembly);
        _sp = services.BuildServiceProvider();
        _sut = _sp.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown() => _sp?.Dispose();

    [Test]
    public async Task Handle_WhenValidationFails_ReturnsResultFailWithValidationError()
    {
        // Arrange
        var command = new ValidatedCommand { Name = "" }; // empty — violates NotEmpty

        // Act
        var result = await _sut.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error!;
        validationError.Errors.Should().ContainKey("Name");
    }

    [Test]
    public async Task Handle_WhenValidationFails_HandlerIsNotInvoked()
    {
        // Arrange
        var command = new ValidatedCommand { Name = "" };

        // Act
        await _sut.Send(command);

        // Assert
        ValidatedCommandHandler.WasCalled.Should().BeFalse();
    }

    [Test]
    public async Task Handle_WhenValidationPasses_HandlerIsInvoked()
    {
        // Arrange
        var command = new ValidatedCommand { Name = "OK" };

        // Act
        var result = await _sut.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ValidatedCommandHandler.WasCalled.Should().BeTrue();
    }
}

