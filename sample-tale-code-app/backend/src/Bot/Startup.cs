﻿using System.Globalization;
using DreamTravel.DreamFlights;
using DreamTravel.DreamTrips;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

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

            builder.Services.InstallDreamTrips();
            builder.Services.InstallDreamFlights();
        }
    }
}