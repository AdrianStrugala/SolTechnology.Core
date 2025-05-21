using DreamTravel.Ui;
using DreamTravel.Ui.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var apiUrl = builder.Configuration["ApiBaseUrl"];
var apiKey = builder.Configuration["Authentication:ApiKey"];
if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
    throw new InvalidOperationException("Brakuje ApiBaseUrl lub Authentication:ApiKey w konfiguracji.");

builder.Services.AddScoped(sp =>
{
    var client = new HttpClient { BaseAddress = new Uri(apiUrl) };
    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    return client;
});

builder.Services.AddScoped<GraphService>();

await builder.Build().RunAsync();
