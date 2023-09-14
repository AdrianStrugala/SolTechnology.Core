using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.MatchRepository;

public interface IMatchRepository
{
    List<Match> GetByPlayerId(int playerApiId);
    void Insert(Match match);
}