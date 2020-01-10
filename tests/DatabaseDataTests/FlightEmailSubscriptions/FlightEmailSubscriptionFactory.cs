using System;
using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseDataTests.FlightEmailSubscriptions
{
    public class FlightEmailSubscriptionFactory
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public FlightEmailSubscriptionFactory(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public FlightEmailSubscription InsertFlightEmailSubscriptionForUser(int userId)
        {
            FlightEmailSubscription flightEmailSubscription = new FlightEmailSubscription();
            flightEmailSubscription.UserId = userId;
            flightEmailSubscription.ArrivalDate = DateTime.UtcNow.AddDays(3);
            flightEmailSubscription.DepartureDate = DateTime.UtcNow;
            flightEmailSubscription.From = "Poland";
            flightEmailSubscription.To = "London";
            flightEmailSubscription.MaxDaysOfStay = 4;
            flightEmailSubscription.MinDaysOfStay = 1;
            flightEmailSubscription.OneWay = false;

            string sql = @"
INSERT INTO FlightEmailSubscription
([UserId], [From], [To], [DepartureDate], [ArrivalDate], [MinDaysOfStay], [MaxDaysOfStay], [OneWay])
VALUES
(@UserId, @From, @To, @DepartureDate, @ArrivalDate, @MinDaysOfStay, @MaxDaysOfStay, @OneWay)

SELECT SCOPE_IDENTITY()
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                flightEmailSubscription.Id = connection.QuerySingle<int>(sql, new
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

            return flightEmailSubscription;
        }
    }
}
