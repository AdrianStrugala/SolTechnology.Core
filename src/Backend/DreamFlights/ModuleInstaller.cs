using DreamTravel.DatabaseData.Configuration;
using DreamTravel.DreamFlights.DeleteFlightEmailSubscription;
using DreamTravel.DreamFlights.GetAirports;
using DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser;
using DreamTravel.DreamFlights.GetTodaysFlightEmailData;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Executors;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SubscribeForFlightEmail;
using DreamTravel.DreamFlights.UpdateSubscriptions;
using DreamTravel.FlightProviderData;
using Microsoft.Extensions.DependencyInjection;
using ComposeMessage = DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Executors.ComposeMessage;
using IComposeMessage = DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces.IComposeMessage;

namespace DreamTravel.DreamFlights
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamFlights(this IServiceCollection services)
        {
            //SendDreamTravelFlightEmailHandler
            services.AddScoped<IComposeMessage, ComposeMessage>();
            services.AddScoped<IFilterFlights, FilterFlights>();
            services.AddScoped<ISendDreamTravelFlightEmail, SendDreamTravelFlightEmail.SendDreamTravelFlightEmailHandler>();

            //SubscribeForFlightEmailHandler
            services.AddScoped<ISubscribeForFlightEmail, SubscribeForFlightEmail.SubscribeForFlightEmailHandler>();

            //GetTodaysTodaysFlightEmailDataHandler
            services.AddScoped<IGetTodaysFlightEmailData, GetTodaysTodaysFlightEmailDataHandler>();

            //GetFlightEmailSubscriptionsForUserHandler
            services.AddScoped<IGetFlightEmailSubscriptionsForUser, GetFlightEmailSubscriptionsForUser.GetFlightEmailSubscriptionsForUserHandler>();

            //DeleteFlightEmailSubscriptionHandler
            services.AddScoped<IDeleteFlightEmailSubscription, DeleteFlightEmailSubscription.DeleteFlightEmailSubscriptionHandler>();

            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail.SendOrderedFlightEmail>();
            services.AddScoped<SendOrderedFlightEmail.Interfaces.IComposeMessage, SendOrderedFlightEmail.ComposeMessage>();

            //GetAirportsHandler
            services.AddScoped<IGetAirports, GetAirports.GetAirportsHandler>();

            //Update Subscriptions
            services.AddTransient<IUpdateSubscriptions, UpdateSubscriptionsHandler>();

            services.InstallFlightProviderData();
            services.InstallDatabaseData();

            return services;
        }
    }
}
