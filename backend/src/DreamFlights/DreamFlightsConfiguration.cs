namespace DreamTravel.DreamFlights
{
    public interface IDreamFlightsConfiguration
    {
        bool SendEmails { get; set; }
    }

    public class DreamFlightsConfiguration : IDreamFlightsConfiguration
    {
        public bool SendEmails { get; set; }
    }
}
