using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Bot.DiscoverIndividualChances.DataAccess;
using DreamTravel.Bot.DiscoverIndividualChances.Interfaces;
using DreamTravel.Features;
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
            builder.Services.AddTransient<IComposeMessage, ComposeMessage>();
            builder.Services.AddTransient<IDiscoverIndividualChances, DiscoverIndividualChances.DiscoverIndividualChances>();
            builder.Services.AddTransient<IGetFlightsFromSkyScanner, GetFlightsFromSkyScanner>();

            builder.Services.InstallFeatures();
        }
    }
}