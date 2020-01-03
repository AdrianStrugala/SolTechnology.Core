using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces
{
    public interface ISendOrderedFlightEmail
    {
        void Execute(FlightEmailData flightEmailOrder);
    }
}
