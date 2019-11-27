using System.Collections.Generic;

namespace DreamTravel.Domain.Airports
{
    public class Airport
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<string> Codes { get; set; }
    }
}
