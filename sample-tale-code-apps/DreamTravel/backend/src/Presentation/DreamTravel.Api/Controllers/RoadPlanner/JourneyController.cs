using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Persistence;

namespace DreamTravel.Api.Controllers.RoadPlanner;

public class JourneyController(
    JourneyManager journeyManager, 
    ILogger<SolTechnology.Core.Journey.Controllers.JourneyController> logger, 
    IServiceProvider serviceProvider, IJourneyInstanceRepository journeyRepository) 
    : SolTechnology.Core.Journey.Controllers.JourneyController(journeyManager, logger, serviceProvider, journeyRepository);
