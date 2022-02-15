namespace SolTechnology.TaleCode.Domain.Match;

public interface IMatchRepository
{
    List<Match> GetByPlayerId(int playerApiId);
    void Insert(Match match);
}