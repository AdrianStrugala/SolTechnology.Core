using System.Collections.Generic;

namespace DreamTravel.Domain.Airports
{
    public interface IAirportRepository
    {
        List<Airport> Get();

        Airport GetByPlace(string place);
    }
}