using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Features.SendOrderedFlightEmail.Models;

namespace DreamTravel.Features.SendOrderedFlightEmail.Interfaces
{
    public interface IComposeMessage
    {
        string Execute(Chance chance, Subscription subscription);
    }
}