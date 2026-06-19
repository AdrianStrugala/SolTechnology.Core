using DreamTravel.Queries.CalculateBestPath.Chapters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Tale;

namespace DreamTravel.Queries.CalculateBestPath;

public class CalculateBestPathTale(
    IServiceProvider serviceProvider,
    ILogger<CalculateBestPathTale> logger)
    : StoryHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>(serviceProvider, logger),
      IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
{
    protected override Tale<CalculateBestPathResult> Tell() =>
        Open<InitiateContext>()
            .Expect(ctx => ctx.NoOfCities > 1,
                    new NotFoundError { Message = "A route needs at least two cities." })
            .Read<DownloadRoadData>()
            .Read<FindProfitablePath>()
            .Otherwise<JustOrderCities>()
            .Read<SolveTsp>()
            .WhenLost(error => logger.LogWarning("Best path calculation failed: {Error}", error.Message))
            .Read<FormCalculateBestPathResult>()
            .Finale(ctx => ctx.Output);
}

