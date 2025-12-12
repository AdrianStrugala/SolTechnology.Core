using DreamTravel.Ui;
using DreamTravel.Ui.Configuration;
using DreamTravel.Ui.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiConfig = builder.GetApiConfiguration();
var googleMapsConfig = builder.GetGoogleMapsConfiguration();

builder.Services.AddDreamTravelServices(apiConfig, googleMapsConfig);

await builder.Build().RunAsync();
