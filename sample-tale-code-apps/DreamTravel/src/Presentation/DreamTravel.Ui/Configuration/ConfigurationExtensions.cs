using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DreamTravel.Ui.Configuration;

public static class ConfigurationExtensions
{
    public static ApiConfiguration GetApiConfiguration(this WebAssemblyHostBuilder builder)
    {
        var baseUrl = builder.Configuration["services:dreamtravel-api:https:0"]
                      ?? builder.Configuration["services:dreamtravel-api:http:0"]
                      ?? builder.Configuration["ApiBaseUrl"];

        var apiKey = builder.Configuration["Authentication:ApiKey"];

        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException(
                "API Base URL is not configured. " +
                "Provide 'ApiBaseUrl' in appsettings.json or run via Aspire with service discovery.");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "API Key is not configured. " +
                "Provide 'Authentication:ApiKey' in appsettings.json or user secrets.");
        }

        return new ApiConfiguration
        {
            BaseUrl = baseUrl,
            ApiKey = apiKey
        };
    }

    public static GoogleMapsConfiguration GetGoogleMapsConfiguration(this WebAssemblyHostBuilder builder)
    {
        var apiKey = builder.Configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Google Maps API Key is not configured. " +
                "Provide 'GoogleMaps:ApiKey' in appsettings.json or user secrets.");
        }

        return new GoogleMapsConfiguration
        {
            ApiKey = apiKey
        };
    }
}
