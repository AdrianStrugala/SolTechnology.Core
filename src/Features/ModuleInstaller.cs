using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Features
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddFeatures(this IServiceCollection services)
        {
            RegisterSendDreamTravelFlightEmail(services);

            return services;
        }

        private static void RegisterSendDreamTravelFlightEmail(IServiceCollection services)
        {
            services.AddScoped<IComposeMessage, ComposeMessage>();
            services.AddScoped<IFilterFlights, FilterFlights>();
            services.AddScoped<ISendDreamTravelFlightEmail, SendDreamTravelFlightEmail.SendDreamTravelFlightEmail>();
        }
    }
}
