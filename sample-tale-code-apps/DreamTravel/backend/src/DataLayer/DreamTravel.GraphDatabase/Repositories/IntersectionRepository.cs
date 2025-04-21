using DreamTravel.Trips.Domain.StreetGraph;
using Neo4j.Driver;

namespace DreamTravel.GraphDatabase.Repositories;


    public interface IIntersectionRepository
    {
        Task<IEnumerable<Intersection>> GetAllAsync(string projectId = "base");
        Task<Intersection?> GetByIdAsync(string id, string projectId = "base");
        Task CreateAsync(Intersection intersection, string projectId = "base");
        Task DeleteAsync(string id, string projectId = "base");
        Task UpdateAsync(Intersection intersection, string projectId = "base");
    }



public class IntersectionRepository : IIntersectionRepository, IAsyncDisposable
    {
        private readonly IDriver _driver;

        public IntersectionRepository(IDriverProvider provider)
        {
            _driver = provider.Driver;
        }

        public async Task<IEnumerable<Intersection>> GetAllAsync(string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(@"
                MATCH (n:Intersection {projectId:$projectId})
                RETURN n.id AS id, n.lat AS lat, n.lng AS lng
            ", new { projectId });

            var list = new List<Intersection>();
            await result.ForEachAsync(rec =>
            {
                list.Add(new Intersection
                {
                    Id = rec["id"].As<string>(),
                    Lat = rec["lat"].As<double>(),
                    Lng = rec["lng"].As<double>()
                });
            });
            return list;
        }

        public async Task<Intersection?> GetByIdAsync(string id, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            IResultCursor? result = await session.RunAsync(@"
                MATCH (n:Intersection {projectId:$projectId, id:$id})
                RETURN n.id AS id, n.lat AS lat, n.lng AS lng
            ", new { projectId, id });
            var records = await result.ToListAsync();
            if (records.Count == 0) return null;
            var rec = records[0];
            return new Intersection
            {
                Id = rec["id"].As<string>(),
                Lat = rec["lat"].As<double>(),
                Lng = rec["lng"].As<double>()
            };
        }

        public async Task CreateAsync(Intersection intersection, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            await session.RunAsync(@"
                CREATE (n:Intersection {projectId:$projectId, id:$id, lat:$lat, lng:$lng})
            ", new { projectId, id = intersection.Id, lat = intersection.Lat, lng = intersection.Lng });
        }

        public async Task UpdateAsync(Intersection intersection, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            await session.RunAsync(@"
                MATCH (n:Intersection {projectId:$projectId, id:$id})
                SET n.lat = $lat, n.lng = $lng
            ", new { projectId, id = intersection.Id, lat = intersection.Lat, lng = intersection.Lng });
        }

        public async Task DeleteAsync(string id, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            await session.RunAsync(@"
                MATCH (n:Intersection {projectId:$projectId, id:$id})
                DETACH DELETE n
            ", new { projectId, id });
        }

        public async ValueTask DisposeAsync()
        {
            // no resources to dispose here
            await Task.CompletedTask;
        }
    }