using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Api;
using SolTechnology.Core.Tale.Orchestration;

namespace DreamTravel.Api.Controllers;

/// <summary>
/// DreamTravel-specific Tale API controller. Inherits from the base <see cref="TaleController"/>.
/// </summary>
[Route("api/dreamtravel/tale")]
public class DreamTravelTaleController : TaleController
{
    public DreamTravelTaleController(
        TaleManager storyManager,
        TaleHandlerRegistry registry,
        TaleOptions options,
        ILogger<TaleController> logger)
        : base(storyManager, registry, options, logger)
    {
    }
}
