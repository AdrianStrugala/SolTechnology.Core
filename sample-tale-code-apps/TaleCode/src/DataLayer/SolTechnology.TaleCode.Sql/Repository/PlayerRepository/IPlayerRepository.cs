using Microsoft.EntityFrameworkCore;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

public interface IPlayerRepository
{
    void Insert(Player player);
    void Update(Player player);
    Player GetById(int apiId);
}

public class PlayerRepositoryOnEf : IPlayerRepository
{
    private readonly TaleCodeDbContext _dbContext;

    public PlayerRepositoryOnEf(TaleCodeDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public void Insert(Player player)
    {
        _dbContext.Players.Add(player);
        _dbContext.SaveChanges();
    }

    public Player GetById(int apiId)
    {
        var player = _dbContext.Players
            .Include(p => p.Teams)
            .SingleOrDefault(p => p.ApiId == apiId);

        return player;
    }
    
    public void Update(Player player)
    {
        _dbContext.Players.Update(player);

        foreach (var team in player.Teams)
        {
            var existingTeam = _dbContext.Teams
                .FirstOrDefault(t => t.PlayerApiId == team.PlayerApiId
                                     && t.Name == team.Name
                                     && t.DateFrom == team.DateFrom);

            if (existingTeam != null)
            {
                // Update existing team
                existingTeam.DateTo = team.DateTo;
                _dbContext.Teams.Update(existingTeam);
            }
            else
            {
                // Add new team
                _dbContext.Teams.Add(team);
            }
        }

        _dbContext.SaveChanges();

    }
}