using DreamTravel.Domain.FlightEmailOrders;

namespace DreamTravel.DreamFlights.DeleteFlightEmailOrder
{
    public class DeleteFlightEmailOrder : IDeleteFlightEmailOrder
    {
        private readonly IFlightEmailOrderRepository _flightEmailOrderRepository;

        public DeleteFlightEmailOrder(IFlightEmailOrderRepository flightEmailOrderRepository)
        {
            _flightEmailOrderRepository = flightEmailOrderRepository;
        }


        public void Execute(int id)
        {
            _flightEmailOrderRepository.Delete(id);
        }
    }
}