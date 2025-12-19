using DreamTravel.Domain.StreetGraph;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace DreamTravel.GeolocationDataClients.GoogleApi;

public record TrafficMatrixRequest(
    List<TrafficSegment> Segments,
    DateTime DepartureTime
);

public record TrafficMatrixResponse(
    List<TrafficSegment> Results
);

public partial class GoogleApiClient : IGoogleApiClient
{
    /// <summary>
    /// For each TrafficSegment in the input list, populates DurationInSeconds
    /// by querying Google's Distance Matrix with traffic for departureTime.
    /// Segments are processed in batches of 10 to honor the 100‐element limit.
    /// </summary>
    public async Task<TrafficMatrixResponse> GetSegmentDurationMatrixByTraffic(TrafficMatrixRequest request)
    {
        var departureTime = request.DepartureTime;
        var departureTs = new DateTimeOffset(departureTime).ToUnixTimeSeconds();

        var batch = request.Segments;
        int n = batch.Count;

        // Build origins/destinations strings
        var origins = string.Join('|',
            batch.Select(s =>
                $"{s.FromLat.ToString("G", CultureInfo.InvariantCulture)}," +
                $"{s.FromLng.ToString("G", CultureInfo.InvariantCulture)}"));
        var destinations = string.Join('|',
            batch.Select(s =>
                $"{s.ToLat.ToString("G", CultureInfo.InvariantCulture)}," +
                $"{s.ToLng.ToString("G", CultureInfo.InvariantCulture)}"));

        var url = $"maps/api/distancematrix/json" +
                  $"?origins={origins}" +
                  $"&destinations={destinations}" +
                  $"&departure_time={departureTs}" +
                  $"&key={_options.Key}";

        var resp = await httpClient.GetAsync(url);
        resp.EnsureSuccessStatusCode();

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync());

        // Check if response has rows array
        var rows = json["rows"] as JArray;
        if (rows == null)
        {
            // No rows in response, set all segments to NaN
            foreach (var segment in batch)
            {
                segment.DurationInSeconds = double.NaN;
                segment.DistanceInMeters = double.NaN;
            }
            return new TrafficMatrixResponse(batch);
        }

        // For each segment i in this batch, take the [i][i] element
        for (int i = 0; i < n; i++)
        {
            // Check if row exists and has elements
            if (i >= rows.Count)
            {
                batch[i].DurationInSeconds = double.NaN;
                batch[i].DistanceInMeters = double.NaN;
                continue;
            }

            var elements = rows[i]?["elements"] as JArray;
            if (elements == null || i >= elements.Count)
            {
                batch[i].DurationInSeconds = double.NaN;
                batch[i].DistanceInMeters = double.NaN;
                continue;
            }

            var cell = elements[i];
            if (cell?["status"]?.Value<string>() == "OK")
            {
                batch[i].DurationInSeconds =
                    cell["duration_in_traffic"]?["value"]?.Value<double>()
                    ?? cell["duration"]?["value"]?.Value<double>()
                    ?? double.NaN;

                batch[i].DistanceInMeters =
                    cell["distance"]?["value"]?.Value<double>()
                    ?? double.NaN;
            }
            else
            {
                batch[i].DurationInSeconds = double.NaN;
                batch[i].DistanceInMeters = double.NaN;
            }
        }

        return new TrafficMatrixResponse(batch);
    }
}