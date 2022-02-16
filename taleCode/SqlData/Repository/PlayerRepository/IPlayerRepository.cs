using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

public interface IPlayerRepository
{
    void Insert(Player player);
}