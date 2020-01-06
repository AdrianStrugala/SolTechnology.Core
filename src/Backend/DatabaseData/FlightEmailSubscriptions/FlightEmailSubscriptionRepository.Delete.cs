using Dapper;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DatabaseData.FlightEmailSubscriptions
{
    public partial class FlightEmailSubscriptionRepository : IFlightEmailSubscriptionRepository
    {
        public void Delete(int id)
        {

            string sql = @"
DELETE 
    FlightEmailOrder 
WHERE 
    [Id] = @Id
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(sql, new {id = id});
            }
        }
    }
}