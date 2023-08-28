using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces
{
    public interface ISendOrderedFlightEmail
    {
        void Handle(FlightEmailData flightEmailData);
    }
}
