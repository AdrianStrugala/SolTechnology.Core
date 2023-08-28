using System;
using System.Collections.Generic;

namespace DreamTravel.DreamFlights.UpdateSubscriptions
{
    public class UpdateSubscriptionsCommand
    {
        public List<DayChangedEvent> Events { get; set; }

        public Guid UserId { get; set; }
    }

    public class DayChangedEvent
    {
        public int SubscriptionId { get; set; }
        public string Day { get; set; }
        public bool Value { get; set; }
    }
}
