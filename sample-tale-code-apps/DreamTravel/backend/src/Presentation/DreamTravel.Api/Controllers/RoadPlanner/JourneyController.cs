using SolTechnology.Core.Flow.Workflow.ChainFramework;
using SolTechnology.Core.Flow.Workflow.Persistence;

namespace DreamTravel.Api.Controllers.RoadPlanner;

public class FlowController(
    FlowManager journeyManager, 
    ILogger<SolTechnology.Core.Flow.Controllers.FlowController> logger,
    IFlowInstanceRepository journeyRepository,
    IEnumerable<IFlowHandler> journeyHandlers) 
    : SolTechnology.Core.Flow.Controllers.FlowController(journeyManager, logger, journeyRepository, journeyHandlers);
