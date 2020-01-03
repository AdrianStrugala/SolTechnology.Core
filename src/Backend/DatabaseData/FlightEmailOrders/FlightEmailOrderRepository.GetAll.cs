using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DatabaseData.FlightEmailOrders
{
    public partial class FlightEmailOrderRepository : IFlightEmailOrderRepository
    {
        public List<FlightEmailData> GetAll()
        {
            var result = new List<FlightEmailData>();

            string sql = @"
SELECT 
FlightEmailOrder.[UserId],
FlightEmailOrder.[From],
FlightEmailOrder.[To],
FlightEmailOrder.[DepartureDate],
FlightEmailOrder.[ArrivalDate],
FlightEmailOrder.[MinDaysOfStay], 
FlightEmailOrder.[MaxDaysOfStay], 
FlightEmailOrder.[OneWay],
[User].Name AS UserName,
[User].Email,
[User].Currency

FROM FlightEmailOrder
JOIN [User] on [User].Id = FlightEmailOrder.[UserId]
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<FlightEmailData>(sql).ToList();
            }

            return result;
        }
    }
}