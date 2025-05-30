using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.Persistence;

namespace DreamTravel.Api.Controllers.RoadPlanner;

public class JourneyController(
    JourneyManager journeyManager, 
    ILogger<SolTechnology.Core.Journey.Controllers.JourneyController> logger,
    IJourneyInstanceRepository journeyRepository,
    IEnumerable<IJourneyHandler> journeyHandlers) 
    : SolTechnology.Core.Journey.Controllers.JourneyController(journeyManager, logger, journeyRepository, journeyHandlers);
