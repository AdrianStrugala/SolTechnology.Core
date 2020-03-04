using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;

namespace DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser
{
    public class GetFlightEmailSubscriptionsForUser : IGetFlightEmailSubscriptionsForUser
    {
        private readonly IGetSubscriptionsWithDays _getSubscriptionsWithDays;

        public GetFlightEmailSubscriptionsForUser(IGetSubscriptionsWithDays getSubscriptionsWithDays)
        {
            _getSubscriptionsWithDays = getSubscriptionsWithDays;
        }

        public GetSubscriptionsWithDaysResult Execute(GetSubscriptionsWithDaysQuery query)
        {
            return _getSubscriptionsWithDays.Execute(query);
        }
    }
}