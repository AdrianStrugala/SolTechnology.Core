using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.StaticData.PlayerId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics
{
    public class CalculatePlayerStatisticsContext
    {
        public PlayerStatistics Result { get; set; }
        public PlayerIdMap PlayerIdMap { get; set; }
        public Player Player { get; set; }
        public List<Match> Matches { get; set; }
        public List<Match> NationalTeamMatches { get; set; }
        public List<Match> ClubMatches { get; set; }
    }
}
