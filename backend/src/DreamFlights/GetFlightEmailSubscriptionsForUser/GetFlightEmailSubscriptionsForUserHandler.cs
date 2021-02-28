using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;

namespace DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser
{
    public class GetFlightEmailSubscriptionsForUserHandler : IGetFlightEmailSubscriptionsForUser
    {
        private readonly IGetSubscriptionsWithDays _getSubscriptionsWithDays;

        public GetFlightEmailSubscriptionsForUserHandler(IGetSubscriptionsWithDays getSubscriptionsWithDays)
        {
            _getSubscriptionsWithDays = getSubscriptionsWithDays;
        }

        public GetSubscriptionsWithDaysResult Handle(GetSubscriptionsWithDaysQuery query)
        {
            return _getSubscriptionsWithDays.Execute(query);
        }
    }
}