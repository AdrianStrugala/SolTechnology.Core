using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;

namespace DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser
{
    public interface IGetFlightEmailSubscriptionsForUser
    {
        GetSubscriptionsWithDaysResult Execute(GetSubscriptionsWithDaysQuery query);
    }
}