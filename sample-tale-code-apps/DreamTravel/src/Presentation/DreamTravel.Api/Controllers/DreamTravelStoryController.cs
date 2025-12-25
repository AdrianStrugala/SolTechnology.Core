using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Story.Api;
using SolTechnology.Core.Story.Orchestration;

namespace DreamTravel.Api.Controllers;

/// <summary>
/// DreamTravel-specific Story API controller.
/// Inherits from the base StoryController provided by SolTechnology.Core.Story.
/// Can be customized to add authentication, logging, or other application-specific behavior.
/// </summary>
public class DreamTravelStoryController : StoryController
{
    public DreamTravelStoryController(
        StoryManager storyManager,
        ILogger<StoryController> logger)
        : base(storyManager, logger)
    {
    }

    // All endpoints are inherited from StoryController
    // Override methods here if you need custom behavior:

    // Example: Add authentication to StartStory
    // public override async Task<IActionResult> StartStory(string handlerTypeName, JsonElement input)
    // {
    //     // Custom authentication logic here
    //     return await base.StartStory(handlerTypeName, input);
    // }
}
