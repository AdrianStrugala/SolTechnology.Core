namespace DreamTravel.DreamFlights.UpdateSubscriptions
{
    public class DayChangedEvent
    {
        public int SubscriptionId { get; set; }
        public string Day { get; set; }
        public bool Value { get; set; }
    }
}
