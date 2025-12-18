// RoadPlanner.Data/Neo4jDriverProvider.cs

using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace DreamTravel.GraphDatabase
{
    public interface IDriverProvider
    {
        IDriver Driver { get; }
    }

    public class Neo4jDriverProvider : IDriverProvider, IAsyncDisposable
    {
        public IDriver Driver { get; }

        public Neo4jDriverProvider(IOptions<Neo4jSettings> settings)
        {
            var config = settings.Value;
            Driver = Neo4j.Driver.GraphDatabase.Driver(config.Uri, AuthTokens.Basic(config.User, config.Password));
        }

        public async ValueTask DisposeAsync()
        {
            await Driver.CloseAsync();
            Driver.Dispose();
        }
    }
}