using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Features.FindLocationOfCity;
using DreamTravel.Features.FindNameOfCity;
using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.FlightProviderData;
using DreamTravel.GeolocationData.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Features
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallFeatures(this IServiceCollection services)
        {
            //SendDreamTravelFlightEmail
            services.AddScoped<IComposeMessage, ComposeMessage>();
            services.AddScoped<IFilterFlights, FilterFlights>();
            services.AddScoped<ISendDreamTravelFlightEmail, SendDreamTravelFlightEmail.SendDreamTravelFlightEmail>();

            //FindNameOfCIty
            services.AddScoped<IFindNameOfCity, FindNameOfCity.FindNameOfCity>();

            //FindLocationOfCity
            services.AddScoped<IFindLocationOfCity, FindLocationOfCity.FindLocationOfCity>();


            services.InstallFlightProviderData();
            services.InstallDatabaseData();
            services.InstallGeolocationData();

            return services;
        }
    }
}
