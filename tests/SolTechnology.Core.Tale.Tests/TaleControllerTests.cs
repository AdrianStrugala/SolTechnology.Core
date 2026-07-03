using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
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

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        var repo = new InMemoryTaleRepository();
        var options = new TaleOptions();
        services.AddSingleton(options);
        services.AddSingleton<ITaleRepository>(repo);
        services.AddScoped<TaleManager>();

        // Register chapters + handler + registry
        services.AddTransient<LifecyclePassingChapter>();
        services.AddTransient<LifecyclePauseChapter>();
        services.AddTransient<LifecycleCompleteChapter>();
        services.AddTransient<LifecycleTaleV1>();

        _registry = new TaleHandlerRegistry();
        _registry.Register(typeof(LifecycleTaleV1));
        services.AddSingleton(_registry);

        _sp = services.BuildServiceProvider();

        var manager = _sp.GetRequiredService<TaleManager>();
        _controller = new TestTaleController(manager, _registry, options, NullLogger<TaleController>.Instance);
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

