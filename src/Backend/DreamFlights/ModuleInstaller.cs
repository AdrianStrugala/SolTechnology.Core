using DreamTravel.DatabaseData.Configuration;
using DreamTravel.DreamFlights.DeleteFlightEmailSubscription;
using DreamTravel.DreamFlights.GetAirports;
using DreamTravel.DreamFlights.GetFlightEmailData;
using DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SubscribeForFlightEmail;
using DreamTravel.DreamFlights.UpdateSubscriptions;
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

            //SubscribeForFlightEmail
            services.AddScoped<ISubscribeForFlightEmail, SubscribeForFlightEmail.SubscribeForFlightEmail>();

            //GetFlightEmailData
            services.AddScoped<IGetFlightEmailData, GetFlightEmailData.GetFlightEmailData>();

            //GetFlightEmailSubscriptionsForUser
            services.AddScoped<IGetFlightEmailSubscriptionsForUser, GetFlightEmailSubscriptionsForUser.GetFlightEmailSubscriptionsForUser>();

            //DeleteFlightEmailSubscription
            services.AddScoped<IDeleteFlightEmailSubscription, DeleteFlightEmailSubscription.DeleteFlightEmailSubscription>();

            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail.SendOrderedFlightEmail>();
            services.AddScoped<SendOrderedFlightEmail.Interfaces.IComposeMessage, SendOrderedFlightEmail.ComposeMessage>();

            //GetAirports
            services.AddScoped<IGetAirports, GetAirports.GetAirports>();

            //Update Subscriptions
            services.AddTransient<IUpdateSubscriptions, UpdateSubscriptionsHandler>();

            services.InstallFlightProviderData();
            services.InstallDatabaseData();

            return services;
        }
    }
}
