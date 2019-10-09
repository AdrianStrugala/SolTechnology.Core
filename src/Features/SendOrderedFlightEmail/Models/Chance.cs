namespace DreamTravel.Features.SendOrderedFlightEmail.Models
{
    public class Chance
    {
        public string Origin { get; set; }        
        public string ThereCarrier { get; set; }        
        public string ThereDay { get; set; }

        public string Destination { get; set; }
        public string BackCarrier { get; set; }
        public string BackDay { get; set; }

        public string ActualAt { get; set; }
        public double Price { get; set; }
    }
}
