using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.OrderFlightEmail
{
    public interface IOrderFlightEmail
    {
        void Execute(FlightEmailOrder flightEmailOrder);
    }
}
