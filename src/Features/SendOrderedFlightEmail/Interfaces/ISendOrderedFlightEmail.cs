using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.Features.SendOrderedFlightEmail.Interfaces
{
    public interface ISendOrderedFlightEmail
    {
        void Execute(FlightEmailData flightEmailOrder);
    }
}
