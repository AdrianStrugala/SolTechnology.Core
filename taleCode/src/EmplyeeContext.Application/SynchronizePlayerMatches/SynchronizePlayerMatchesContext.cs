using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesContext
    {
        public PlayerIdMap PlayerIdMap { get; set; }

        public Player Player { get; set; }

        public List<int> MatchesToSync { get; set; } = new List<int>();
    }
}