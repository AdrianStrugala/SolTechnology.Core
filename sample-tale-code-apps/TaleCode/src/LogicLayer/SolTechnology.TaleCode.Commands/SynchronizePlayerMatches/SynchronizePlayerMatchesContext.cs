using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesContext
    {
        public int PlayerId { get; set; }

        public Player Player { get; set; }

        public List<int> MatchesToSync { get; set; } = new List<int>();

        public SynchronizePlayerMatchesContext(int playerId)
        {
            PlayerId = playerId;
        }
    }
}