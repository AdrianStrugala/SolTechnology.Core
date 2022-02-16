using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesContext
    {
        public SynchronizePlayerMatchesCommand Command { get; set; }
        public Player Player { get; set; }
        public List<int> MatchIds { get; set; }
    }
}
