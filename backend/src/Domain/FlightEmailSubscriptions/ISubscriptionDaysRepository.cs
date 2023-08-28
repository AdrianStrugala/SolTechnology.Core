using System;
using System.Collections.Generic;

namespace DreamTravel.Domain.FlightEmailSubscriptions
{
    public interface ISubscriptionDaysRepository
    {
        void Insert(SubscriptionDays subscriptionDays);

        Dictionary<long, SubscriptionDays> GetByUser(Guid userId);

        void Update(SubscriptionDays subscriptionDays);
    }
}