    using DreamTravel.Queries.CalculateBestPath.Chapters;
    using Microsoft.Extensions.Logging;
    using SolTechnology.Core.CQRS;
    using SolTechnology.Core.Story;

    namespace DreamTravel.Queries.CalculateBestPath;

    public class CalculateBestPathStory(
        IServiceProvider serviceProvider,
        ILogger<CalculateBestPathStory> logger)
        : StoryHandler<CalculateBestPathQuery, CalculateBestPathContext, CalculateBestPathResult>(serviceProvider, logger),
          IQueryHandler<CalculateBestPathQuery, CalculateBestPathResult>
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
