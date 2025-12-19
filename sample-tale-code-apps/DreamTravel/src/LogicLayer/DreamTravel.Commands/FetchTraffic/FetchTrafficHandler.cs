using DreamTravel.GeolocationDataClients.GoogleApi;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using DreamTravel.GraphDatabase.Repositories;
using DreamTravel.Domain.StreetGraph;

namespace DreamTravel.Commands.FetchTraffic
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

                    // Filter out segments with invalid data (NaN) before updating database
                    var validResults = response.Results
                        .Where(r => !double.IsNaN(r.DurationInSeconds) &&
                                    !double.IsNaN(r.DistanceInMeters) &&
                                    r.DurationInSeconds > 0 &&
                                    r.DistanceInMeters > 0)
                        .ToList();

                    if (validResults.Count > 0)
                    {
                        await streetRepo.UpdateTrafficRegularTime(validResults);
                        logger.LogInformation("Batch processed: {Valid} valid results out of {Total} segments",
                            validResults.Count, batch.Count);
                    }
                    else
                    {
                        logger.LogWarning("Batch skipped: no valid results from Google API for {Count} segments",
                            batch.Count);
                    }
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
            logger.LogInformation("Streets total: " + streets.Count);

            // 3) build segment list
            var segments = new List<TrafficSegment>(streets.Count());
            foreach (var s in streets)
            {
                if (s.TrafficRegularSpeed.HasValue && s.TrafficRegularSpeed != 0)
                {
                    logger.LogInformation("Skipped " + s.Name);
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
                    logger.LogInformation("Skipped " + s.Name);
                }
            }

            logger.LogInformation("Streets to update: " + segments.Count);

            return segments;
        }
    }
}
