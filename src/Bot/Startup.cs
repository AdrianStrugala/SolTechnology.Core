using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using DreamTravel.DatabaseData;
using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Infrastructure;
using DreamTravel.Bot.DiscoverIndividualChances.DataAccess;
using DreamTravel.Bot.DiscoverIndividualChances.Interfaces;
using DreamTravel.DatabaseData.Subscriptions;
using DreamTravel.Features;
using DreamTravel.FlightProviderData;
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
            var applicatonConfiguration = new ApplicationConfiguration();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\..\\appsettings.json", true)
                .AddJsonFile("appsettings.json", true)
                .Build();

            configuration.Bind(applicatonConfiguration);


            builder.Services.AddSingleton<ApplicationConfiguration>(applicatonConfiguration);
            builder.Services.AddTransient<IComposeMessage, ComposeMessage>();
            builder.Services.AddTransient<IDiscoverIndividualChances, DiscoverIndividualChances.DiscoverIndividualChances>();
            builder.Services.AddTransient<IGetFlightsFromSkyScanner, GetFlightsFromSkyScanner>();


            builder.Services.AddDatabaseData();
            builder.Services.AddFlightProviderData();
            builder.Services.AddFeatures();
        }
    }
}