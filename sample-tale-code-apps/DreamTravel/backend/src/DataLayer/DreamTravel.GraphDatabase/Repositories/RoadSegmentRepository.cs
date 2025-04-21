using DreamTravel.Trips.Domain.StreetGraph;
using Neo4j.Driver;

namespace DreamTravel.GraphDatabase.Repositories;

public interface IRoadSegmentRepository
{
    Task<IEnumerable<RoadSegment>> GetAllAsync(string projectId = "base");
    Task<RoadSegment?> GetByIdAsync(string id, string projectId = "base");
    Task CreateAsync(RoadSegment segment, string projectId = "base");
    Task DeleteAsync(string id, string projectId = "base");
    Task UpdateAsync(RoadSegment segment, string projectId = "base");
}

 public class RoadSegmentRepository : IRoadSegmentRepository, IAsyncDisposable
    {
        private readonly IDriver _driver;

        public RoadSegmentRepository(IDriverProvider provider)
        {
            _driver = provider.Driver;
        }

        public async Task<IEnumerable<RoadSegment>> GetAllAsync(string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(@"
                MATCH (a:Intersection)-[r:CONNECTS_TO {projectId:$projectId}]->(b:Intersection)
                RETURN id(r) AS id,
                       a.id AS fromId, b.id AS toId,
                       r.name AS name, r.length AS length,
                       r.lanes AS lanes, r.`turn:lanes` AS turnLanes
            ", new { projectId });

            var list = new List<RoadSegment>();
            await result.ForEachAsync(rec =>
            {
                list.Add(new RoadSegment
                {
                    Id = rec["id"].As<string>(),
                    FromId = rec["fromId"].As<string>(),
                    ToId = rec["toId"].As<string>(),
                    Name = rec["name"].As<string?>(),
                    Length = rec["length"].As<double>(),
                    Lanes = rec["lanes"].As<int?>(),
                    TurnLanes = rec["turnLanes"].As<string?>()
                });
            });
            return list;
        }

        public async Task<RoadSegment?> GetByIdAsync(string id, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(@"
                MATCH (a:Intersection)-[r:CONNECTS_TO {projectId:$projectId}]->(b:Intersection)
                WHERE id(r) = toInteger($id)
                RETURN id(r) AS id,
                       a.id AS fromId, b.id AS toId,
                       r.name AS name, r.length AS length,
                       r.lanes AS lanes, r.`turn:lanes` AS turnLanes
            ", new { projectId, id });
            var records = await result.ToListAsync();
            if (records.Count == 0) return null;
            var rec = records[0];
            return new RoadSegment
            {
                Id = rec["id"].As<string>(),
                FromId = rec["fromId"].As<string>(),
                ToId = rec["toId"].As<string>(),
                Name = rec["name"].As<string?>(),
                Length = rec["length"].As<double>(),
                Lanes = rec["lanes"].As<int?>(),
                TurnLanes = rec["turnLanes"].As<string?>()
            };
        }

        public async Task CreateAsync(RoadSegment segment, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            await session.RunAsync(@"
                MATCH (a:Intersection {projectId:$projectId, id:$fromId})
                MATCH (b:Intersection {projectId:$projectId, id:$toId})
                CREATE (a)-[r:CONNECTS_TO {projectId:$projectId, name:$name, length:$length, lanes:$lanes, `turn:lanes`:$turnLanes}]->(b)
            ", new
            {
                projectId,
                fromId = segment.FromId,
                toId = segment.ToId,
                name = segment.Name,
                length = segment.Length,
                lanes = segment.Lanes,
                turnLanes = segment.TurnLanes
            });
        }

        public async Task UpdateAsync(RoadSegment segment, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            await session.RunAsync(@"
                MATCH (a:Intersection {projectId:$projectId})-[r:CONNECTS_TO {projectId:$projectId}]->(b:Intersection)
                WHERE id(r) = toInteger($id)
                SET r.name = $name,
                    r.length = $length,
                    r.lanes = $lanes,
                    r.`turn:lanes` = $turnLanes
            ", new
            {
                projectId,
                id = segment.Id,
                name = segment.Name,
                length = segment.Length,
                lanes = segment.Lanes,
                turnLanes = segment.TurnLanes
            });
        }

        public async Task DeleteAsync(string id, string projectId = "base")
        {
            await using var session = _driver.AsyncSession();
            await session.RunAsync(@"
                MATCH ()-[r:CONNECTS_TO {projectId:$projectId}]->()
                WHERE id(r) = toInteger($id)
                DELETE r
            ", new { projectId, id });
        }

        public async ValueTask DisposeAsync()
        {
            // no resources to dispose here
            await Task.CompletedTask;
        }
    }