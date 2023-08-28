using System;

namespace DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays
{
    public class GetSubscriptionsWithDaysQuery
    {
        public Guid UserId { get; }

        public GetSubscriptionsWithDaysQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
