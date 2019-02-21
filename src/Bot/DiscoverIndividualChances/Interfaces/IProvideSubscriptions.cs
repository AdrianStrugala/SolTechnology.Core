using System.Collections.Generic;

namespace DreamTravel.Bot.DiscoverIndividualChances.Interfaces
{
    using Models;

    public interface IProvideSubscriptions
    {
        List<Subscription> Execute();
    }

}
