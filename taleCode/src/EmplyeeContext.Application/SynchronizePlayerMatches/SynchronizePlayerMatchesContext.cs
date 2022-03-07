using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.StaticData;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesContext : ICommand
    {
        public string PlayerName { get; set; }

        public PlayerIdMap PlayerIdMap { get; set; }

        public Player Player { get; set; }

        public List<int> MatchesToSync { get; set; } = new List<int>();
    }
}