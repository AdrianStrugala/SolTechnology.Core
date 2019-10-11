using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.Features.SendOrderedFlightEmail.Interfaces
{
    public interface ISendOrderedFlightEmail
    {
        void Execute(FlightEmailOrder flightEmailOrder);
    }
}
