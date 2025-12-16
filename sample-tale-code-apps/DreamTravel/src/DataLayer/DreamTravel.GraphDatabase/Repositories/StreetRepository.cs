using DreamTravel.Domain.StreetGraph;
using Neo4j.Driver;

namespace DreamTravel.GraphDatabase.Repositories;

public interface IStreetRepository
{
    Task<List<Street>> GetAllAsync();
    Task<Street?> GetByIdAsync(string id);
    Task CreateAsync(Street segment);
    Task UpdateAsync(Street segment);
    Task DeleteAsync(string id);

    Task UpdateTrafficRegularTime(IEnumerable<TrafficSegment> trafficResults);
}

public class StreetRepository(IDriverProvider provider) : IStreetRepository, IAsyncDisposable
{
    private readonly IDriver _driver = provider.Driver;

    public async Task<List<Street>> GetAllAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(@"
                MATCH (a)-[r:RELATED]->(b)
                WHERE a.id IS NOT NULL AND b.id IS NOT NULL
                RETURN
                  id(r)                          AS id,
                  a.id                           AS fromId,
                  b.id                           AS toId,
                  r.name                         AS name,
                  toFloat(r.length)              AS length,
                  toInteger(r.lanes)             AS lanes,
                  r.`turn:lanes`                 AS turnLanes,
                  r.oneway                       AS oneway,
                  r.bridge                       AS bridge,
                  r.tunnel                       AS tunnel,
                  r.highway                      AS highway,
                  r.service                      AS service,
                  r.junction                     AS junction,
                  r.ref                          AS ref,
                  r.access                       AS access,
                  r.surface                      AS surface,
                  toFloat(r.width)               AS width,
                  r.lit                          AS lit,
                  toFloat(r.trafficRegularTime)  AS trafficRegularTime,
                  toFloat(r.trafficRegularSpeed) AS trafficRegularSpeed
            ");

        var list = new List<Street>();
        await result.ForEachAsync(rec =>
        {
            double? trafficRegularTime = null;
            if (rec.Keys.Contains("trafficRegularTime"))
                trafficRegularTime = rec["trafficRegularTime"].As<double?>();

            double? trafficRegularSpeed = null;
            if (rec.Keys.Contains("trafficRegularSpeed"))
                trafficRegularSpeed = rec["trafficRegularSpeed"].As<double?>();

            list.Add(new Street
            {
                Id = rec["id"].As<string>(),
                FromId = rec["fromId"].As<string>(),
                ToId = rec["toId"].As<string>(),
                Name = rec["name"].As<string?>(),
                Length = rec["length"].As<double?>(),
                Lanes = rec["lanes"].As<int?>(),
                TurnLanes = rec["turnLanes"].As<string?>(),
                Oneway = rec["oneway"].As<string?>(),
                Bridge = rec["bridge"].As<string?>(),
                Tunnel = rec["tunnel"].As<string?>(),
                Highway = rec["highway"].As<string?>(),
                Service = rec["service"].As<string?>(),
                Junction = rec["junction"].As<string?>(),
                Ref = rec["ref"].As<string?>(),
                Access = rec["access"].As<string?>(),
                Surface = rec["surface"].As<string?>(),
                Width = rec["width"].As<double?>(),
                Lit = rec["lit"].As<string?>(),
                TrafficRegularTime = trafficRegularTime,
                TrafficRegularSpeed = trafficRegularSpeed
            });
        });

        return list;
    }

    public async Task<Street?> GetByIdAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(@"
                MATCH (a)-[r:RELATED]->(b)
                WHERE id(r) = toInteger($id)
                RETURN id(r) AS id,
                       a.id AS fromId,
                       b.id AS toId
            ", new { id });
        var recs = await result.ToListAsync();
        if (recs.Count == 0) return null;
        var rec = recs[0];
        return new Street
        {
            Id = rec["id"].As<string>(),
            FromId = rec["fromId"].As<string>(),
            ToId = rec["toId"].As<string>()
        };
    }

    public async Task CreateAsync(Street segment)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(@"
                MATCH (a { id: $fromId })
                MATCH (b { id: $toId })
                CREATE (a)-[r:RELATED]->(b)
            ", new { fromId = segment.FromId, toId = segment.ToId });
    }

    public async Task UpdateAsync(Street segment)
    {
        // No additional properties to update for RELATED
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(@"
                MATCH ()-[r:RELATED]->()
                WHERE id(r) = toInteger($id)
                DELETE r
            ", new { id });
    }

    public async Task UpdateTrafficRegularTime(IEnumerable<TrafficSegment> trafficResults)
    {
        var batch = trafficResults.Select(r => new {
            id = long.Parse(r.SegmentId),
            duration = r.DurationInSeconds,
            distance = r.DistanceInMeters
        }).ToList();

        const string cypher = @"
            UNWIND $batch AS row
            MATCH ()-[r:RELATED]->()
            WHERE id(r) = row.id
            SET r.trafficRegularTime  = row.duration,
                r.length              = coalesce(r.length, row.distance),
                r.trafficRegularSpeed = CASE
                                          WHEN row.duration > 0 THEN row.distance / row.duration
                                          ELSE NULL
                                        END
        ";


        // open a write session
        await using var session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

        // run inside a write transaction
        await session.ExecuteWriteAsync(async tx =>
        {
            // run your update
            var cursor = await tx.RunAsync(cypher, new { batch });
            // make sure to consume it so the commit happens
            await cursor.ConsumeAsync();
        });
    }


    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}