using System;
using System.Collections.Generic;

namespace SolTechnology.TaleCode.SqlData.Models
{
    public partial class Team
    {
        public long Id { get; set; }
        public int PlayerApiId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public virtual Player PlayerApi { get; set; }
    }
}
