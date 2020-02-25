using System.Collections.Generic;

namespace DreamTravel.Domain.Airports
{
    public interface IAirportRepository
    {
        List<Airport> GetAll();

        Airport GetByPlace(string place);
    }
}