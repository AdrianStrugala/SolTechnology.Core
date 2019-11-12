using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DatabaseData
{
    public interface IFlightEmailOrderRepository
    {
        void Insert(FlightEmailOrder flightEmailOrder);

        List<FlightEmailData> GetAll();

        List<FlightEmailOrder> GetByUserId(int userId);
    }
}