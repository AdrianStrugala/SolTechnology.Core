using System.Collections.Generic;
using DapperExtensions;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.FlightEmailOrders
{
    public class FlightEmailOrderRepository : IFlightEmailOrderRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public FlightEmailOrderRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public void Insert(FlightEmailOrder flightEmailOrder)
        {
            throw new System.NotImplementedException();

//            using (var connection = _dbConnectionFactory.CreateConnection())
//            {
//               var id = connection.Insert()
//            }
        }

        public List<FlightEmailOrder> GetAll()
        {
            throw new System.NotImplementedException();
        }
    }
}