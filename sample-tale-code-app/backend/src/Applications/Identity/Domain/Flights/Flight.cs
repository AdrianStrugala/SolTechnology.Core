namespace DreamTravel.Domain.Flights
{
    public class Flight
    {
        public string ThereDate { get; set; }
        public string ThereDepartureCity { get; set; }
        public string ThereArrivalCity { get; set; }
        public string ThereDepartureHour { get; set; }
        public string ThereArrivalHour { get; set; }

        public string BackDate { get; set; }
        public string BackDepartureCity { get; set; }
        public string BackArrivalCity { get; set; }
        public string BackDepartureHour { get; set; }
        public string BackArrivalHour { get; set; }

        public double Price { get; set; }
    }
}
