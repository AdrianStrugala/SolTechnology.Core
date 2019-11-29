using DreamTravel.DatabaseData.Configuration;
using DreamTravel.DreamFlights.GetAirports;
using DreamTravel.DreamFlights.GetFlightEmailData;
using DreamTravel.DreamFlights.GetFlightEmailOrdersForUser;
using DreamTravel.DreamFlights.OrderFlightEmail;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using DreamTravel.FlightProviderData;
using Microsoft.Extensions.DependencyInjection;
using ComposeMessage = DreamTravel.DreamFlights.SendDreamTravelFlightEmail.ComposeMessage;
using IComposeMessage = DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces.IComposeMessage;

namespace DreamTravel.DreamFlights
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamFlights(this IServiceCollection services)
        {
            //SendDreamTravelFlightEmail
            services.AddScoped<IComposeMessage, ComposeMessage>();
            services.AddScoped<IFilterFlights, FilterFlights>();
            services.AddScoped<ISendDreamTravelFlightEmail, SendDreamTravelFlightEmail.SendDreamTravelFlightEmail>();

            //OrderFlightEmail
            services.AddScoped<IOrderFlightEmail, OrderFlightEmail.OrderFlightEmail>();

            //GetFlightEmailData
            services.AddScoped<IGetFlightEmailData, GetFlightEmailData.GetFlightEmailData>();

            //GetFlightEmailOrdersForUser
            services.AddScoped<IGetFlightEmailOrdersForUser, GetFlightEmailOrdersForUser.GetFlightEmailOrdersForUser>();

            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail.SendOrderedFlightEmail>();
            services.AddScoped<SendOrderedFlightEmail.Interfaces.IComposeMessage, SendOrderedFlightEmail.ComposeMessage>();

            //GetAirports
            services.AddScoped<IGetAirports, GetAirports.GetAirports>();

            services.InstallFlightProviderData();
            services.InstallDatabaseData();

            return services;
        }
    }
}
