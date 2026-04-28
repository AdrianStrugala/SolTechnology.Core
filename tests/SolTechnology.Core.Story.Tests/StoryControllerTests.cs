using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Api;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for <see cref="StoryController"/> — whitelist, HTTP semantics, end-to-end via
/// in-memory persistence. Uses a minimal concrete subclass to exercise the abstract base.
/// </summary>
[TestFixture]
public class StoryControllerTests
{
    private IServiceProvider _sp = null!;
    private TestStoryController _controller = null!;
    private StoryHandlerRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        var repo = new InMemoryStoryRepository();
        var options = new StoryOptions();
        services.AddSingleton(options);
        services.AddSingleton<IStoryRepository>(repo);
        services.AddScoped<StoryManager>();

        // Register chapters + handler + registry
        services.AddTransient<LifecyclePassingChapter>();
        services.AddTransient<LifecyclePauseChapter>();
        services.AddTransient<LifecycleCompleteChapter>();
        services.AddTransient<LifecycleStoryV1>();

        _registry = new StoryHandlerRegistry();
        _registry.Register(typeof(LifecycleStoryV1));
        services.AddSingleton(_registry);

        _sp = services.BuildServiceProvider();

        var manager = _sp.GetRequiredService<StoryManager>();
        _controller = new TestStoryController(manager, _registry, options, NullLogger<StoryController>.Instance);
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

        var result = await _controller.StartStory(nameof(LifecycleStoryV1), input);

        result.Should().BeOfType<AcceptedResult>();
    }

    [Test]
    public async Task InvalidStoryId_ShouldReturn400()
    {
        var result = await _controller.GetStoryState("not-a-valid-auid");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Resume_And_Get_Lifecycle_EndToEnd()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });
        var start = await _controller.StartStory(nameof(LifecycleStoryV1), input);
        var startPayload = (start as AcceptedResult)!.Value;

        // Extract StoryId via reflection on the Result<StoryInstanceDto> shape.
        var dto = (StoryInstanceDto)startPayload!.GetType().GetProperty("Data")!.GetValue(startPayload)!;
        var storyId = dto.StoryId;

        // GetStoryState should return 200 with WaitingForInput.
        var state = await _controller.GetStoryState(storyId);
        state.Should().BeOfType<OkObjectResult>();

        // Resume with valid payload.
        var payload = JsonSerializer.SerializeToElement(new PausePayload { Token = "ABC" });
        var resume = await _controller.ResumeStory(storyId, payload);
        resume.Should().BeOfType<OkObjectResult>();

        // Get result.
        var resultResp = await _controller.GetStoryResult(storyId);
        resultResp.Should().BeOfType<OkObjectResult>();

        var resultDto = (StoryResultDto)((OkObjectResult)resultResp).Value!;
        resultDto.Status.Should().Be(StoryStatus.Completed);
        resultDto.Output.Should().NotBeNull();
    }

    [Test]
    public async Task Cancel_ShouldReturn200_AndFlipStatus()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });
        var start = await _controller.StartStory(nameof(LifecycleStoryV1), input);
        var dto = (StoryInstanceDto)((AcceptedResult)start).Value!.GetType().GetProperty("Data")!.GetValue(((AcceptedResult)start).Value!)!;

        var cancel = await _controller.CancelStory(dto.StoryId);
        cancel.Should().BeOfType<OkObjectResult>();

        var refreshed = await _controller.GetStoryState(dto.StoryId);
        var refreshedDto = (StoryInstanceDto)((OkObjectResult)refreshed).Value!.GetType().GetProperty("Data")!.GetValue(((OkObjectResult)refreshed).Value!)!;
        refreshedDto.Status.Should().Be(StoryStatus.Cancelled);
    }

    [Test]
    public async Task GetResult_NotCompleted_ShouldReturn400()
    {
        var input = JsonSerializer.SerializeToElement(new LifecycleInput { Value = 1 });
        var start = await _controller.StartStory(nameof(LifecycleStoryV1), input);
        var dto = (StoryInstanceDto)((AcceptedResult)start).Value!.GetType().GetProperty("Data")!.GetValue(((AcceptedResult)start).Value!)!;

        var result = await _controller.GetStoryResult(dto.StoryId);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private sealed class TestStoryController : StoryController
    {
        public TestStoryController(
            StoryManager manager,
            StoryHandlerRegistry registry,
            StoryOptions options,
            ILogger<StoryController> logger)
            : base(manager, registry, options, logger) { }
    }
}

