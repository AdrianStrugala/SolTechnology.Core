using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Api;
using SolTechnology.Core.Story.Orchestration;

namespace DreamTravel.Api.Controllers;

/// <summary>
/// DreamTravel-specific Story API controller. Inherits from the base <see cref="StoryController"/>.
/// </summary>
[Route("api/dreamtravel/story")]
public class DreamTravelStoryController : StoryController
{
    public DreamTravelStoryController(
        StoryManager storyManager,
        StoryHandlerRegistry registry,
        StoryOptions options,
        ILogger<StoryController> logger)
        : base(storyManager, registry, options, logger)
    {
    }
}
