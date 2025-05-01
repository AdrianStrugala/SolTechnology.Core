using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using DreamTravel.GraphDatabase.Repositories;
using DreamTravel.Trips.Domain.StreetGraph;

namespace DreamTravel.Trips.Commands.FetchTraffic
{
    public class FetchTrafficHandler(
        IGoogleApiClient googleClient,
        IIntersectionRepository intersectionRepo,
        IStreetRepository streetRepo,
        ILogger<FetchTrafficHandler> logger)
        : ICommandHandler<FetchTrafficCommand>
    {
        private const int BatchSize = 10;

        public async Task<Result> Handle(FetchTrafficCommand request, CancellationToken cancellationToken)
        {
            var departureTime = request.DepartureTime;

            logger.LogInformation("Starting traffic-midday update at {Time}", departureTime);

            var segments = await GetAllSegmentsAsync();
            if (segments.Count == 0)
            {
                logger.LogInformation("No segments require update. Exiting.");
                return Result.Success();
            }

            logger.LogInformation("Found {Count} segments to update", segments.Count);

            var batches = segments
                .Select((seg, idx) => new { seg, idx })
                .GroupBy(x => x.idx / BatchSize)
                .Select(g => g.Select(x => x.seg).ToList())
                .ToList();

            logger.LogInformation("Processing in {BatchCount} batches of up to {BatchSize}", batches.Count, BatchSize);

            foreach (var batch in batches)
            {
                try
                {
                    var trafficRequest = new TrafficMatrixRequest(batch, departureTime);
                    var response = await googleClient.GetSegmentDurationMatrixByTraffic(trafficRequest);

                    await streetRepo.UpdateTrafficRegularTime(response.Results);

                    logger.LogInformation("Batch of {Count} segments updated successfully", batch.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error updating batch starting with segment {SegmentId}",
                        batch.First().SegmentId);
                }
            }

            logger.LogInformation("Traffic-midday update complete");
            return Result.Success();
        }

        private async Task<List<TrafficSegment>> GetAllSegmentsAsync()
        {
            // 1) load all intersections
            var intersections = (await intersectionRepo.GetAllAsync())
                .ToList();
            var lookup = intersections.ToDictionary(i => i.Id);

            // 2) load all streets
            var streets = await streetRepo.GetAllAsync();

            // 3) build segment list
            var segments = new List<TrafficSegment>(streets.Count());
            foreach (var s in streets)
            {
                if (s.TrafficRegularTime.HasValue && s.TrafficRegularTime != 0)
                {
                    continue;
                }
                
                if (lookup.TryGetValue(s.FromId, out var from) &&
                    lookup.TryGetValue(s.ToId, out var to))
                {

                    segments.Add(new TrafficSegment(
                        SegmentId: s.Id,
                        FromLat: from.Lat,
                        FromLng: from.Lng,
                        ToLat: to.Lat,
                        ToLng: to.Lng
                    ));
                }
                else
                {
                    logger.LogInformation("Skipped" + s.Name);
                }
            }

            return segments;
        }
    }
}
