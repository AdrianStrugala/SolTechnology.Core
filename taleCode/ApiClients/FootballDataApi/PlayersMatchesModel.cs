using SolTechnology.TaleCode.Domain;

namespace ApiClients.FootballDataApi
{
    public class PlayersMatchesModel
    {
        public int Count { get; set; }
        public Player Player { get; set; }

        public List<Match> matches { get; set; }
    }
}
