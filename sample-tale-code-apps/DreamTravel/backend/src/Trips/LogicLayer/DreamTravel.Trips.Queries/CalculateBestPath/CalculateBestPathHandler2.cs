using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using SolTechnology.Core.CQRS.SuperChain;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathHandler(IServiceProvider serviceProvider)
    : ChainHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>(serviceProvider)
{
    protected override async Task HandleChain()
    {
        await Invoke<InitiateContext>();
        await Invoke<DownloadRoadData>();
        await Invoke<FindProfitablePath>();
        await Invoke<SolveTsp>();
        await Invoke<FormCalculateBestPathResult>();
    }
}
