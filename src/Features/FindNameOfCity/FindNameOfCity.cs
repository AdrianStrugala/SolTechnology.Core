using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;

namespace DreamTravel.Features.FindNameOfCity
{
    public class FindNameOfCity : IFindNameOfCity
    {
        private readonly ICityRepository _cityRepository;

        public FindNameOfCity(ICityRepository cityRepository)
        {
            _cityRepository = cityRepository;

        }

        public async Task<City> Execute(double lat, double lng)
        {
            City result = new City
            {
                Latitude = lat,
                Longitude = lng
            };

            result = await _cityRepository.GetName(result);

            return result;
        }
    }
}
