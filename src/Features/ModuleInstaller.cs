using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Features.CalculateBestPath.Interfaces;
using DreamTravel.Features.FindLocationOfCity;
using DreamTravel.Features.FindNameOfCity;
using DreamTravel.Features.GetFlightEmailOrders;
using DreamTravel.Features.LimitCostOfPaths;
using DreamTravel.Features.Logging;
using DreamTravel.Features.OrderFlightEmail;
using DreamTravel.Features.Registration;
using DreamTravel.Features.SendDreamTravelFlightEmail;
using DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.Features.SendOrderedFlightEmail.Interfaces;
using DreamTravel.FlightProviderData;
using DreamTravel.GeolocationData.Configuration;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.DependencyInjection;
using ComposeMessage = DreamTravel.Features.SendDreamTravelFlightEmail.ComposeMessage;
using IComposeMessage = DreamTravel.Features.SendDreamTravelFlightEmail.Interfaces.IComposeMessage;

namespace DreamTravel.Features
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallFeatures(this IServiceCollection services)
        {
            //TSP engine
            services.AddTransient<ITSP, AntColony>();
            //CalculateBestPath
            services.AddScoped<ICalculateBestPath, CalculateBestPath.CalculateBestPath>();
            services.AddScoped<IDownloadRoadData, CalculateBestPath.DownloadRoadData>();
            services.AddScoped<IFindProfitablePath, CalculateBestPath.FindProfitablePath>();
            services.AddScoped<IFormPathsFromMatrices, CalculateBestPath.FormPathsFromMatrices>();

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

            //GetFlightEmailOrders
            services.AddScoped<IGetFlightEmailOrders, GetFlightEmailOrders.GetFlightEmailOrders>();

            //SendOrderedFlightEmail
            services.AddScoped<ISendOrderedFlightEmail, SendOrderedFlightEmail.SendOrderedFlightEmail>();
            services.AddScoped<SendOrderedFlightEmail.Interfaces.IComposeMessage, SendOrderedFlightEmail.ComposeMessage>();

            //Registration
            services.AddScoped<IRegistration, Registration.Registration>();

            //Logging
            services.AddScoped<ILogging, Logging.Logging>();

            services.InstallFlightProviderData();
            services.InstallDatabaseData();
            services.InstallGeolocationData();

            return services;
        }
    }
}
