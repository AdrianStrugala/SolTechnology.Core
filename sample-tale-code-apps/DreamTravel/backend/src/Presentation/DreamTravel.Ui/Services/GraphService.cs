// DreamTravel.Ui/Services/GraphService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using DreamTravel.Trips.Domain.StreetGraph;

namespace DreamTravel.Ui.Services
{
    public class GraphService
    {
        private readonly HttpClient _http;
        private const string DefaultProjectId = "base";

        public GraphService(HttpClient http) => _http = http;

        public async Task<List<Intersection>> GetNodesAsync()
        {
            var url = $"/api/projects/nodes";
            var resp = await _http.GetFromJsonAsync<ApiResponse<Intersection>>(url);
            return resp?.Data ?? new List<Intersection>();
        }

        public async Task<List<Street>> GetStreetsAsync()
        {
            var url = $"/api/projects/streets";
            var resp = await _http.GetFromJsonAsync<ApiResponse<Street>>(url);
            return resp?.Data ?? new List<Street>();
        }
    }
}