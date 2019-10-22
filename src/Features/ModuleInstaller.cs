using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Features.DreamFlight.GetFlightEmailOrders;
using DreamTravel.Features.DreamFlight.OrderFlightEmail;
using DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail;
using DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.Features.DreamFlight.SendOrderedFlightEmail;
using DreamTravel.Features.DreamFlight.SendOrderedFlightEmail.Interfaces;
using DreamTravel.Features.DreamTrip.CalculateBestPath;
using DreamTravel.Features.DreamTrip.CalculateBestPath.Interfaces;
using DreamTravel.Features.DreamTrip.FindLocationOfCity;
using DreamTravel.Features.DreamTrip.FindNameOfCity;
using DreamTravel.Features.DreamTrip.LimitCostOfPaths;
using DreamTravel.Features.Identity.Logging;
using DreamTravel.Features.Identity.Registration;
using DreamTravel.FlightProviderData;
using DreamTravel.GeolocationData.Configuration;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;
using ComposeMessage = DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail.ComposeMessage;
using IComposeMessage = DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail.Interfaces.IComposeMessage;

namespace DreamTravel.Features
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallFeatures(this IServiceCollection services)
        {
            //TSP engine
            services.AddTransient<ITSP, AntColony>();
            //CalculateBestPath
            services.AddScoped<ICalculateBestPath, CalculateBestPath>();
            services.AddScoped<IDownloadRoadData, DownloadRoadData>();
            services.AddScoped<IFindProfitablePath, FindProfitablePath>();
            services.AddScoped<IFormPathsFromMatrices, FormPathsFromMatrices>();

            //SendDreamTravelFlightEmail
            services.AddScoped<IComposeMessage, ComposeMessage>();
            services.AddScoped<IFilterFlights, FilterFlights>();
            services.AddScoped<ISendDreamTravelFlightEmail, SendDreamTravelFlightEmail>();

            //FindNameOfCIty
            services.AddScoped<IFindNameOfCity, FindNameOfCity>();

            //FindLocationOfCity
            services.AddScoped<IFindLocationOfCity, FindLocationOfCity>();

            //LimitCostOfPaths
            services.AddScoped<ILimitCostOfPaths, LimitCostOfPaths>();

            //OrderFlightEmail
            services.AddScoped<IOrderFlightEmail, OrderFlightEmail>();

            //GetFlightEmailOrders
            services.AddScoped<IGetFlightEmailOrders, GetFlightEmailOrders>();

            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail>();
            services.AddScoped<DreamFlight.SendOrderedFlightEmail.Interfaces.IComposeMessage, DreamFlight.SendOrderedFlightEmail.ComposeMessage>();

            //Registration
            services.AddScoped<IRegistration, Registration>();

            //Logging
            services.AddScoped<ILogging, Logging>();

            services.InstallFlightProviderData();
            services.InstallDatabaseData();
            services.InstallGeolocationData();

            return services;
        }
    }
}
