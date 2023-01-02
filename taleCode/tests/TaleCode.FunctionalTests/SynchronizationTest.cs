using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using TaleCode.FunctionalTests.TestsConfiguration;
using TaleCode.IntegrationTests.SqlData;
using Xunit;

namespace TaleCode.FunctionalTests
{
    [Collection(nameof(TaleCodeFunctionalTestsCollection))]
    public class SynchronizationTest
    {
        private readonly SqlFixture _sqlFixture;
        private readonly TestServer _backgroundWorker;

        public SynchronizationTest(FunctionalTestsFixture functionalTestsFixture)
        {
            _backgroundWorker = functionalTestsFixture.BackgroundWorkerFixture.TestServer;
            _sqlFixture = functionalTestsFixture.SqlFixture;
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