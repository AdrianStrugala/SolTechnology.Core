using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.OrderFlightEmail
{
    public interface IOrderFlightEmail
    {
        void Execute(FlightEmailOrder flightEmailOrder);
    }
}
