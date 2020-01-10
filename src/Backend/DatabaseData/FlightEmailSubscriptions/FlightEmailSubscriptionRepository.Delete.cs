using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DatabaseData.FlightEmailSubscriptions
{
    public partial class FlightEmailSubscriptionRepository : IFlightEmailSubscriptionRepository
    {
        public void Delete(int id)
        {

            string sql = @"
DELETE 
    FlightEmailSubscription 
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