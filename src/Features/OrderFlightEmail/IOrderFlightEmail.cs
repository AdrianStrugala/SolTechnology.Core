using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.Features.OrderFlightEmail
{
    public interface IOrderFlightEmail
    {
        void Execute(FlightEmailOrder flightEmailOrder);
    }
}
