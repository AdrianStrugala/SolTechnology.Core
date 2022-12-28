using TaleCode.IntegrationTests.SqlData;
using Xunit;

namespace TaleCode.ComponentTests.TestsConfiguration
{
    public class ComponentTestsFixture
    {
        public BackgroundWorkerFixture BackgroundWorkerFixture { get; set; }
        public SqlFixture SqlFixture { get; set; }

        public ComponentTestsFixture()
        {
            BackgroundWorkerFixture ??= new BackgroundWorkerFixture();
            SqlFixture ??= new SqlFixture();

            SqlFixture.InitializeAsync().GetAwaiter().GetResult();
        }
    }

    [CollectionDefinition(nameof(TaleCodeComponentTests))]
    public class TaleCodeComponentTests : ICollectionFixture<ComponentTestsFixture>
    {
    }

}
