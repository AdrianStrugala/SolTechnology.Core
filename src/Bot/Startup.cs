using DreamTravel.DatabaseData.Users;
using Autofac;
using AzureFunctions.Autofac.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using DreamTravel.DatabaseData;
using DreamTravel.FlightProviderData;
using DreamTravel.FlightProviderData.Flights.GetFlights;
using DreamTravel.Infrastructure;

namespace DreamTravel.Bot
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(string functionName)
        {
            var applicatonConfiguration = new ApplicationConfiguration();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\..\\appsettings.json", true)
                .AddJsonFile("appsettings.json", true)
                .Build();

            configuration.Bind(applicatonConfiguration);

            DependencyInjection.Initialize(
                builder =>
                {
                    builder.RegisterInstance(applicatonConfiguration).As<ApplicationConfiguration>().AsImplementedInterfaces();

                    builder.RegisterAssemblyTypes(GetType().Assembly)
                        .Except<ApplicationConfiguration>()
                        .AsImplementedInterfaces();

                    builder.RegisterType<FlightRepository>().As<IFlightRepository>();
                    builder.RegisterType<UserRepository>().As<IUserRepository>();
                },
                functionName);
        }

        public T ResolveDependency<T>(string functionClassName = null)
        {
            return (T)DependencyInjection.Resolve(typeof(T), functionClassName, string.Empty, Guid.Empty);
        }

        public object ResolveDependency(Type type, string functionClassName = null)
        {
            return DependencyInjection.Resolve(type, functionClassName, string.Empty, Guid.Empty);
        }
    }
}