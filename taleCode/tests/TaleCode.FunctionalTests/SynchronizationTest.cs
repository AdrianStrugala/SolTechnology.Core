using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using TaleCode.ComponentTests.TestsConfiguration;
using TaleCode.IntegrationTests.SqlData;
using Xunit;

namespace TaleCode.ComponentTests
{
    [Collection(nameof(TaleCodeComponentTests))]
    public class SynchronizationTest
    {
        private readonly SqlFixture _sqlFixture;
        private readonly TestServer _backgroundWorker;

        public SynchronizationTest(ComponentTestsFixture componentTestsFixture)
        {
            _backgroundWorker = componentTestsFixture.BackgroundWorkerFixture.TestServer;
            _sqlFixture = componentTestsFixture.SqlFixture;
        }

        [Fact]
        public async Task After_Synchronization_Data_Can_Be_Accessed_By_Api()
        {
            await _backgroundWorker
                .CreateRequest("api/SynchronizePlayerMatches/44")
                .GetAsync();

            var x = 1;
        }
    }
}