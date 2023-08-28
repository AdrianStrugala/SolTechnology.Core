using DreamTravel.DatabaseData.Configuration;
using DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser;
using DreamTravel.DreamFlights.GetTodaysFlightEmailData;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Executors;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using DreamTravel.FlightProviderData;
using DreamTravel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ComposeMessage = DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Executors.ComposeMessage;
using IComposeMessage = DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces.IComposeMessage;

namespace DreamTravel.DreamFlights
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDreamFlights(this IServiceCollection services)
        {
            services.RegisterAllImplementations(typeof(ICommandHandler<>));

            //SendDreamTravelFlightEmailHandler
            services.AddScoped<IComposeMessage, ComposeMessage>();
            services.AddScoped<IFilterFlights, FilterFlights>();
            services.AddScoped<ISendDreamTravelFlightEmail, SendDreamTravelFlightEmailHandler>();

            //GetTodaysFlightEmailDataHandler
            services.AddScoped<IGetTodaysFlightEmailData, GetTodaysFlightEmailDataHandler>();

            //GetFlightEmailSubscriptionsForUserHandler
            services.AddScoped<IGetFlightEmailSubscriptionsForUser, GetFlightEmailSubscriptionsForUserHandler>();
            
            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail.SendOrderedFlightEmail>();
            services.AddScoped<SendOrderedFlightEmail.Interfaces.IComposeMessage, SendOrderedFlightEmail.ComposeMessage>();
            

            services.InstallStaticData();
            services.InstallDatabaseData();

            return services;
        }
    }
}
