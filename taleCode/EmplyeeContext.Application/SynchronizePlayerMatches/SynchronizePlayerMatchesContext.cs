using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesContext : ICommand
    {
        public int PlayerId { get; set; }

        public Player Player { get; set; }

        public List<int> MatchesToSync { get; set; } = new List<int>();
    }
}