using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces
{
    public interface ISendOrderedFlightEmail
    {
        void Execute(FlightEmailData flightEmailData);
    }
}
