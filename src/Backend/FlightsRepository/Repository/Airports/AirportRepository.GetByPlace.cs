using System.Linq;
using DreamTravel.Domain.Airports;
using DreamTravel.FlightProviderData.Repository.Airports.PreCalculation;

namespace DreamTravel.FlightProviderData.Repository.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public Airport GetByPlace(string place)
        {
            var result = AirportDataSource.Get().FirstOrDefault(a => a.Name == place);

            return result;
        }
    }
}