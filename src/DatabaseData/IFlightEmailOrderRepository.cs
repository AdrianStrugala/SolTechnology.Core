using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;

namespace DreamTravel.DatabaseData
{
    public interface IFlightEmailOrderRepository
    {
        void Insert(FlightEmailOrder flightEmailOrder);

        List<FlightEmailOrder> GetAll();
    }
}