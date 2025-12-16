using DreamTravel.Trips.Domain.StreetGraph;
using Neo4j.Driver;

namespace DreamTravel.GraphDatabase.Repositories;


public interface IIntersectionRepository
{
    Task<IEnumerable<Intersection>> GetAllAsync();
    Task<Intersection?> GetByIdAsync(string id);
    Task CreateAsync(Intersection intersection);
    Task UpdateAsync(Intersection intersection);
    Task DeleteAsync(string id);
}



public class IntersectionRepository(IDriverProvider provider) : IIntersectionRepository, IAsyncDisposable
{
    private readonly IDriver _driver = provider.Driver;

    public async Task<IEnumerable<Intersection>> GetAllAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(@"
                MATCH (n)
                WHERE n.id IS NOT NULL AND n.x IS NOT NULL AND n.y IS NOT NULL
                RETURN n.id AS id, n.x AS x, n.y AS y
            ");
        var list = new List<Intersection>();
        await result.ForEachAsync(rec =>
        {
            var xStr = rec["x"].As<string>();
            var yStr = rec["y"].As<string>();
            list.Add(new Intersection
            {
                Id = rec["id"].As<string>(),
                Lat = double.Parse(yStr),
                Lng = double.Parse(xStr)
            });
        });
        return list;
    }

    public async Task<Intersection?> GetByIdAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(@"
                MATCH (n)
                WHERE n.id = $id
                RETURN n.id AS id, n.x AS x, n.y AS y
            ", new { id });
        var recs = await result.ToListAsync();
        if (recs.Count == 0) return null;
        var rec = recs[0];
        var xStr = rec["x"].As<string>();
        var yStr = rec["y"].As<string>();
        return new Intersection
        {
            Id = rec["id"].As<string>(),
            Lat = double.Parse(yStr),
            Lng = double.Parse(xStr)
        };
    }

    public async Task CreateAsync(Intersection intersection)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(@"
                CREATE (n:Intersection { id: $id, x: toString($lng), y: toString($lat) })
            ", new { id = intersection.Id, lat = intersection.Lat, lng = intersection.Lng });
    }

    public async Task UpdateAsync(Intersection intersection)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(@"
                MATCH (n:Intersection { id: $id })
                SET n.x = toString($lng), n.y = toString($lat)
            ", new { id = intersection.Id, lat = intersection.Lat, lng = intersection.Lng });
    }

    public async Task DeleteAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(@"
                MATCH (n:Intersection { id: $id })
                DETACH DELETE n
            ", new { id });
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}