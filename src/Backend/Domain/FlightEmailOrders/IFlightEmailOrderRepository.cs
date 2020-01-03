using System.Collections.Generic;

namespace DreamTravel.Domain.FlightEmailOrders
{
    public interface IFlightEmailOrderRepository
    {
        void Insert(FlightEmailOrder flightEmailOrder);

        List<FlightEmailData> GetAll();

        List<FlightEmailOrder> GetByUserId(int userId);

        void Delete(int id);
    }
}