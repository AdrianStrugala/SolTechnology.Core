using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.FlightEmailSubscriptions
{
    public partial class FlightEmailSubscriptionRepository : IFlightEmailSubscriptionRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public FlightEmailSubscriptionRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public int Insert(FlightEmailSubscription flightEmailSubscription)
        {
            string sql = @"
INSERT INTO FlightEmailSubscription
([UserId], [From], [To], [DepartureDate], [ArrivalDate], [MinDaysOfStay], [MaxDaysOfStay], [OneWay])
OUTPUT INSERTED.ID
VALUES
(@UserId, @From, @To, @DepartureDate, @ArrivalDate, @MinDaysOfStay, @MaxDaysOfStay, @OneWay)
";

            int subscriptionId;

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                subscriptionId = connection.QuerySingle<int>(sql, new
                {
                    UserId = flightEmailSubscription.UserId,
                    From = flightEmailSubscription.From,
                    To = flightEmailSubscription.To,
                    DepartureDate = flightEmailSubscription.DepartureDate,
                    ArrivalDate = flightEmailSubscription.ArrivalDate,
                    MinDaysOfStay = flightEmailSubscription.MinDaysOfStay,
                    MaxDaysOfStay = flightEmailSubscription.MaxDaysOfStay,
                    OneWay = flightEmailSubscription.OneWay
                });
            }

            return subscriptionId;
        }
    }
}