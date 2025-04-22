using DreamTravel.Trips.Domain.StreetGraph;
using Neo4j.Driver;

namespace DreamTravel.GraphDatabase.Repositories;

public interface IStreetRepository
{
    Task<IEnumerable<Street>> GetAllAsync();
    Task<Street?> GetByIdAsync(string id);
    Task CreateAsync(Street segment);
    Task UpdateAsync(Street segment);
    Task DeleteAsync(string id);
}

public class StreetRepository(IDriverProvider provider) : IStreetRepository, IAsyncDisposable
{
    private readonly IDriver _driver = provider.Driver;

    public async Task<IEnumerable<Street>> GetAllAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(@"
                MATCH (a)-[r:RELATED]->(b)
                WHERE a.id IS NOT NULL AND b.id IS NOT NULL
                RETURN id(r) AS id,
                       a.id AS fromId,
                       b.id AS toId
            ");
        var list = new List<Street>();
        await result.ForEachAsync(rec => list.Add(new Street
        {
            Id = rec["id"].As<string>(),
            FromId = rec["fromId"].As<string>(),
            ToId = rec["toId"].As<string>()
        }));
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

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}