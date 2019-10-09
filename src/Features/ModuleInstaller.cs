using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Features.FindLocationOfCity;
using DreamTravel.Features.FindNameOfCity;
using DreamTravel.Features.LimitCostOfPaths;
using DreamTravel.Features.OrderFlightEmail;
using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.Features.SendOrderedFlightEmail;
using DreamTravel.FlightProviderData;
using DreamTravel.GeolocationData.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ComposeMessage = DreamTravel.Features.SendDreamTravelFlightEmail.ComposeMessage;

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

            //LimitCostOfPaths
            services.AddScoped<ILimitCostOfPaths, LimitCostOfPaths.LimitCostOfPaths>();

            //OrderFlightEmail
            services.AddScoped<IOrderFlightEmail, OrderFlightEmail.OrderFlightEmail>();

            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail.SendOrderedFlightEmail>();
            services.AddScoped<SendOrderedFlightEmail.Interfaces.IComposeMessage, SendOrderedFlightEmail.ComposeMessage>();

            services.InstallFlightProviderData();
            services.InstallDatabaseData();
            services.InstallGeolocationData();

            return services;
        }
    }
}
