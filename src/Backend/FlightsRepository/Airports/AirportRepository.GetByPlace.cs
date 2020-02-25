using System.Linq;
using DreamTravel.Domain.Airports;

namespace DreamTravel.FlightProviderData.Airports
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