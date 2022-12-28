using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace TaleCode.ComponentTests.TestsConfiguration
{
    public class BackgroundWorkerFixture : IDisposable
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public BackgroundWorkerFixture()
        {
            var webAppFactory = new WebApplicationFactory<Program>();
            TestServer = webAppFactory.Server;

            ServerClient = TestServer.CreateClient();
        }

        public void Dispose()
        {
            ServerClient?.Dispose();
            TestServer?.Dispose();
        }
    }
}
