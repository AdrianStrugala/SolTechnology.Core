using System.Collections.Generic;

namespace DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay
{
    public interface IGetSubscriptionDetailsByDay
    {
        List<FlightEmailData> Execute(string day);
    }
}