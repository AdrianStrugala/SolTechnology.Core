using DreamTravel.Ui.Configuration;
using DreamTravel.Ui.Services;
using MudBlazor.Services;

namespace DreamTravel.Ui.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDreamTravelServices(
        this IServiceCollection services,
        ApiConfiguration apiConfig,
        GoogleMapsConfiguration googleMapsConfig)
    {
        services.AddMudServices();

        services.AddScoped(sp =>
        {
            var client = new HttpClient { BaseAddress = new Uri(apiConfig.BaseUrl) };
            client.DefaultRequestHeaders.Add("X-Api-Key", apiConfig.ApiKey);
            return client;
        });

        services.AddScoped<GraphService>();
        services.AddScoped<ITspService, TspService>();

        services.AddSingleton(googleMapsConfig);

        return services;
    }
}
