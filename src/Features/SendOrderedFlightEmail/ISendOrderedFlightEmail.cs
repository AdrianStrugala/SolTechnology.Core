using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.Features.SendOrderedFlightEmail
{
    public interface ISendOrderedFlightEmail
    {
        void Execute(FlightEmailOrder flightEmailOrder);
    }
}
