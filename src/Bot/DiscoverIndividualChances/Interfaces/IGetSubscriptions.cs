using System.Collections.Generic;

namespace DreamTravel.Bot.DiscoverIndividualChances.Interfaces
{
    using Models;

    public interface IGetSubscriptions
    {
        List<Subscription> Execute();
    }

}
