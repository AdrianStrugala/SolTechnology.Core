﻿using TaleCode.Faker.FakesBase;
using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;

namespace TaleCode.Faker
{
    public class WireMockStartup
    {
        public WireMockServer WireMockServer = null!;
        private readonly List<IFakeApi> _fakeServices = new();

        public void Run(int port = 0, bool runAsServer = false)
        {
            var settings = new WireMockServerSettings
            {
                AllowPartialMapping = false,
                StartAdminInterface = true,
                ReadStaticMappings = false,
                Logger = new WireMockConsoleLogger(),
                Port = port
            };

            WireMockServer = StartServer(runAsServer, settings);
            RegisterFakeServices(WireMockServer);
        }

        private static WireMockServer StartServer(bool runAsServer, WireMockServerSettings settings)
        {
            if (!runAsServer)
            {
                return WireMockServer.Start(settings);
            }

            var standaloneServer = StandAloneApp.Start(settings);
            Console.WriteLine(@"WireMockServer running at ports: [{0}]", string.Join(",", standaloneServer.Ports));
            return standaloneServer;
        }

        public IFakeServiceBuilderWithRequest<TApiClient> GetFakeApi<TApiClient>() where TApiClient : class
        {
            var fakeApi = _fakeServices.First(f =>
                f.GetType().BaseType != null &&
                f.GetType().BaseType!.GenericTypeArguments.Contains(typeof(TApiClient)));

            return (IFakeServiceBuilderWithRequest<TApiClient>)fakeApi;
        }

        private void RegisterFakeServices(WireMockServer mockServer)
        {
            var fakeApis = typeof(WireMockStartup).Assembly.DefinedTypes
                .Where(x =>
                    !x.IsAbstract &&
                    x.IsClass &&
                    x.ImplementedInterfaces.Contains(typeof(IFakeApi)))
                .ToArray();

            foreach (var fakeApi in fakeApis)
            {
                var fakeService = (IFakeApi)Activator.CreateInstance(fakeApi)!;
                fakeService.Register(mockServer);
                _fakeServices.Add(fakeService);
            }
        }
    }
}