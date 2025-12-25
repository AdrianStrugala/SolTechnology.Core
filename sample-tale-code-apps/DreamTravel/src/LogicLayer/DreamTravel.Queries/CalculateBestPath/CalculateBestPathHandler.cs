using DreamTravel.Queries.CalculateBestPath.Chapters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath;

public class CalculateBestPathHandler(
    IServiceProvider serviceProvider,
    ILogger<CalculateBestPathHandler> logger)
    : StoryHandler<CalculateBestPathQuery, CalculateBestPathNarration, CalculateBestPathResult>(serviceProvider, logger)
{
    protected override async Task TellStory()
    {
        await ReadChapter<InitiateContext>();
        await ReadChapter<DownloadRoadData>();
        await ReadChapter<FindProfitablePath>();
        await ReadChapter<SolveTsp>();
        await ReadChapter<FormCalculateBestPathResult>();
    }
}
