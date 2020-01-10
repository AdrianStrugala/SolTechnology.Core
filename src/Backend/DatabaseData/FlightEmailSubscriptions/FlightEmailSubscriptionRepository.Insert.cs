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

        public void Insert(FlightEmailSubscription flightEmailSubscription)
        {
            string sql = @"
INSERT INTO FlightEmailSubscription
([UserId], [From], [To], [DepartureDate], [ArrivalDate], [MinDaysOfStay], [MaxDaysOfStay], [OneWay])
VALUES
(@UserId, @From, @To, @DepartureDate, @ArrivalDate, @MinDaysOfStay, @MaxDaysOfStay, @OneWay)
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(sql, new
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
        }
    }
}