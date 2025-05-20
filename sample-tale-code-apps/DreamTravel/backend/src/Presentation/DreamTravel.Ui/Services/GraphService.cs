
using System.Net.Http.Json;
using DreamTravel.Trips.Domain.StreetGraph;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Ui.Services;

public class GraphService
{
    private readonly HttpClient _http;
    private const string DefaultProjectId = "base";

    public GraphService(HttpClient http) => _http = http;

    public async Task<List<Intersection>> GetNodesAsync()
    {
        var url = $"/api/projects/nodes";
        var resp = await _http.GetFromJsonAsync<Result<List<Intersection>>>(url);
        return resp?.Data ?? new List<Intersection>();
    }

    public async Task<List<Street>> GetStreetsAsync()
    {
        var url = $"/api/projects/streets";
        var resp = await _http.GetFromJsonAsync<Result<List<Street>>>(url);
        return resp?.Data ?? new List<Street>();
    }
        
    /// <summary>
    /// Recalculates traffic data for a given set of streets and intersections
    /// </summary>
    /// <param name="streets">List of streets including any new ones</param>
    /// <param name="intersections">List of intersections</param>
    /// <returns>Response with updated traffic data</returns>
    public async Task<Result<List<TrafficSegment>>> RecalculateTrafficAsync(List<Street> streets, List<Intersection> intersections)
    {
        var response = await _http.CreateRequest("/api/traffic/recalculate")
            .WithBody(new
            {
                Streets = streets,
                Intersections = intersections
            })
            .PostAsync<Result<List<TrafficSegment>>>();

        return response;
    }
}