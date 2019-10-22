using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.OrderFlightEmail
{
    public interface IOrderFlightEmail
    {
        void Execute(FlightEmailOrder flightEmailOrder);
    }
}
