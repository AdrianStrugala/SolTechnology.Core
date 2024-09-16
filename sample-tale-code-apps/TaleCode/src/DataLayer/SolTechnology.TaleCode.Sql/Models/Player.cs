using System;
using System.Collections.Generic;

namespace SolTechnology.TaleCode.SqlData.Models
{
    public partial class Player
    {
        public Player()
        {
            Matches = new HashSet<Match>();
            Teams = new HashSet<Team>();
        }

        public int Id { get; set; }
        public int ApiId { get; set; }
        public string Name { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Position { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public virtual ICollection<Match> Matches { get; set; }
        public virtual ICollection<Team> Teams { get; set; }
    }
}
