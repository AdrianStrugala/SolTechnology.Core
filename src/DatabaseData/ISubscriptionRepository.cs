using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.DatabaseData
{
    public interface ISubscriptionRepository
    {
        List<Subscription> GetSubscriptions();
    }
}