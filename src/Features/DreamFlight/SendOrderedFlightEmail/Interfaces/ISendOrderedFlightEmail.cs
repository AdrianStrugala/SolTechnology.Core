using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.DreamFlight.SendOrderedFlightEmail.Interfaces
{
    public interface ISendOrderedFlightEmail
    {
        void Execute(FlightEmailData flightEmailOrder);
    }
}
