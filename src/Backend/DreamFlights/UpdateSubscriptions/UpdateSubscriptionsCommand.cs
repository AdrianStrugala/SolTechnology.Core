using System.Collections.Generic;

namespace DreamTravel.DreamFlights.UpdateSubscriptions
{
    public class UpdateSubscriptionsCommand
    {
        public List<DayChangedEvent> Events { get; set; }

        public int UserId { get; set; }
    }
}
