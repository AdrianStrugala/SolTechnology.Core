using DreamTravel.Trips.Queries.CalculateBestPath.Executors;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathHandler : PipelineHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>
{
    public CalculateBestPathHandler(IServiceProvider serviceProvider) : base(serviceProvider) { }
    
    protected override void RegisterSteps()
    {
        Step<DownloadRoadData>();
        Step<FindProfitablePath>();
        Step<SolveTsp>();
        Step<FormCalculateBestPathResult>();
    }
}
