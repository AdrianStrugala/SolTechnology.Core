using System.Collections.Generic;

namespace DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays
{
    public class GetSubscriptionsWithDaysResult
    {
        public List<SubscriptionWithDays> SubscriptionsWithDays { get; set; }

        public GetSubscriptionsWithDaysResult()
        {
            SubscriptionsWithDays = new List<SubscriptionWithDays>();
        }
    }
}
