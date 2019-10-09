using System.Globalization;
using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Bot.DiscoverIndividualChances.DataAccess;
using DreamTravel.Features;
using DreamTravel.Features.SendOrderedFlightEmail.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using IComposeMessage = DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces.IComposeMessage;

[assembly: FunctionsStartup(typeof(DreamTravel.Bot.Startup))]

namespace DreamTravel.Bot
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var cultureInfo = new CultureInfo("en-US");
            cultureInfo.NumberFormat.CurrencySymbol = "€";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            builder.Services.AddTransient<IComposeMessage, ComposeMessage>();
            builder.Services.AddTransient<IDiscoverIndividualChances, DiscoverIndividualChances.DiscoverIndividualChances>();
            builder.Services.AddTransient<IGetFlightsFromSkyScanner, GetFlightsFromSkyScanner>();

            builder.Services.InstallFeatures();
        }
    }
}