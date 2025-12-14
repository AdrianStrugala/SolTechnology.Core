using System.Net.Http.Json;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Ui.Models;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Ui.Services;

public class TspService : ITspService
{
    private readonly HttpClient _http;
    private const string FindCityByNameUrl = "/api/v2/FindCityByName";
    private const string FindCityByCoordinatesUrl = "/api/v2/FindCityByCoordinates";
    private const string CalculateBestPathUrl = "/api/v2/CalculateBestPath";

    public TspService(HttpClient http) => _http = http;

    public async Task<Result<City>> FindCityByNameAsync(string name)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(FindCityByNameUrl, new { name });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result<City>>();
            return result ?? Result<City>.Fail("Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            return Result<City>.Fail($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<City>.Fail($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<City>> FindCityByCoordinatesAsync(double lat, double lng)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(FindCityByCoordinatesUrl, new { lat, lng });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result<City>>();
            return result ?? Result<City>.Fail("Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            return Result<City>.Fail($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<City>.Fail($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<CalculateBestPathResult>> CalculateBestPathAsync(List<City> cities)
    {
        try
        {
            var query = new CalculateBestPathQuery { Cities = cities };
            var response = await _http.PostAsJsonAsync(CalculateBestPathUrl, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result<CalculateBestPathResult>>();
            return result ?? Result<CalculateBestPathResult>.Fail("Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            return Result<CalculateBestPathResult>.Fail($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<CalculateBestPathResult>.Fail($"Unexpected error: {ex.Message}");
        }
    }

    public static string FormatTimeFromSeconds(double totalSeconds)
    {
        var hours = (int)(totalSeconds / 3600);
        var minutes = (int)((totalSeconds % 3600) / 60);
        var seconds = (int)(totalSeconds % 60);

        return $"{hours}:{minutes:D2}:{seconds:D2}";
    }
}
