using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Api;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Tests for <see cref="TaleController"/> — whitelist, HTTP semantics, end-to-end via
/// in-memory persistence. Uses a minimal concrete subclass to exercise the abstract base.
/// </summary>
[TestFixture]
public class TaleControllerTests
{
    private IServiceProvider _sp = null!;
    private TestTaleController _controller = null!;
    private TaleHandlerRegistry _registry = null!;
    private InMemoryTaleRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        _repo = new InMemoryTaleRepository();
        var options = new TaleOptions();
        services.AddSingleton(options);
        services.AddSingleton<ITaleRepository>(_repo);
        services.AddScoped<TaleManager>();

        // Register chapters + handler + registry
        services.AddTransient<LifecyclePassingChapter>();
        services.AddTransient<LifecyclePauseChapter>();
        services.AddTransient<LifecycleCompleteChapter>();
        services.AddTransient<LifecycleTaleV1>();

        _registry = new TaleHandlerRegistry();
        _registry.Register(typeof(LifecycleTaleV1));
        _registry.Register(typeof(ThrowingConstructorTale));
        services.AddSingleton(_registry);

        _sp = services.BuildServiceProvider();

        var manager = _sp.GetRequiredService<TaleManager>();
        _controller = new TestTaleController(manager, _registry, options, NullLogger<TaleController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-42" } }
        };
    }

    [TearDown]
    public void TearDown() => (_sp as IDisposable)?.Dispose();

    [Test]
    public async Task StartStory_UnknownHandler_ShouldReturn404()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });

        var result = await _controller.StartStory("UnknownStory", input);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task StartStory_ValidHandler_PausesAt_202()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });

        var result = await _controller.StartStory(nameof(LifecycleTaleV1), input);

        result.Should().BeOfType<AcceptedResult>();
    }

    [Test]
    public async Task InvalidTaleId_ShouldReturn400()
    {
        var result = await _controller.GetStoryState("not-a-valid-auid");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Resume_And_Get_Lifecycle_EndToEnd()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });
        var start = await _controller.StartStory(nameof(LifecycleTaleV1), input);
        var startPayload = (start as AcceptedResult)!.Value;

        // Extract TaleId via reflection on the Result<TaleInstanceDto> shape.
        var dto = (TaleInstanceDto)startPayload!.GetType().GetProperty("Data")!.GetValue(startPayload)!;
        var taleId = dto.TaleId;

        // GetStoryState should return 200 with WaitingForInput.
        var state = await _controller.GetStoryState(taleId);
        state.Should().BeOfType<OkObjectResult>();

        // Resume with valid payload.
        var payload = JsonSerializer.SerializeToElement(new PausePayload { Token = "ABC" });
        var resume = await _controller.ResumeStory(taleId, payload);
        resume.Should().BeOfType<OkObjectResult>();

        // Get result.
        var resultResp = await _controller.GetStoryResult(taleId);
        resultResp.Should().BeOfType<OkObjectResult>();

        var resultDto = (TaleResultDto)((OkObjectResult)resultResp).Value!;
        resultDto.Status.Should().Be(TaleStatus.Completed);
        resultDto.Output.Should().NotBeNull();
    }

    [Test]
    public async Task Cancel_ShouldReturn200_AndFlipStatus()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });
        var start = await _controller.StartStory(nameof(LifecycleTaleV1), input);
        var dto = (TaleInstanceDto)((AcceptedResult)start).Value!.GetType().GetProperty("Data")!.GetValue(((AcceptedResult)start).Value!)!;

        var cancel = await _controller.CancelStory(dto.TaleId);
        cancel.Should().BeOfType<OkObjectResult>();

        var refreshed = await _controller.GetStoryState(dto.TaleId);
        var refreshedDto = (TaleInstanceDto)((OkObjectResult)refreshed).Value!.GetType().GetProperty("Data")!.GetValue(((OkObjectResult)refreshed).Value!)!;
        refreshedDto.Status.Should().Be(TaleStatus.Cancelled);
    }

    [Test]
    public async Task GetResult_NotCompleted_ShouldReturn400()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });
        var start = await _controller.StartStory(nameof(LifecycleTaleV1), input);
        var dto = (TaleInstanceDto)((AcceptedResult)start).Value!.GetType().GetProperty("Data")!.GetValue(((AcceptedResult)start).Value!)!;

        var result = await _controller.GetStoryResult(dto.TaleId);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Start_CamelCaseInput_Binds()
    {
        // Arrange
        using var input = JsonDocument.Parse("""{"value": 7}""");

        // Act
        var start = await _controller.StartStory(nameof(LifecycleTaleV1), input.RootElement);

        // Assert
        var dto = DataOf<TaleInstanceDto>(start.Should().BeOfType<AcceptedResult>().Subject);
        var stored = await _repo.FindById(Auid.Parse(dto.TaleId));
        JsonSerializer.Deserialize<LifecycleContext>(stored!.Context, TaleJsonOptions.Default)!.Input.Value.Should().Be(7);
    }

    [Test]
    public async Task Resume_InvalidPayload_PausesForRetry_AndNoStackTraceInGet()
    {
        // Arrange
        var start = await _controller.StartStory(nameof(LifecycleTaleV1), JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 }));
        var taleId = DataOf<TaleInstanceDto>((AcceptedResult)start).TaleId;
        using var invalid = JsonDocument.Parse("""{"token": 123}""");

        // Act
        var resume = await _controller.ResumeStory(taleId, invalid.RootElement);
        var state = await _controller.GetStoryState(taleId);

        // Assert
        resume.Should().BeOfType<AcceptedResult>();
        var dto = DataOf<TaleInstanceDto>(state.Should().BeOfType<OkObjectResult>().Subject);
        dto.Status.Should().Be(TaleStatus.WaitingForInput);
        dto.CurrentChapter!.Error.Should().BeOfType<ValidationError>();
        JsonSerializer.Serialize(((OkObjectResult)state).Value).Should().NotContain("   at ");
    }

    [Test]
    public async Task Start_HandlerThrows_BodyHasNoExceptionMessage()
    {
        // Act
        var start = await _controller.StartStory(nameof(ThrowingConstructorTale), JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 }));

        // Assert
        var body = start.Should().BeOfType<BadRequestObjectResult>().Subject.Value;
        JsonSerializer.Serialize(body).Should().NotContain(ThrowingConstructorTale.Secret);
    }

    [Test]
    public async Task Start_RepositoryThrows_500BodyHasNoExceptionMessage_AndCarriesCorrelationId()
    {
        // Arrange
        var repository = Substitute.For<ITaleRepository>();
        repository.FindByIdempotencyKey(Arg.Any<string>())
            .Returns<Task<TaleInstance?>>(_ => throw new InvalidOperationException(ThrowingConstructorTale.Secret));
        var manager = new TaleManager(_sp.GetRequiredService<IServiceScopeFactory>(), repository, NullLogger<TaleManager>.Instance);
        var controller = new TestTaleController(manager, _registry, new TaleOptions(), NullLogger<TaleController>.Instance)
        {
            ControllerContext = _controller.ControllerContext
        };

        // Act
        var start = await controller.StartStory(nameof(LifecycleTaleV1), JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 }), "idem-1");

        // Assert
        var objectResult = start.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var error = objectResult.Value.Should().BeAssignableTo<Result>().Subject.Error!;
        error.Message.Should().NotContain(ThrowingConstructorTale.Secret);
        error.CorrelationId.Should().Be("trace-42");
    }

    private static T DataOf<T>(ObjectResult result) =>
        (T)result.Value!.GetType().GetProperty("Data")!.GetValue(result.Value)!;

    public sealed class ThrowingConstructorTale : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>
    {
        public const string Secret = "secret-connection-string";

        public ThrowingConstructorTale(IServiceProvider sp, ILogger<ThrowingConstructorTale> logger)
            : base(sp, logger) => throw new InvalidOperationException(Secret);

        protected override Tale<LifecycleOutput> Tell() => Open<LifecyclePassingChapter>().Finale(ctx => ctx.Output);
    }

    [Test]
    public void ToDataFields_SelfReferencingType_Terminates()
    {
        // Arrange
        var chapter = new SelfReferencingInputChapter();

        // Act
        var schema = chapter.GetRequiredInputSchema();

        // Assert
        var node = schema.Should().ContainSingle(f => f.Name == nameof(SelfReferencingInput.Node)).Subject;
        node.IsComplex.Should().BeTrue();
        node.Children.Should().ContainSingle(f => f.Name == nameof(Node.Parent))
            .Which.Children.Should().BeEmpty("a type already on the path is not expanded again");
        schema.Should().ContainSingle(f => f.Name == nameof(SelfReferencingInput.Culture))
            .Which.Children.Should().NotBeEmpty();
    }

    public sealed class Node
    {
        public Node? Parent { get; set; }
    }

    public sealed class SelfReferencingInput
    {
        public Node? Node { get; set; }

        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
    }

    public sealed class SelfReferencingInputChapter : InteractiveChapter<LifecycleContext, SelfReferencingInput>
    {
        public override Task<Result> ReadWithInput(LifecycleContext context, SelfReferencingInput userInput, CancellationToken cancellationToken) =>
            Result.SuccessAsTask();
    }

    private sealed class TestTaleController : TaleController
    {
        public TestTaleController(
            TaleManager manager,
            TaleHandlerRegistry registry,
            TaleOptions options,
            ILogger<TaleController> logger)
            : base(manager, registry, options, logger) { }
    }
}

