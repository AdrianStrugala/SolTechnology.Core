namespace DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays
{
    public class GetSubscriptionsWithDaysQuery
    {
        public int UserId { get; }

        public GetSubscriptionsWithDaysQuery(int userId)
        {
            UserId = userId;
        }
    }
}
