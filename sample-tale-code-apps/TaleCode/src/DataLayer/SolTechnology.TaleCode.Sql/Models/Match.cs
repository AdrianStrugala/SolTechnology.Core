using System;
using System.Collections.Generic;

namespace SolTechnology.TaleCode.SqlData.Models
{
    public partial class Match
    {
        public int Id { get; set; }
        public int ApiId { get; set; }
        public int PlayerApiId { get; set; }
        public DateTime? Date { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public int? HomeTeamScore { get; set; }
        public int? AwayTeamScore { get; set; }
        public string Winner { get; set; }
        public string CompetitionWinner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public virtual Player PlayerApi { get; set; }
    }
}
