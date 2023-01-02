using TaleCode.IntegrationTests.SqlData;
using Xunit;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public class FunctionalTestsFixture
    {
        public BackgroundWorkerFixture BackgroundWorkerFixture { get; set; }
        public SqlFixture SqlFixture { get; set; }

        public FunctionalTestsFixture()
        {
            BackgroundWorkerFixture ??= new BackgroundWorkerFixture();
            SqlFixture ??= new SqlFixture();

            SqlFixture.InitializeAsync().GetAwaiter().GetResult();
        }
    }

    [CollectionDefinition(nameof(TaleCodeFunctionalTestsCollection))]
    public class TaleCodeFunctionalTestsCollection : ICollectionFixture<FunctionalTestsFixture>
    {
    }

}
